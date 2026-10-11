using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolarExplorer
{
    public enum UniverseView { SolarSystem, MilkyWay, LocalGroup, Galaxy, Neighborhood, StarSystem, Planet }
    public sealed class AtlasNavigation
    {
        public string Id,Label;
        public AtlasNavigation(string id,string label) { Id=id; Label=label; }
    }

    public sealed class UniverseExplorer : MonoBehaviour
    {
        public UniverseView View { get; private set; }=UniverseView.SolarSystem;
        public bool Active => View!=UniverseView.SolarSystem || IsCrossing;
        public bool IsCrossing { get; private set; }
        public string SystemId { get; private set; }
        public string SelectedId { get; private set; }
        public UniverseCatalog Catalog { get; private set; }
        public UniverseUI UI { get; private set; }
        public AtlasScene CurrentScene { get; private set; }
        public bool InStarSystem => View==UniverseView.StarSystem || View==UniverseView.Planet;
        public SolarSystemBootstrap Owner { get; private set; }
        private Camera cameraView;
        private UniverseVisuals visuals;
        private readonly Dictionary<string,AtlasScene> scenes=new Dictionary<string,AtlasScene>();
        private Action pending;
        private float travelTime;
        private bool swapped;
        public bool AutomaticTravel { get; set; }=true;

        public void Initialize(SolarSystemBootstrap owner,Camera camera)
        {
            Owner=owner; cameraView=camera; Catalog=UniverseCatalog.Load();
            var world=new GameObject("Universe atlas worlds"); world.transform.SetParent(transform,false);
            visuals=new UniverseVisuals(world.transform);
            UI=gameObject.AddComponent<UniverseUI>(); UI.Initialize(this,camera);
        }

        public void OpenMilkyWay() => Navigate(UniverseView.MilkyWay,"milky-way",true);
        public void OpenLocalGroup() => Navigate(UniverseView.LocalGroup,"local-group",true);
        public void OpenNeighborhood() => Navigate(UniverseView.Neighborhood,"neighborhood",false);
        public void OpenGalaxy(string id)
        {
            if(Array.Find(Catalog.galaxies,item=>item.id==id)==null) return;
            Navigate(id=="milky-way"?UniverseView.MilkyWay:UniverseView.Galaxy,id,false);
        }
        public void OpenSystem(string id)
        {
            if(id=="solar") { Home(); return; }
            if(Array.Find(Catalog.systems,item=>item.article.id==id)==null) return;
            Navigate(UniverseView.StarSystem,id,false);
        }
        public void Home() { if(View!=UniverseView.SolarSystem) Navigate(UniverseView.SolarSystem,"solar",false); }

        public void Back()
        {
            if(Owner.InteractionLocked) return;
            switch(View)
            {
                case UniverseView.Planet: ShowSystemOverview(); break;
                case UniverseView.StarSystem: OpenNeighborhood(); break;
                case UniverseView.Neighborhood: OpenMilkyWay(); break;
                case UniverseView.Galaxy: case UniverseView.MilkyWay: OpenLocalGroup(); break;
                case UniverseView.LocalGroup: Home(); break;
            }
        }

        public void Select(string id)
        {
            if(Owner.InteractionLocked) return;
            if(InStarSystem && CurrentScene!=null)
            {
                var body=CurrentScene.Targets.Find(item=>item.Id==id);
                if(body!=null) { FocusBody(body); return; }
            }
            if(id=="solar") Home();
            else if(id=="neighborhood") OpenNeighborhood();
            else if(id=="local-group") OpenLocalGroup();
            else if(Array.Exists(Catalog.galaxies,item=>item.id==id)) OpenGalaxy(id);
            else OpenSystem(id);
        }

        public void SelectAt(Vector2 point)
        {
            if(Owner.InteractionLocked || CurrentScene==null || ExplorerInput.OverInterface(point)) return;
            if(Physics.Raycast(cameraView.ScreenPointToRay(point),out var hit,cameraView.farClipPlane))
            {
                var target=hit.collider.GetComponentInParent<AtlasTarget>();
                if(target!=null) { Select(target.Id); return; }
            }
            float best=Mathf.Max(25,cameraView.pixelHeight*0.027f); AtlasTarget nearest=null;
            foreach(var target in CurrentScene.Targets)
            {
                Vector3 projected=cameraView.WorldToScreenPoint(target.transform.position);
                if(projected.z<=0) continue;
                float distance=Vector2.Distance(projected,point);
                if(distance<best) { best=distance; nearest=target; }
            }
            if(nearest!=null) Select(nearest.Id);
        }

        private void Navigate(UniverseView destination,string id,bool outward)
        {
            if(Owner.InteractionLocked) return;
            IsCrossing=true; swapped=false; travelTime=0;
            pending=()=>ApplyDestination(destination,id,outward);
            UI.BeginCrossing(); Owner.CameraRig.BeginScaleDeparture();
            Owner.Interface.RefreshInteractionState();
            if(Owner.CameraRig.ReduceMotion)
            {
                pending(); pending=null; Owner.CameraRig.Snap(); FinishCrossing();
            }
        }

        private void ApplyDestination(UniverseView destination,string id,bool outward)
        {
            if(CurrentScene!=null) CurrentScene.Root.SetActive(false);
            CurrentScene=null; View=destination; SelectedId=id;
            if(destination==UniverseView.SolarSystem)
            {
                UI.SetHUDVisible(false); Owner.RestoreSolarOverview(); return;
            }
            Owner.SolarWorld.SetActive(false); Owner.Interface.Canvas.gameObject.SetActive(false);
            cameraView.clearFlags=CameraClearFlags.SolidColor;
            cameraView.farClipPlane=5000;
            cameraView.backgroundColor=new Color(0.0015f,0.003f,0.009f);
            string key=destination==UniverseView.Galaxy || destination==UniverseView.MilkyWay?"galaxy:"+id:id;
            if(!scenes.TryGetValue(key,out var scene))
            {
                if(destination==UniverseView.LocalGroup) scene=visuals.LocalGroup(Catalog.galaxies);
                else if(destination==UniverseView.Neighborhood) scene=visuals.Neighborhood();
                else if(destination==UniverseView.StarSystem) scene=visuals.System(Array.Find(Catalog.systems,item=>item.article.id==id));
                else { int index=Array.FindIndex(Catalog.galaxies,item=>item.id==id); scene=visuals.Galaxy(index,Catalog.galaxies[index]); }
                scenes.Add(key,scene);
            }
            CurrentScene=scene; scene.Root.SetActive(true); scene.ShowOrbitGuides(true);
            if(destination==UniverseView.StarSystem) SystemId=id;
            ShowArticle();
            Owner.CameraRig.ShowMap(scene.Pivot,scene.Radius,scene.Angle,false,true,outward);
        }

        private void FocusBody(AtlasTarget body)
        {
            View=UniverseView.Planet; SelectedId=body.Id;
            CurrentScene.ShowOrbitGuides(false);
            Vector3 direction=body.transform.position.normalized;
            float yaw=body.transform.position.sqrMagnitude<0.1f?-25:Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg-25;
            Owner.CameraRig.ShowMap(body.transform.position,body.Radius*1.14f,new Vector2(32,yaw),true);
            ShowArticle();
        }

        private void ShowSystemOverview()
        {
            View=UniverseView.StarSystem; SelectedId=SystemId;
            CurrentScene.ShowOrbitGuides(true);
            Owner.CameraRig.ShowMap(CurrentScene.Pivot,CurrentScene.Radius,CurrentScene.Angle,false);
            ShowArticle();
        }

        private void ShowArticle()
        {
            AtlasEntry article; string primary; Action action; var destinations=new List<AtlasNavigation>();
            if(View==UniverseView.LocalGroup)
            {
                article=Catalog.localGroup; primary="ENTER THE MILKY WAY"; action=OpenMilkyWay;
                foreach(var galaxy in Catalog.galaxies) destinations.Add(new AtlasNavigation(galaxy.id,ShortGalaxy(galaxy.id)));
            }
            else if(View==UniverseView.Neighborhood)
            {
                article=Catalog.neighborhood; primary="EXPLORE TRAPPIST-1"; action=()=>OpenSystem("trappist-1");
                destinations.Add(new AtlasNavigation("solar","SOLAR SYSTEM"));
                destinations.Add(new AtlasNavigation("proxima-centauri","PROXIMA CENTAURI"));
                destinations.Add(new AtlasNavigation("trappist-1","TRAPPIST-1"));
            }
            else if(InStarSystem)
            {
                var system=Array.Find(Catalog.systems,item=>item.article.id==SystemId);
                var planet=Array.Find(system.planets,item=>item.article.id==SelectedId);
                article=planet!=null?planet.article:system.article;
                primary=View==UniverseView.Planet?"RESET VIEW":"NEARBY STARS";
                action=View==UniverseView.Planet?(Action)Owner.CameraRig.ResetView:OpenNeighborhood;
                destinations.Add(new AtlasNavigation(system.article.id,"STAR"));
                foreach(var world in system.planets) destinations.Add(new AtlasNavigation(world.article.id,world.article.name.Replace("TRAPPIST-1 ","" ).ToUpperInvariant()));
            }
            else
            {
                article=Array.Find(Catalog.galaxies,item=>item.id==SelectedId);
                bool milky=View==UniverseView.MilkyWay;
                primary=milky?"EXPLORE NEARBY STARS":"VIEW LOCAL GROUP"; action=milky?(Action)OpenNeighborhood:OpenLocalGroup;
                if(milky) { destinations.Add(new AtlasNavigation("neighborhood","NEARBY STARS")); destinations.Add(new AtlasNavigation("solar","SOLAR SYSTEM")); destinations.Add(new AtlasNavigation("local-group","LOCAL GROUP")); }
                else foreach(var galaxy in Catalog.galaxies) destinations.Add(new AtlasNavigation(galaxy.id,ShortGalaxy(galaxy.id)));
            }
            UI.Show(article,primary,action,destinations);
        }

        public string LabelFor(string id)
        {
            if(id=="neighborhood") return "SOLAR NEIGHBORHOOD";
            if(id=="solar") return "SOLAR SYSTEM";
            if(id=="proxima-centauri") return "PROXIMA CENTAURI";
            if(id=="trappist-1") return "TRAPPIST-1";
            var galaxy=Array.Find(Catalog.galaxies,item=>item.id==id);
            if(galaxy!=null) return ShortGalaxy(id);
            foreach(var system in Catalog.systems) foreach(var planet in system.planets) if(planet.article.id==id) return planet.article.name.ToUpperInvariant();
            return id.ToUpperInvariant();
        }
        private static string ShortGalaxy(string id)
        {
            if(id=="large-magellanic-cloud") return "LARGE MAGELLANIC CLOUD";
            if(id=="small-magellanic-cloud") return "SMALL MAGELLANIC CLOUD";
            return id.Replace('-',' ').ToUpperInvariant();
        }
        private void Update() { if(AutomaticTravel) AdvanceTravel(Time.unscaledDeltaTime); }
        public void AdvanceTravel(float delta)
        {
            if(!IsCrossing) return;
            travelTime+=Mathf.Max(0,delta);
            if(!swapped && travelTime>=0.7f) { swapped=true; pending(); pending=null; }
            float alpha=!swapped?Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.22f,0.7f,travelTime)):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.7f,1.35f,travelTime));
            UI.SetCurtain(alpha);
            if(travelTime>=2.1f) FinishCrossing();
        }
        private void FinishCrossing()
        {
            IsCrossing=false; UI.SetCurtain(0); UI.Canvas.gameObject.SetActive(View!=UniverseView.SolarSystem);
            UI.Refresh(); Owner.Interface.RefreshInteractionState();
        }
        public void AdvanceAnimation(float delta) { visuals?.Advance(delta); }
        private void OnDestroy() { visuals?.Dispose(); }
    }
}
