using System.Collections.Generic;
using UnityEngine;

namespace SolarExplorer
{
    // Bundled, attributed maps; no runtime network calls.
    public sealed class SpaceVisuals
    {
        private readonly List<Object> owned = new List<Object>();
        private readonly List<GameObject> orbits = new List<GameObject>();
        private Mesh sphereMesh;
        private GameObject sunBody, sunGlow;
        private Material previousSky, galaxySky;
        private bool showOrbits = true;
        private int hoveredPlanet=2;
        private float ambientTime;
        private Material debrisMaterial;
        private SolarActivity solarActivity;
        public float OrbitOpacity { get; private set; } = 1;
        public static readonly string[] TextureNames = { "8k_mercury", "4k_venus_atmosphere", "8k_earth_daymap", "8k_mars", "8k_jupiter", "8k_saturn", "2k_uranus", "2k_neptune", "8k_sun" };

        private Material Surface(PlanetData data, int kind)
        {
            var shader = Resources.Load<Shader>("PlanetSurface");
            if (shader == null) throw new System.InvalidOperationException("PlanetSurface shader is missing.");
            var mat = new Material(shader);
            mat.SetColor("_Color", data.Accent);
            Color secondary;
            ColorUtility.TryParseHtmlString(data.secondaryColor, out secondary);
            mat.SetColor("_Secondary", secondary);
            mat.SetFloat("_Kind", kind);
            var texture = Resources.Load<Texture2D>("Textures/" + TextureNames[kind]);
            if (texture != null) { mat.SetTexture("_MainTex",texture); mat.SetFloat("_UseTexture",1); }
            if (kind == 2)
            {
                mat.SetTexture("_NightTex",Resources.Load<Texture2D>("Textures/8k_earth_nightmap"));
                mat.SetTexture("_NormalTex",Resources.Load<Texture2D>("Textures/2k_earth_normal_map"));
                mat.SetTexture("_SpecularTex",Resources.Load<Texture2D>("Textures/2k_earth_specular_map"));
                mat.SetTexture("_CloudTex",Resources.Load<Texture2D>("Textures/8k_earth_clouds"));
            }
            if(kind==5) mat.SetTexture("_RingTex",Resources.Load<Texture2D>("Textures/8k_saturn_ring_alpha"));
            owned.Add(mat);
            return mat;
        }

        private Material Unlit(Color color)
        {
            var mat = new Material(Resources.Load<Shader>("SpaceUnlit"));
            mat.SetColor("_Color", color);
            owned.Add(mat);
            return mat;
        }

