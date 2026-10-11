using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SolarExplorer
{
    public sealed class AtlasScene
    {
        public GameObject Root;
        public readonly List<AtlasTarget> Targets=new List<AtlasTarget>();
        public readonly List<LineRenderer> OrbitGuides=new List<LineRenderer>();
        public Vector3 Pivot;
        public float Radius;
        public Vector2 Angle=new Vector2(58,0);

        public void ShowOrbitGuides(bool visible)
        {
            foreach(var guide in OrbitGuides) guide.enabled=visible;
        }
    }

    // Original procedural artwork, deliberately separate from measured catalog data.
    public sealed class UniverseVisuals
    {
        private readonly List<Object> owned=new List<Object>();
        private readonly List<Material> animated=new List<Material>();
        private readonly List<Transform> spinning=new List<Transform>();
        private Mesh sphere;
        private readonly Transform parent;
        private float clock;

        public UniverseVisuals(Transform parent) { this.parent=parent; }

        public AtlasScene Galaxy(int index,AtlasEntry data)
        {
            var scene=NewScene(data.name,48);
            GalaxyArtwork(scene.Root.transform,Vector3.zero,64,index,15000);
            if(index==0)
            {
                var target=Marker(scene.Root.transform,"neighborhood",new Vector3(28,2,-18),1.15f,new Color(0.40f,1.3f,1.7f));
                scene.Targets.Add(target);
                var beam=new GameObject("Solar neighborhood beacon",typeof(LineRenderer)); beam.transform.SetParent(target.transform,false);
                var line=beam.GetComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=2;
                line.SetPosition(0,Vector3.zero); line.SetPosition(1,Vector3.up*8); line.widthMultiplier=0.20f;
                line.sharedMaterial=Flat(new Color(0.3f,0.85f,1,0.7f));
            }
            return scene;
        }

        public AtlasScene LocalGroup(AtlasEntry[] galaxies)
        {
            var scene=NewScene("Local Group",90); scene.Angle=new Vector2(68,0);
            var positions=new[]{new Vector3(-52,0,-40),new Vector3(55,8,52),new Vector3(93,1,-31),new Vector3(-81,-3,37),new Vector3(-30,6,68)};
            float[] radii={32,38,24,15,11};
            for(int i=0;i<5;i++)
            {
                var galaxy=GalaxyArtwork(scene.Root.transform,positions[i],radii[i],i,i<3?4500:1900);
                galaxy.transform.localRotation=Quaternion.Euler(i==1?24:i*4,i*37,i==2?-17:0);
                var target=galaxy.AddComponent<AtlasTarget>(); target.Id=galaxies[i].id; target.Radius=radii[i];
                galaxy.AddComponent<SphereCollider>().radius=radii[i]*0.86f; scene.Targets.Add(target);
            }
            return scene;
        }

        public AtlasScene Neighborhood()
        {
            var scene=NewScene("Solar neighborhood",36); scene.Angle=new Vector2(38,0);
            scene.Targets.Add(Body(scene.Root.transform,"solar",new Vector3(-29,0,-12),4,"#FFBA59","#742016",true,2));
            scene.Targets.Add(Body(scene.Root.transform,"proxima-centauri",new Vector3(19,0,-20),3,"#FF793D","#742016",true,5));
            scene.Targets.Add(Body(scene.Root.transform,"trappist-1",new Vector3(7,1,28),3.3f,"#FF6832","#742016",true,9));
            return scene;
        }

        public AtlasScene System(StarSystemEntry system)
        {
            var scene=NewScene(system.article.name,system.planets.Length>1?57:30); scene.Angle=new Vector2(59,0);
            scene.Targets.Add(Body(scene.Root.transform,system.article.id,Vector3.zero,9,"#FF7B38","#742016",true,1));
            for(int i=0;i<system.planets.Length;i++)
            {
                var planet=system.planets[i]; float orbit=system.planets.Length>1?22+i*8.1f:30;
                float angle=(system.planets.Length>1?(-155+i*58):125)*Mathf.Deg2Rad;
                Vector3 position=new Vector3(Mathf.Cos(angle)*orbit,0,Mathf.Sin(angle)*orbit);
                float radius=5.5f*Mathf.Clamp(planet.radiusEarth,0.75f,1.2f);
                scene.Targets.Add(Body(scene.Root.transform,planet.article.id,position,radius,planet.color,planet.secondaryColor,false,11+i*7));
                var orbitObject=new GameObject("Illustrative orbital guide",typeof(LineRenderer)); orbitObject.transform.SetParent(scene.Root.transform,false);
                var line=orbitObject.GetComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.positionCount=192;
                line.widthMultiplier=0.065f; line.sharedMaterial=Flat(new Color(0.11f,0.28f,0.36f,0.22f));
                for(int step=0;step<192;step++) { float a=step*Mathf.PI*2/192; line.SetPosition(step,new Vector3(Mathf.Cos(a)*orbit,0,Mathf.Sin(a)*orbit)); }
                scene.OrbitGuides.Add(line);
            }
            return scene;
        }

        private AtlasScene NewScene(string name,float radius)
        {
            var scene=new AtlasScene {Root=new GameObject(name),Radius=radius};
            scene.Root.transform.SetParent(parent,false); scene.Root.SetActive(false);
            var random=new System.Random(4021);
            var points=new List<Vector3>(); var colors=new List<Color>(); var sizes=new List<float>();
            for(int i=0;i<1500;i++)
            {
                float z=Range(random,-1,1),a=Range(random,0,Mathf.PI*2),r=Mathf.Sqrt(1-z*z);
                points.Add(new Vector3(r*Mathf.Cos(a),z,r*Mathf.Sin(a))*900);
                float intensity=Range(random,0.16f,0.48f); colors.Add(new Color(intensity*0.7f,intensity*0.85f,intensity)); sizes.Add(Range(random,0.35f,1.0f));
            }
            Points("Distant stars",scene.Root.transform,points,colors,sizes);
            return scene;
        }

        private GameObject GalaxyArtwork(Transform parent,Vector3 position,float radius,int kind,int count)
        {
            var galaxy=new GameObject("Galaxy artwork"); galaxy.transform.SetParent(parent,false); galaxy.transform.localPosition=position;
            var disk=new GameObject("Luminous disk and dust lanes",typeof(MeshFilter),typeof(MeshRenderer)); disk.transform.SetParent(galaxy.transform,false);
            var mesh=new Mesh {name="Galaxy disk"};
            mesh.vertices=new[]{new Vector3(-radius,0,-radius),new Vector3(-radius,0,radius),new Vector3(radius,0,radius),new Vector3(radius,0,-radius)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}; mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateBounds(); owned.Add(mesh);
            disk.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=Material("GalaxyDisk"); material.SetFloat("_Seed",kind+1); material.SetFloat("_Arms",kind==0?4:2); material.SetFloat("_Irregular",kind>=3?1:0);
            disk.GetComponent<MeshRenderer>().sharedMaterial=material;
            var random=new System.Random(3107+kind*59);
            var positions=new List<Vector3>(); var colors=new List<Color>(); var sizes=new List<float>();
            for(int i=0;i<count;i++)
            {
                float r=Mathf.Pow(Range(random,0.001f,1),0.70f)*0.96f;
                bool bulge=i<count/5 && kind<3;
                if(bulge) r=Mathf.Pow(Range(random,0,1),1.5f)*0.25f;
                int arms=kind==0?4:2;
                float angle=Mathf.Log(r+0.09f)*2.0f-(kind+1)+(i%arms)*Mathf.PI*2/arms+Range(random,-0.23f,0.23f);
                if(bulge || kind>=3 || i%7==0) angle=Range(random,0,Mathf.PI*2);
                float height=Gaussian(random)*(bulge?0.08f:0.018f)*(1-r);
                Vector3 point=new Vector3(Mathf.Cos(angle)*r,height,Mathf.Sin(angle)*r)*radius;
                if(kind>=3) { point.x*=0.68f; point.x+=Mathf.Sin(point.z/radius*8)*radius*0.09f; }
                positions.Add(point);
                float intensity=Range(random,0.10f,0.70f);
                Color color=Color.Lerp(new Color(0.34f,0.56f,1.1f),new Color(1.3f,0.93f,0.55f),Mathf.Exp(-r*6));
                if(i%31==0 && !bulge) color=new Color(1.0f,0.27f,0.45f);
                colors.Add(color*intensity);
                sizes.Add(radius*Range(random,0.0018f,0.006f)*(i%97==0?2.4f:1));
            }
            Points("Resolved starlight",galaxy.transform,positions,colors,sizes);
            return galaxy;
        }

        private void Points(string name,Transform parent,List<Vector3> positions,List<Color> colors,List<float> sizes)
        {
            int count=positions.Count;
            var vertices=new Vector3[count*4]; var uv=new Vector2[count*4]; var shape=new Vector2[count*4]; var tint=new Color[count*4]; var indices=new int[count*6];
            Vector2[] corners={new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)};
            for(int i=0;i<count;i++)
            {
                for(int c=0;c<4;c++) { int at=i*4+c; vertices[at]=positions[i]; uv[at]=corners[c]; shape[at]=new Vector2(sizes[i],i*1.37f); tint[at]=colors[i]; }
                int v=i*4,t=i*6; indices[t]=v; indices[t+1]=v+1; indices[t+2]=v+2; indices[t+3]=v; indices[t+4]=v+2; indices[t+5]=v+3;
            }
            var mesh=new Mesh {name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.vertices=vertices; mesh.uv=uv; mesh.uv2=shape; mesh.colors=tint; mesh.triangles=indices; mesh.RecalculateBounds();
            var bounds=mesh.bounds; bounds.Expand(8); mesh.bounds=bounds; owned.Add(mesh);
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(parent,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=Material("GalaxyStars"); animated.Add(material); obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        private AtlasTarget Marker(Transform parent,string id,Vector3 position,float radius,Color color)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name=id; obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=Vector3.one*radius*2;
            obj.GetComponent<Renderer>().sharedMaterial=Flat(color);
            var target=obj.AddComponent<AtlasTarget>(); target.Id=id; target.Radius=radius; return target;
        }

        private AtlasTarget Body(Transform parent,string id,Vector3 position,float radius,string color,string secondary,bool star,int seed)
        {
            var root=new GameObject(id); root.transform.SetParent(parent,false); root.transform.localPosition=position;
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name="Illustrated surface"; obj.transform.SetParent(root.transform,false); obj.transform.localScale=Vector3.one*radius*2;
            obj.GetComponent<MeshFilter>().sharedMesh=Sphere();
            var material=Material("ExoplanetSurface"); ColorUtility.TryParseHtmlString(color,out var primary); ColorUtility.TryParseHtmlString(secondary,out var dark);
            material.SetColor("_Color",primary); material.SetColor("_Secondary",dark); material.SetFloat("_Star",star?1:0); material.SetFloat("_Seed",seed);
            obj.GetComponent<Renderer>().sharedMaterial=material; animated.Add(material); spinning.Add(obj.transform);
            var target=root.AddComponent<AtlasTarget>(); target.Id=id; target.Radius=radius; target.SpinningSurface=obj.transform;
            return target;
        }

        private Mesh Sphere()
        {
            if(sphere!=null) return sphere;
            const int longitude=128,latitude=64; var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            for(int y=0;y<=latitude;y++) for(int x=0;x<=longitude;x++)
            {
                float a=x*Mathf.PI*2/longitude,b=y*Mathf.PI/latitude;
                var n=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)); vertices.Add(n*0.5f); normals.Add(n); uv.Add(new Vector2((float)x/longitude,1-(float)y/latitude));
            }
            for(int y=0;y<latitude;y++) for(int x=0;x<longitude;x++) { int a=y*(longitude+1)+x,b=a+longitude+1; triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b}); }
            sphere=new Mesh {name="Exoplanet sphere"}; sphere.SetVertices(vertices); sphere.SetNormals(normals); sphere.SetUVs(0,uv); sphere.SetTriangles(triangles,0); sphere.RecalculateBounds(); owned.Add(sphere); return sphere;
        }

        private Material Material(string shader) { var material=new Material(Resources.Load<Shader>(shader)); owned.Add(material); return material; }
        private Material Flat(Color color) { var material=Material("SpaceUnlit"); material.SetColor("_Color",color); return material; }
        private static float Range(System.Random random,float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
        private static float Gaussian(System.Random random) => (Range(random,0,1)+Range(random,0,1)+Range(random,0,1)-1.5f)*1.5f;
        public void Advance(float delta)
        {
            clock+=delta; foreach(var material in animated) material.SetFloat("_Clock",clock);
            foreach(var surface in spinning) if(surface.gameObject.activeInHierarchy) surface.Rotate(Vector3.up,delta*5,Space.Self);
        }
        public void Dispose() { foreach(var item in owned) if(item!=null) Object.Destroy(item); }
    }
}
