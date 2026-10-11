using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SolarExplorer.Editor
{
    [InitializeOnLoad]
    public static class UniverseSmokeChecks
    {
        private const string Key="SolarExplorer.UniverseChecks";
        static UniverseSmokeChecks() { EditorApplication.playModeStateChanged+=OnMode; }
        [MenuItem("Solar Explorer/Run Universe Checks and Previews")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before running atlas checks.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ProjectChecks.Validate(); EditorSceneManager.OpenScene(ProjectChecks.ScenePath);
            SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }
        private static void OnMode(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            SessionState.SetBool(Key,false); new GameObject("Universe checks").AddComponent<UniverseSmokeDriver>();
        }
    }

    public sealed class UniverseSmokeDriver : MonoBehaviour
    {
        private bool failed;
        private SolarSystemBootstrap owner;
        private UniverseExplorer atlas;
        private Camera cameraView,overlay;
        private RenderTexture target;
        private void Awake() { Application.logMessageReceived+=OnLog; }
        private void OnDestroy() { Application.logMessageReceived-=OnLog; }
        private void OnLog(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true; }
        private IEnumerator Start()
        {
            yield return null;
            try { Check(); } catch(Exception error) { failed=true; Debug.LogException(error); }
            Debug.Log(failed?"UNIVERSE_CHECKS_FAILED":"UNIVERSE_CHECKS_PASSED");
            if(Application.isBatchMode) EditorApplication.Exit(failed?1:0); else EditorApplication.isPlaying=false;
        }
        private void Check()
        {
            owner=Object.FindFirstObjectByType<SolarSystemBootstrap>(); atlas=owner.Universe; cameraView=owner.CameraRig.GetComponent<Camera>();
            owner.CameraRig.enabled=false; atlas.AutomaticTravel=false; atlas.UI.AutomaticLayout=false; owner.Interface.AutomaticLayout=false;
            overlay=new GameObject("Atlas preview overlay",typeof(Camera)).GetComponent<Camera>(); overlay.enabled=false;
            overlay.clearFlags=CameraClearFlags.Depth; overlay.cullingMask=1<<5; overlay.nearClipPlane=0.01f; overlay.farClipPlane=1; overlay.fieldOfView=cameraView.fieldOfView;
            cameraView.cullingMask&=~(1<<5);
            foreach(var canvas in new[]{owner.Interface.Canvas,atlas.UI.Canvas})
            { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=overlay; canvas.planeDistance=0.1f; }
            string folder=Path.GetFullPath("Logs/UniversePreviews"); Directory.CreateDirectory(folder);
            foreach(var dimensions in new[]{new Vector2Int(1440,900),new Vector2Int(1920,1080),new Vector2Int(1024,640),new Vector2Int(390,844),new Vector2Int(844,390),new Vector2Int(1024,768),new Vector2Int(768,1024)})
            {
                target=new RenderTexture(dimensions.x,dimensions.y,24) {antiAliasing=4}; target.Create();
                cameraView.targetTexture=overlay.targetTexture=target; cameraView.aspect=overlay.aspect=(float)dimensions.x/dimensions.y;
                Rect safe=dimensions.x==390?new Rect(0,34,390,763):dimensions.x==844?new Rect(44,21,756,369):new Rect(0,0,dimensions.x,dimensions.y);
                owner.Interface.ApplyLayout(dimensions.x,dimensions.y,safe,dimensions.x<1100); atlas.UI.ApplyLayout(dimensions.x,dimensions.y,safe);
                owner.CameraRig.Snap(); Vector3 solarHome=cameraView.transform.position;
                owner.Interface.GalaxyButton.onClick.Invoke();
                Require(owner.InteractionLocked && atlas.IsCrossing,"Leaving the Solar System must lock input.");
                atlas.OpenSystem("trappist-1"); owner.SelectPlanet(2); owner.CameraRig.ZoomBy(2);
                Settle(); Require(atlas.View==UniverseView.MilkyWay && !owner.SolarWorld.activeSelf,"Milky Way view must replace the solar scene.");
                CheckUI(safe); Capture(folder,"milky-way",dimensions.x);
                var beacon=atlas.CurrentScene.Targets[0]; Physics.SyncTransforms();
                atlas.SelectAt(cameraView.WorldToScreenPoint(beacon.transform.position)); Settle();
                Require(atlas.View==UniverseView.Neighborhood,"The Milky Way's solar-neighborhood marker must be selectable.");
                Capture(folder,"nearby-stars",dimensions.x);
                atlas.OpenSystem("trappist-1"); Settle();
                Require(atlas.CurrentScene.Targets.Count==8 && atlas.InStarSystem,"TRAPPIST-1 must contain its star and seven planets.");
                CheckUI(safe); Capture(folder,"trappist-system",dimensions.x);
                foreach(var planet in atlas.Catalog.systems[0].planets)
                {
                    atlas.Select(planet.article.id);
                    Require(owner.InteractionLocked,"Exoplanet travel must lock input.");
                    string selected=atlas.SelectedId; atlas.Home(); atlas.Back(); atlas.Select("trappist-1");
                    Require(atlas.SelectedId==selected,"Travel must ignore navigation commands.");
                    Settle(); Require(atlas.View==UniverseView.Planet && atlas.SelectedId==planet.article.id,"Exoplanet selection must arrive at its destination.");
                    foreach(var guide in atlas.CurrentScene.OrbitGuides) Require(!guide.enabled,"Orbital guides must stay out of planet close-ups.");
                    TestTouch(); CheckUI(safe);
                    if(planet.article.id=="trappist-e") Capture(folder,"trappist-e",dimensions.x);
                }
                // The X restores the system, including its guides and direct world selection.
                atlas.UI.CloseButton.onClick.Invoke(); Settle();
                Require(atlas.View==UniverseView.StarSystem,"The planet X must return to its system.");
                foreach(var guide in atlas.CurrentScene.OrbitGuides) Require(guide.enabled,"System overview must restore its orbital guides.");
                var first=atlas.CurrentScene.Targets[1]; Physics.SyncTransforms(); atlas.SelectAt(cameraView.WorldToScreenPoint(first.transform.position)); Settle();
                Require(atlas.SelectedId==first.Id,"A rendered exoplanet must be selectable directly.");
                atlas.Back(); Settle(); atlas.Back(); Settle();
                Require(atlas.View==UniverseView.Neighborhood,"Back must restore the neighborhood.");
                atlas.OpenSystem("proxima-centauri"); Settle(); atlas.Select("proxima-b"); Settle(); TestTouch();
                Capture(folder,"proxima-b",dimensions.x);
                TestPause(); atlas.Home(); Settle();
                Require(atlas.View==UniverseView.SolarSystem && owner.SolarWorld.activeSelf && !owner.CameraRig.MapMode && !atlas.UI.Canvas.gameObject.activeSelf,"Home must fully restore the original Solar System.");
                Require(Vector3.Distance(solarHome,cameraView.transform.position)<0.02f,"Home must restore the original solar overview framing.");
                atlas.OpenMilkyWay(); Settle(); atlas.OpenLocalGroup(); Settle();
                Require(atlas.CurrentScene.Targets.Count==5,"Local Group must expose five galaxies.");
                CheckUI(safe); Capture(folder,"local-group",dimensions.x);
                foreach(var galaxy in atlas.Catalog.galaxies)
                {
                    atlas.OpenGalaxy(galaxy.id); Settle(); Require(atlas.SelectedId==galaxy.id,"Each galaxy must open its own article.");
                    if(galaxy.id=="andromeda" || galaxy.id=="large-magellanic-cloud") Capture(folder,galaxy.id,dimensions.x);
                    atlas.Back(); Settle(); Require(atlas.View==UniverseView.LocalGroup,"A galaxy's X/up action must return to the Local Group.");
                }
                owner.ToggleMotion(); atlas.OpenGalaxy("milky-way");
                Require(!owner.InteractionLocked && atlas.View==UniverseView.MilkyWay,"Instant motion must finish scale transitions immediately.");
                atlas.Home(); Require(!owner.InteractionLocked && atlas.View==UniverseView.SolarSystem,"Instant return home must also unlock immediately."); owner.ToggleMotion();
                Debug.Log("UNIVERSE_LAYOUT_PASSED: "+dimensions.x+"x"+dimensions.y+"; five galaxies, nearby stars, seven TRAPPIST-1 planets, Proxima b, touch orbit/pinch, source scrolling, travel locks, pause, instant travel and home restoration.");
                cameraView.targetTexture=overlay.targetTexture=null; target.Release(); Destroy(target);
            }
        }
        private void Settle()
        {
            for(int frame=0;frame<160;frame++)
            {
                atlas.AdvanceTravel(1f/60); owner.CameraRig.AdvanceMotion(1f/60);
                Vector3 position=cameraView.transform.position;
                Require(float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z),"Camera must remain finite.");
                if(atlas.InStarSystem && atlas.CurrentScene!=null) foreach(var body in atlas.CurrentScene.Targets)
                    Require(Vector3.Distance(position,body.transform.position)>body.Radius+0.05f,"Camera may not enter an exoplanet or its star.");
            }
            Require(!owner.InteractionLocked,"Atlas travel must finish in a bounded duration.");
            atlas.UI.Refresh(); atlas.UI.RefreshMarkers(); Canvas.ForceUpdateCanvases(); Physics.SyncTransforms();
        }
        private void TestTouch()
        {
            var input=owner.GetComponent<ExplorerInput>(); var rig=owner.CameraRig;
            Vector2 size=new Vector2(target.width,target.height),center=Vector2.Scale(rig.FocusViewport,size);
            input.ProcessTouches(Array.Empty<TouchContact>(),0,0,size);
            Quaternion before=cameraView.transform.rotation;
            Require(!ExplorerInput.OverInterface(center),"The selected exoplanet must be touchable clear of UI.");
            input.ProcessTouches(new[]{new TouchContact(0,center,TouchPhase.Began)},1,1,size);
            input.ProcessTouches(new[]{new TouchContact(0,center+Vector2.right*size.x*0.12f,TouchPhase.Moved)},1,1.2f,size);
            input.ProcessTouches(new[]{new TouchContact(0,center+Vector2.right*size.x*0.12f,TouchPhase.Ended)},1,1.3f,size);
            rig.AdvanceMotion(1); Require(Quaternion.Angle(before,cameraView.transform.rotation)>10,"One finger must rotate around an exoplanet.");
            input.ProcessTouches(new[]{new TouchContact(0,center-Vector2.right*20,TouchPhase.Began),new TouchContact(1,center+Vector2.right*20,TouchPhase.Began)},2,2,size);
            input.ProcessTouches(new[]{new TouchContact(0,center-Vector2.right*40,TouchPhase.Moved),new TouchContact(1,center+Vector2.right*40,TouchPhase.Moved)},2,2.2f,size);
            input.ProcessTouches(Array.Empty<TouchContact>(),0,2.3f,size);
            Require(rig.Zoom<0.6f,"Two fingers must zoom into an exoplanet.");
            rig.ZoomBy(0.001f); Require(rig.Zoom==1,"Exoplanet zoom-out must stop at the arrival distance.");
            rig.ResetView(); Settle();
        }
        private void CheckUI(Rect safe)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var button in new[]{atlas.UI.CloseButton,atlas.UI.PrimaryButton,atlas.UI.BackButton})
            {
                var corners=new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach(var corner in corners) { Vector2 point=RectTransformUtility.WorldToScreenPoint(overlay,corner); Require(safe.Contains(point),"Atlas controls must remain within the safe area."); }
            }
            Require(atlas.UI.SourceButton.transform.GetSiblingIndex()==atlas.UI.ArticleScroll.content.childCount-1,"NASA link must be the last article item.");
            atlas.UI.ArticleScroll.verticalNormalizedPosition=0; Canvas.ForceUpdateCanvases();
            Require(atlas.UI.ArticleScroll.viewport.rect.Contains(atlas.UI.ArticleScroll.viewport.InverseTransformPoint(atlas.UI.SourceButton.transform.position)),"The NASA link must be reachable by scrolling.");
            atlas.UI.ArticleScroll.verticalNormalizedPosition=1;
        }
        private void TestPause()
        {
            var surface=atlas.CurrentScene.Targets[1].SpinningSurface; Quaternion before=surface.rotation;
            owner.AdvanceWorldAnimation(2); Require(Quaternion.Angle(before,surface.rotation)>5,"Exoplanets must animate while idle.");
            owner.TogglePause(); before=surface.rotation; owner.AdvanceWorldAnimation(2); Require(Quaternion.Angle(before,surface.rotation)<0.001f,"Pause must freeze exoplanet animation."); owner.TogglePause();
        }
        private void Capture(string folder,string name,int width)
        {
            atlas.UI.RefreshMarkers(); Canvas.ForceUpdateCanvases();
            foreach(var canvas in new[]{owner.Interface.Canvas,atlas.UI.Canvas}) foreach(var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=5;
            cameraView.Render(); overlay.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+"-"+width+".png"),image.EncodeToPNG()); RenderTexture.active=previous; Destroy(image);
        }
        private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    }
}