        public PlanetBody CreatePlanet(PlanetData data, int index, Vector3 position, Transform parent, PlanetAppearance appearance = null)
        {
            var root = new GameObject(data.name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            surface.name = "Mapped planet surface";
            surface.transform.SetParent(root.transform, false);
            surface.transform.localScale = Vector3.one * data.visualRadius * 2;
            surface.transform.localRotation = Quaternion.Euler(0, index == 2 ? 55 : index * 39, index == 6 ? 98 : 12);
            if(index==2) surface.transform.localRotation=Quaternion.Euler(25,0,-12)*Quaternion.Euler(0,55,0);
            if(index==4) surface.transform.localRotation=Quaternion.Euler(40,0,-10)*Quaternion.Euler(0,index*39,0);
            if(index==5)
            {
                surface.transform.localRotation=SolarLayout.SaturnTilt*Quaternion.Euler(0,index*39,0);
                surface.transform.localScale=Vector3.Scale(surface.transform.localScale,new Vector3(1,0.90f,1));
            }
            surface.GetComponent<MeshFilter>().sharedMesh = SphereMesh();
            var material = appearance != null && appearance.surfaceMaterial != null ? appearance.surfaceMaterial : Surface(data, index);
            if (appearance != null && appearance.surfaceMaterial == null && appearance.albedoTexture != null)
            {
                material.SetTexture("_MainTex", appearance.albedoTexture);
                material.SetFloat("_UseTexture", 1);
            }
            surface.GetComponent<Renderer>().sharedMaterial = material;
            var body = root.AddComponent<PlanetBody>();
            body.Initialize(data, index, surface.transform);
            if (index == 5) CreateRings(root.transform, data.visualRadius);
            if (index == 2 && (appearance == null || (appearance.albedoTexture == null && appearance.surfaceMaterial == null)))
            {
                var clouds = Layer("Earth cloud layer",surface.transform,1.008f,"EarthClouds");
                clouds.SetTexture("_MainTex",Resources.Load<Texture2D>("Textures/8k_earth_clouds"));
            }
            if (index != 0)
            {
                var atmosphere = Layer("Atmosphere",surface.transform,1.025f,"Atmosphere");
                Color scatter = index == 2 ? new Color(0.12f,0.46f,0.9f,0.42f) : new Color(data.Accent.r,data.Accent.g,data.Accent.b,0.18f);
                atmosphere.SetColor("_Color",scatter);
            }
            return body;
        }

        private Material Layer(string name,Transform parent,float scale,string shader)
        {
            var layer = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            layer.transform.SetParent(parent,false); layer.transform.localScale=Vector3.one*scale;
            layer.GetComponent<MeshFilter>().sharedMesh=SphereMesh();
            var material=new Material(Resources.Load<Shader>(shader)); owned.Add(material);
            layer.GetComponent<MeshRenderer>().sharedMaterial=material;
            return material;
        }

        public PlanetBody CreateSun(Transform parent, PlanetData data, GameObject visualPrefab = null)
        {
            var lightObject = new GameObject("Sun light", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1,0.92f,0.79f);
            light.intensity = 2; light.range = 300;
            if (visualPrefab != null)
            {
                sunBody = Object.Instantiate(visualPrefab, parent, false);
                sunBody.name = "Sun";
                sunBody.transform.localPosition = Vector3.zero;
                sunBody.transform.localScale = Vector3.one * SolarLayout.SunRadius;
                var collider = sunBody.AddComponent<SphereCollider>(); collider.radius = 1;
                var customBody = sunBody.AddComponent<PlanetBody>();
                customBody.Initialize(data, SolarSystemBootstrap.SunIndex, sunBody.transform);
                return customBody;
            }
            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunBody = sun;
            sun.name = "Sun";
            sun.transform.SetParent(parent, false);
            sun.transform.localPosition = Vector3.zero;
            sun.transform.localScale = Vector3.one * SolarLayout.SunRadius * 2;
            sun.GetComponent<MeshFilter>().sharedMesh = SphereMesh();
            sun.GetComponent<Renderer>().sharedMaterial = Surface(new PlanetData {
                color = "#FFBB4D", secondaryColor = "#FF7733"
            }, 8);
            var body = sun.AddComponent<PlanetBody>();
            body.Initialize(data, SolarSystemBootstrap.SunIndex, sun.transform);
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunGlow = glow;
            glow.name = "Solar corona"; glow.transform.SetParent(parent, false);
            glow.transform.localScale = Vector3.one * SolarLayout.SunRadius * 8;
            Object.Destroy(glow.GetComponent<Collider>());
            var glowMaterial = new Material(Resources.Load<Shader>("SunGlow"));
            owned.Add(glowMaterial);
            glow.GetComponent<Renderer>().sharedMaterial = glowMaterial;
            var activityRoot = new GameObject("Solar activity"); activityRoot.transform.SetParent(parent, false);
            solarActivity = activityRoot.AddComponent<SolarActivity>();
            var filament = new Material(Resources.Load<Shader>("SolarFilament")); owned.Add(filament);
            solarActivity.Initialize(filament);
            return body;
        }

        public void CreateOrbit(float radius, Color accent, Transform parent)
        {
            var orbit = new GameObject("Orbit " + radius, typeof(LineRenderer));
            orbit.transform.SetParent(parent, false);
            var line = orbit.GetComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 512;
            line.widthMultiplier = 0.65f;
            var material=new Material(Resources.Load<Shader>("OrbitGlow")); owned.Add(material);
            line.sharedMaterial=material; line.textureMode=LineTextureMode.Stretch;
            line.startColor = line.endColor = Color.white;
            for (int i=0;i<512;i++)
            {
                float a=i*Mathf.PI*2/512;
                line.SetPosition(i,SolarLayout.OrbitPoint(radius,a));
            }
            orbits.Add(orbit);
        }

        public void SetOrbitsVisible(bool visible)
        {
            showOrbits=visible;
        }

        public void SetOverviewVisibility(float visibility)
        {
            OrbitOpacity=showOrbits?visibility:0;
            foreach(var resource in owned)
                if(resource is Material material && material.HasProperty("_Overview")) material.SetFloat("_Overview",visibility);
            for(int i=0;i<orbits.Count;i++)
            {
                var material=orbits[i].GetComponent<LineRenderer>().sharedMaterial;
                material.SetFloat("_Opacity",OrbitOpacity);
                material.SetFloat("_Hover",i==hoveredPlanet?1:0);
            }
            if(debrisMaterial!=null) debrisMaterial.SetFloat("_Opacity",visibility);
        }

        public void SetHoveredPlanet(int index) { hoveredPlanet=index<0?2:index; }

        public void AdvanceAmbience(float deltaTime,bool stopped)
        {
            if(!stopped)
            {
                ambientTime+=deltaTime;
                if (sunBody != null) sunBody.transform.Rotate(Vector3.up, deltaTime * 3, Space.Self);
                if (solarActivity != null) solarActivity.SetTime(ambientTime);
            }
            foreach(var resource in owned)
                if(resource is Material material && material.HasProperty("_AmbientTime")) material.SetFloat("_AmbientTime",ambientTime);
        }

        public void CreateDebris(Transform parent)
        {
            var random=new System.Random(4187);
            var vertices=new List<Vector3>(); var normals=new List<Vector3>();
            var colors=new List<Color>(); var triangles=new List<int>();
            var centers=new List<Vector4>(); var spins=new List<Vector4>();
            float boundRadius=0, boundHeight=0;
            const float g=1.618034f;
            Vector3[] shape={new Vector3(-1,g,0),new Vector3(1,g,0),new Vector3(-1,-g,0),new Vector3(1,-g,0),
                new Vector3(0,-1,g),new Vector3(0,1,g),new Vector3(0,-1,-g),new Vector3(0,1,-g),
                new Vector3(g,0,-1),new Vector3(g,0,1),new Vector3(-g,0,-1),new Vector3(-g,0,1)};
            for(int i=0;i<shape.Length;i++) shape[i]=shape[i].normalized;
            int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            // A single mesh of small faceted rocks keeps the scene light enough for desktop.
            for(int rock=0;rock<3400;rock++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2;
                float radius=rock<2700?101+(float)random.NextDouble()*43:176+(float)random.NextDouble()*105;
                Vector3 center=SolarLayout.OrbitPoint(radius,a);
                center.y=((float)random.NextDouble()-0.5f)*9;
                float size=0.14f+Mathf.Pow((float)random.NextDouble(),3)*0.95f;
                if(rock>3350)
                {
                    center=new Vector3(-115+(float)random.NextDouble()*340,6+(float)random.NextDouble()*10,-105-(float)random.NextDouble()*55);
                    size=1.5f+(float)random.NextDouble()*2.8f;
                }
                Quaternion rotation=Quaternion.Euler((float)random.NextDouble()*360,(float)random.NextDouble()*360,(float)random.NextDouble()*360);
                Vector3 scale=new Vector3(size,size*(0.65f+(float)random.NextDouble()*0.6f),size);
                float shade=0.35f+(float)random.NextDouble()*0.5f;
                var roughShape=new Vector3[shape.Length];
                for(int v=0;v<shape.Length;v++) roughShape[v]=shape[v]*(0.72f+(float)random.NextDouble()*0.55f);
                // A separate seed preserves the art-directed initial distribution.
                var motionRandom=new System.Random(rock*7919+17);
                var axis=new Vector3((float)motionRandom.NextDouble()-0.5f,(float)motionRandom.NextDouble()-0.5f,(float)motionRandom.NextDouble()-0.5f).normalized;
                float speed=(0.010f+(float)motionRandom.NextDouble()*0.014f)*Mathf.Sqrt(120f/radius);
                var orbitData=new Vector4(center.x,center.y,center.z,speed);
                var spinData=new Vector4(axis.x,axis.y,axis.z,0.18f+(float)motionRandom.NextDouble()*0.42f);
                boundRadius=Mathf.Max(boundRadius,new Vector2(center.x,center.z/SolarLayout.DepthCompression).magnitude+size*3);
                boundHeight=Mathf.Max(boundHeight,Mathf.Abs(center.y)+size*3);
                for(int face=0;face<faces.Length;face+=3)
                {
                    Vector3 a0=rotation*Vector3.Scale(roughShape[faces[face]],scale)+center;
                    Vector3 b=rotation*Vector3.Scale(roughShape[faces[face+1]],scale)+center;
                    Vector3 c=rotation*Vector3.Scale(roughShape[faces[face+2]],scale)+center;
                    Vector3 normal=Vector3.Cross(b-a0,c-a0).normalized;
                    int start=vertices.Count;
                    vertices.Add(a0); vertices.Add(b); vertices.Add(c);
                    for(int v=0;v<3;v++)
                    {
                        normals.Add(normal); colors.Add(new Color(shade,shade*0.95f,shade*0.9f,1)); triangles.Add(start+v);
                        centers.Add(orbitData); spins.Add(spinData);
                    }
                }
            }
            var mesh=new Mesh {name="Ambient asteroid belt",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32}; owned.Add(mesh);
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            mesh.SetUVs(0,centers); mesh.SetUVs(1,spins);
            // Cover the entire animated orbit, so moving rocks aren't culled at their original bounds.
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(boundRadius*2,boundHeight*2,boundRadius*2*SolarLayout.DepthCompression));
            var belt=new GameObject("Asteroid belt",typeof(MeshFilter),typeof(MeshRenderer)); belt.transform.SetParent(parent,false);
            belt.GetComponent<MeshFilter>().sharedMesh=mesh;
            debrisMaterial=new Material(Resources.Load<Shader>("AsteroidSurface")); owned.Add(debrisMaterial);
            belt.GetComponent<MeshRenderer>().sharedMaterial=debrisMaterial;
        }

        public void SetSunVisible(bool visible)
        {
            if (sunBody != null) sunBody.SetActive(visible);
            if (sunGlow != null) sunGlow.SetActive(visible);
        }

        private Mesh SphereMesh()
        {
            if (sphereMesh != null) return sphereMesh;
            const int longitude=256, latitude=128;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>();
            var uv=new List<Vector2>(); var triangles=new List<int>();
            for(int y=0;y<=latitude;y++)
            for(int x=0;x<=longitude;x++)
            {
                float v=(float)y/latitude, u=(float)x/longitude;
                float a=u*Mathf.PI*2, b=v*Mathf.PI;
                var n=new Vector3(-Mathf.Sin(b)*Mathf.Sin(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Cos(a));
                vertices.Add(n*0.5f); normals.Add(n); uv.Add(new Vector2(u,1-v));
                if(y==latitude || x==longitude) continue;
                int i=y*(longitude+1)+x;
                triangles.AddRange(new[]{i,i+1,i+longitude+1,i+1,i+longitude+2,i+longitude+1});
            }
            sphereMesh=new Mesh { name="Smooth planet sphere" };
            sphereMesh.SetVertices(vertices); sphereMesh.SetNormals(normals); sphereMesh.SetUVs(0,uv);
            sphereMesh.SetTriangles(triangles,0); sphereMesh.RecalculateBounds(); owned.Add(sphereMesh);
            return sphereMesh;
        }

        private void CreateRings(Transform parent, float radius)
        {
            const int segments = 512;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int s = 0; s < segments; s++)
            {
                int start = vertices.Count;
                float a=s*Mathf.PI*2/segments, b=(s+1)*Mathf.PI*2/segments;
                float inner=radius*1.3f, outer=radius*2.3f;
                vertices.Add(new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner));
                vertices.Add(new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer));
                vertices.Add(new Vector3(Mathf.Cos(b)*outer,0,Mathf.Sin(b)*outer));
                vertices.Add(new Vector3(Mathf.Cos(b)*inner,0,Mathf.Sin(b)*inner));
                uv.Add(new Vector2(0,0)); uv.Add(new Vector2(1,0)); uv.Add(new Vector2(1,1)); uv.Add(new Vector2(0,1));
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var mesh = new Mesh { name = "Saturn rings" };
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            owned.Add(mesh);
            var ring = new GameObject("Rings", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(parent, false);
            ring.transform.localRotation = SolarLayout.SaturnTilt;
            ring.GetComponent<MeshFilter>().sharedMesh = mesh;
            var ringMaterial=new Material(Resources.Load<Shader>("PlanetRings")); owned.Add(ringMaterial);
            ringMaterial.SetTexture("_MainTex",Resources.Load<Texture2D>("Textures/8k_saturn_ring_alpha"));
            ringMaterial.SetVector("_Center",parent.position); ringMaterial.SetFloat("_Radius",radius);
            ring.GetComponent<MeshRenderer>().sharedMaterial=ringMaterial;
        }

        public void CreateStars(Transform parent)
        {
            previousSky=RenderSettings.skybox;
            galaxySky=new Material(Resources.Load<Shader>("GalaxySky")); owned.Add(galaxySky);
            galaxySky.SetTexture("_MainTex",Resources.Load<Texture2D>("Textures/eso0932a_milky_way"));
            RenderSettings.skybox=galaxySky;
            var random = new System.Random(1049);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var starUVs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < 1800; i++)
            {
                float z = (float)random.NextDouble() * 2 - 1;
                float a = (float)random.NextDouble() * Mathf.PI * 2;
                float r = Mathf.Sqrt(1-z*z);
                var normal = new Vector3(r*Mathf.Cos(a), z, r*Mathf.Sin(a));
                var center = normal * 850;
                var right = Vector3.Cross(normal, Vector3.up).normalized;
                var up = Vector3.Cross(right, normal);
                float size = 0.4f + Mathf.Pow((float)random.NextDouble(),5)*3.3f;
                float brightness = 0.7f + (float)random.NextDouble()*1.8f;
                var color = i%5==0?new Color(brightness,brightness*0.78f,brightness*0.53f,(float)random.NextDouble()):new Color(brightness*0.55f,brightness*0.78f,brightness,(float)random.NextDouble());
                int start = vertices.Count;
                vertices.Add(center-right*size-up*size);
                vertices.Add(center+right*size-up*size);
                vertices.Add(center+right*size+up*size);
                vertices.Add(center-right*size+up*size);
                for(int v=0;v<4;v++) colors.Add(color);
                starUVs.Add(Vector2.zero); starUVs.Add(Vector2.right); starUVs.Add(Vector2.one); starUVs.Add(Vector2.up);
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var mesh = new Mesh { name = "Seeded starfield" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0,starUVs); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            owned.Add(mesh);
            var stars = new GameObject("Starfield", typeof(MeshFilter), typeof(MeshRenderer));
            stars.transform.SetParent(parent, false);
            stars.GetComponent<MeshFilter>().sharedMesh = mesh;
            var starMaterial=new Material(Resources.Load<Shader>("StarGlint")); owned.Add(starMaterial);
            stars.GetComponent<MeshRenderer>().sharedMaterial = starMaterial;
        }

        public void Dispose()
        {
            if(RenderSettings.skybox==galaxySky) RenderSettings.skybox=previousSky;
            foreach (var resource in owned) if (resource != null) Object.Destroy(resource);
            owned.Clear();
        }
    }
}
