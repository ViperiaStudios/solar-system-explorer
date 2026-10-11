using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SolarExplorer.Editor
{
    [InitializeOnLoad]
    public static class SceneSmokeChecks
    {
        private const string ActiveKey="SolarExplorer.SmokeChecks";
        internal const string SunPreviewKey="SolarExplorer.SunPreview";
        static SceneSmokeChecks() { EditorApplication.playModeStateChanged += OnModeChanged; }

        [MenuItem("Solar Explorer/Run Scene Checks and Previews")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before running scene checks.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ProjectChecks.Validate();
            EditorSceneManager.OpenScene(ProjectChecks.ScenePath);
            SessionState.SetBool(ActiveKey,true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Solar Explorer/Render Sun Activity Preview")]
        public static void RenderSun()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before rendering a preview.");
            SessionState.SetBool(SunPreviewKey,true);
            Run();
        }

        private static void OnModeChanged(PlayModeStateChange mode)
        {
            if(mode!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey,false)) return;
            SessionState.SetBool(ActiveKey,false);
            new GameObject("Scene smoke checks").AddComponent<SceneSmokeDriver>();
        }
    }

    public sealed class SceneSmokeDriver : MonoBehaviour
    {
        private bool failed;
        private Camera overlayCamera;
        private void Awake() { Application.logMessageReceived += OnLog; }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
        private void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true;
        }

        private IEnumerator Start()
        {
            yield return null;
            try
            {
                bool sunPreview=SessionState.GetBool(SceneSmokeChecks.SunPreviewKey,false);
                SessionState.SetBool(SceneSmokeChecks.SunPreviewKey,false);
                if(sunPreview) CaptureSun(); else Check();
            }
            catch(Exception exception) { failed=true; Debug.LogException(exception); }
            Debug.Log(failed?"SOLAR_SCENE_CHECKS_FAILED":"SOLAR_SCENE_CHECKS_PASSED");
            if(Application.isBatchMode) EditorApplication.Exit(failed?1:0);
            else EditorApplication.isPlaying=false;
        }

        private void Check()
        {
            var explorer=Object.FindFirstObjectByType<SolarSystemBootstrap>();
            Require(explorer!=null && explorer.Planets.Length==8,"Eight planets must spawn.");
            var camera=explorer.CameraRig.GetComponent<Camera>();
            var canvas=explorer.Interface.Canvas;
            explorer.Interface.AutomaticLayout=false;
            InteractionChecks.Gestures();
            // Render the UI after post-processing, matching the runtime overlay canvas.
            overlayCamera=new GameObject("Preview overlay camera",typeof(Camera)).GetComponent<Camera>();
            overlayCamera.enabled=false; overlayCamera.clearFlags=CameraClearFlags.Depth;
            overlayCamera.cullingMask=1<<5; overlayCamera.nearClipPlane=0.01f; overlayCamera.farClipPlane=1;
            overlayCamera.fieldOfView=camera.fieldOfView;
            camera.cullingMask&=~(1<<5);
            foreach(var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=5;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=overlayCamera; canvas.planeDistance=0.1f;
            string folder=Path.GetFullPath("Logs/Previews"); Directory.CreateDirectory(folder);
            foreach(var dimensions in new[]{new Vector2Int(1440,900),new Vector2Int(1920,1080),new Vector2Int(1024,640),new Vector2Int(390,844),new Vector2Int(844,390),new Vector2Int(1024,768),new Vector2Int(768,1024)})
            {
                var texture=new RenderTexture(dimensions.x,dimensions.y,24) { antiAliasing=4 };
                texture.Create(); camera.targetTexture=texture; camera.aspect=(float)dimensions.x/dimensions.y;
                overlayCamera.targetTexture=texture; overlayCamera.aspect=camera.aspect;
                // Match the reference canvas to the offscreen target, independent of batch Game view size.
                var scaler=canvas.GetComponent<CanvasScaler>(); scaler.enabled=false;
                bool touch=dimensions.x==390 || dimensions.x==844 || dimensions.y==768 || dimensions.x==768;
                Rect safe=dimensions.x==390?new Rect(0,34,390,763):(dimensions.x==844?new Rect(44,21,756,369):new Rect(0,0,dimensions.x,dimensions.y));
                explorer.Interface.ApplyLayout(dimensions.x,dimensions.y,safe,touch);
                explorer.Overview(); explorer.CameraRig.Snap();
                Canvas.ForceUpdateCanvases(); explorer.Interface.RefreshProjectedLabels();
                Physics.SyncTransforms();
                foreach(var planet in explorer.Bodies)
                {
                    Vector3 p=camera.WorldToViewportPoint(planet.transform.position);
                    Require(p.z>0 && p.x>0.05f && p.x<0.95f && p.y>0.2f && p.y<0.92f,"Overview framing: "+planet.Data.name);
                    RaycastHit hit;
                    Require(Physics.Raycast(camera.ViewportPointToRay(p),out hit,1400) && hit.collider.GetComponentInParent<PlanetBody>()==planet,"Planet must be clickable: "+planet.Data.name);
                }
                Capture(camera,texture,Path.Combine(folder,"overview-"+dimensions.x+".png"));
                CheckLabels(explorer,camera,canvas);
                if(dimensions.x==1920)
                {
                    CheckCinematicFraming(explorer,camera);
                    CheckLiveMotion(explorer,camera,texture,folder);
                }
                if(dimensions.x==1440) { CheckOverviewState(explorer); CheckCameraTravel(explorer); }
                explorer.ToggleView(); explorer.CameraRig.Snap();
                Canvas.ForceUpdateCanvases(); explorer.Interface.RefreshProjectedLabels();
                Capture(camera,texture,Path.Combine(folder,"overhead-"+dimensions.x+".png"));
                CheckLabels(explorer,camera,canvas);
                explorer.ToggleView(); explorer.CameraRig.Snap();
                for(int i=0;i<9;i++)
                {
                    explorer.SelectPlanet(i); explorer.CameraRig.Snap();
                    Canvas.ForceUpdateCanvases(); explorer.Interface.RefreshProjectedLabels();
                    var p=camera.WorldToViewportPoint(explorer.Bodies[i].transform.position);
                    Require(Vector2.Distance(p,explorer.CameraRig.FocusViewport)<0.02f,"Focus framing: "+i);
                    Require(explorer.Interface.GetComponentInChildren<Text>(true)!=null,"UI missing.");
                    InteractionChecks.Article(explorer,overlayCamera,safe);
                    if(i==5) CheckSaturnFraming(explorer,camera);
                    Capture(camera,texture,Path.Combine(folder,explorer.Bodies[i].Data.name.ToLowerInvariant()+"-"+dimensions.x+".png"));
                    if(i==5)
                    {
                        explorer.CameraRig.ZoomBy(1.65f); explorer.CameraRig.AdvanceMotion(1);
                        Capture(camera,texture,Path.Combine(folder,"saturn-detail-"+dimensions.x+".png"));
                        explorer.CameraRig.ResetView(); explorer.CameraRig.Snap();
                    }
                }
                InteractionChecks.Controls(explorer,overlayCamera);
                InteractionChecks.NativeTouchOrbit(explorer,overlayCamera);
                explorer.TogglePause();
                foreach(var planet in explorer.Planets) Require(!planet.Rotating,"Pause must stop rotation.");
                explorer.TogglePause();
                camera.targetTexture=null; overlayCamera.targetTexture=null; texture.Release(); Destroy(texture);
            }
            Debug.Log("Checked nine celestial bodies, selection rays, camera framing, article scrolling, touch controls, safe areas, seven desktop/mobile layouts, and rendered previews.");
        }

        private void CaptureSun()
        {
            var explorer=Object.FindFirstObjectByType<SolarSystemBootstrap>();
            var camera=explorer.CameraRig.GetComponent<Camera>(); var ui=explorer.Interface;
            var target=new RenderTexture(1920,1080,24) { antiAliasing=4 }; target.Create();
            camera.targetTexture=target; camera.aspect=1920f/1080;
            overlayCamera=new GameObject("Preview overlay camera",typeof(Camera)).GetComponent<Camera>();
            overlayCamera.enabled=false; overlayCamera.clearFlags=CameraClearFlags.Depth;
            overlayCamera.cullingMask=1<<5; overlayCamera.nearClipPlane=0.01f; overlayCamera.farClipPlane=1;
            overlayCamera.fieldOfView=camera.fieldOfView; overlayCamera.targetTexture=target; overlayCamera.aspect=camera.aspect;
            camera.cullingMask&=~(1<<5); ui.AutomaticLayout=false;
            ui.Canvas.renderMode=RenderMode.ScreenSpaceCamera; ui.Canvas.worldCamera=overlayCamera; ui.Canvas.planeDistance=0.1f;
            ui.ApplyLayout(1920,1080,new Rect(0,0,1920,1080),false);
            explorer.SelectPlanet(8); explorer.CameraRig.Snap(); explorer.RefreshVisuals();
            var material=explorer.Bodies[8].GetComponent<Renderer>().sharedMaterial;
            float clock=material.GetFloat("_AmbientTime");
            var before=ReadScenePixels(camera,target);
            material.SetFloat("_AmbientTime",clock+3);
            var after=ReadScenePixels(camera,target);
            int changed=0;
            for(int i=0;i<before.Length;i++) if(PixelDifference(before[i],after[i])>25) changed++;
            material.SetFloat("_AmbientTime",clock);
            Require(changed>10000,"Solar plasma must visibly churn even with its sphere rotation held fixed.");
            Debug.Log("SOLAR_PLASMA_PASSED: "+changed+" changed rendered pixels with fixed sphere rotation.");
            CheckSolarLoops(explorer,camera,target);
            string folder=Path.GetFullPath("Logs/Previews/SolarMotion"); Directory.CreateDirectory(folder);
            for(int frame=0;frame<180;frame++)
            {
                Capture(camera,target,Path.Combine(folder,"frame-"+frame.ToString("D4")+".png"));
                explorer.AdvanceWorldAnimation(1f/30);
            }
            camera.targetTexture=null; overlayCamera.targetTexture=null; target.Release(); Destroy(target);
            Debug.Log("SOLAR_PREVIEW_CAPTURED: 180 frames of live Sun activity at 1920x1080.");
        }

        private void CheckSolarLoops(SolarSystemBootstrap explorer,Camera camera,RenderTexture target)
        {
            var sun=explorer.Bodies[8]; var material=sun.GetComponent<Renderer>().sharedMaterial;
            var renderers=explorer.GetComponentInChildren<SolarActivity>().GetComponentsInChildren<LineRenderer>();
            var layers=new int[renderers.Length];
            int mask=camera.cullingMask, sunLayer=sun.gameObject.layer;
            var clear=camera.clearFlags; var background=camera.backgroundColor;
            Quaternion fixedRotation=sun.transform.rotation;
            float fixedClock=material.GetFloat("_AmbientTime");
            camera.cullingMask=1<<30; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            sun.gameObject.layer=30;
            for(int i=0;i<renderers.Length;i++) { layers[i]=renderers[i].gameObject.layer; renderers[i].gameObject.layer=30; }
            var before=ReadScenePixels(camera,target);
            explorer.AdvanceWorldAnimation(0.45f);
            Quaternion nextRotation=sun.transform.rotation; float nextClock=material.GetFloat("_AmbientTime");
            // Hold the sphere completely still; only the prominence render can change.
            sun.transform.rotation=fixedRotation; material.SetFloat("_AmbientTime",fixedClock);
            var after=ReadScenePixels(camera,target); int changed=0;
            for(int i=0;i<before.Length;i++) if(PixelDifference(before[i],after[i])>25) changed++;
            Require(changed>250,"Prominences must visibly change within half a second, independently of Sun rotation and surface activity.");
            explorer.TogglePause(); explorer.AdvanceWorldAnimation(2);
            var paused=ReadScenePixels(camera,target); int changedPaused=0;
            for(int i=0;i<after.Length;i++) if(PixelDifference(after[i],paused[i])>2) changedPaused++;
            Require(changedPaused==0,"Paused prominence image must remain unchanged.");
            explorer.TogglePause(); sun.transform.rotation=nextRotation; material.SetFloat("_AmbientTime",nextClock);
            sun.gameObject.layer=sunLayer;
            for(int i=0;i<renderers.Length;i++) renderers[i].gameObject.layer=layers[i];
            camera.cullingMask=mask; camera.clearFlags=clear; camera.backgroundColor=background;
            Debug.Log("SOLAR_LOOPS_PASSED: "+changed+" changed pixels in 0.45 seconds with fixed sphere/surface; identical paused render.");
        }

        private void CheckLiveMotion(SolarSystemBootstrap explorer,Camera camera,RenderTexture target,string folder)
        {
            explorer.Overview(); explorer.CameraRig.Snap(); explorer.RefreshVisuals();
            var sun=explorer.SolarWorld.transform.Find("Sun");
            var belt=explorer.SolarWorld.transform.Find("Asteroid belt").gameObject;
            var material=belt.GetComponent<Renderer>().sharedMaterial;
            var rotations=new Quaternion[8];
            for(int i=0;i<8;i++) rotations[i]=explorer.Planets[i].Surface.localRotation;
            Quaternion sunRotation=sun.localRotation;
            var loop=explorer.GetComponentInChildren<SolarActivity>().GetComponentInChildren<LineRenderer>();
            Vector3 loopBefore=loop.GetPosition(loop.positionCount/2);
            explorer.AdvanceWorldAnimation(2);
            for(int i=0;i<8;i++) Require(Quaternion.Angle(rotations[i],explorer.Planets[i].Surface.localRotation)>12,"Idle planet must visibly rotate: "+explorer.Planets[i].Data.name);
            Require(Vector3.Distance(loopBefore,loop.GetPosition(loop.positionCount/2))>0.05f,"Solar loops must grow and change shape.");
            Require(Quaternion.Angle(sunRotation,sun.localRotation)>5,"Sun must rotate while idle.");

            int mask=camera.cullingMask,layer=belt.layer;
            var clear=camera.clearFlags; var background=camera.backgroundColor;
            belt.layer=30; camera.cullingMask=1<<30; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            Color32[] before=ReadScenePixels(camera,target);
            explorer.AdvanceWorldAnimation(3);
            Color32[] after=ReadScenePixels(camera,target);
            int changed=0;
            for(int i=0;i<before.Length;i++) if(PixelDifference(before[i],after[i])>25) changed++;
            Require(changed>1000,"The rendered asteroid belt must visibly move, not just advance its clock.");
            explorer.TogglePause();
            float stoppedClock=material.GetFloat("_AmbientTime");
            for(int i=0;i<8;i++) rotations[i]=explorer.Planets[i].Surface.localRotation;
            sunRotation=sun.localRotation; loopBefore=loop.GetPosition(loop.positionCount/2);
            explorer.AdvanceWorldAnimation(3);
            var paused=ReadScenePixels(camera,target);
            int pausedChanges=0;
            for(int i=0;i<after.Length;i++) if(PixelDifference(after[i],paused[i])>2) pausedChanges++;
            Require(pausedChanges==0 && stoppedClock==material.GetFloat("_AmbientTime"),"Pause must freeze the actual rendered asteroid positions.");
            Require(loopBefore==loop.GetPosition(loop.positionCount/2),"Pause must freeze solar loop geometry.");
            Require(Quaternion.Angle(sunRotation,sun.localRotation)<0.001f,"Pause must stop the Sun.");
            for(int i=0;i<8;i++) Require(Quaternion.Angle(rotations[i],explorer.Planets[i].Surface.localRotation)<0.001f,"Pause must stop planet rotation.");
            explorer.TogglePause(); explorer.ToggleMotion();
            explorer.AdvanceWorldAnimation(1);
            Require(material.GetFloat("_AmbientTime")>stoppedClock+0.9f,"Instant camera mode must leave the world alive.");
            for(int i=0;i<8;i++) Require(Quaternion.Angle(rotations[i],explorer.Planets[i].Surface.localRotation)>6,"Resume must restart every planet.");
            explorer.ToggleMotion();
            belt.layer=layer; camera.cullingMask=mask; camera.clearFlags=clear; camera.backgroundColor=background;
            Capture(camera,target,Path.Combine(folder,"overview-moving-1920.png"));
            if(Environment.GetEnvironmentVariable("SOLAR_CAPTURE_MOTION")=="1")
            {
                string frames=Path.Combine(folder,"Motion"); Directory.CreateDirectory(frames);
                for(int frame=0;frame<180;frame++)
                {
                    Capture(camera,target,Path.Combine(frames,"frame-"+frame.ToString("D4")+".png"));
                    explorer.AdvanceWorldAnimation(1f/30);
                }
            }
            Debug.Log("LIVE_MOTION_PASSED: eight rotating planets, rotating Sun, "+changed+" changed asteroid pixels over 3 seconds, exact paused frame, resume and instant-camera independence.");
        }

        private static int PixelDifference(Color32 a,Color32 b)
        {
            return Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        }

        private static Color32[] ReadScenePixels(Camera camera,RenderTexture target)
        {
            camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            var pixels=image.GetPixels32(); DestroyImmediate(image); RenderTexture.active=previous;
            return pixels;
        }

        private void Capture(Camera camera,RenderTexture target,string path)
        {
            Object.FindFirstObjectByType<SolarSystemBootstrap>().RefreshVisuals();
            Object.FindFirstObjectByType<SolarSystemBootstrap>().Interface.RefreshProjectedLabels();
            Canvas.ForceUpdateCanvases();
            foreach(var panel in Object.FindFirstObjectByType<SolarSystemBootstrap>().Interface.Canvas.GetComponentsInChildren<HudPanel>())
            {
                if(panel.canvasRenderer.cull) continue; // Offscreen scroll content is intentionally clipped.
                var mesh=panel.canvasRenderer.GetMesh();
                Require(mesh!=null && mesh.vertexCount>0,"HUD panel must generate visible geometry: "+panel.name);
            }
            // Facts are rebuilt on selection, so include newly-created descendants too.
            foreach(var child in Object.FindFirstObjectByType<SolarSystemBootstrap>().Interface.Canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=5;
            camera.Render(); overlayCamera.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG()); RenderTexture.active=previous; Destroy(image);
        }

        private static void CheckSaturnFraming(SolarSystemBootstrap explorer,Camera camera)
        {
            var rig=explorer.CameraRig; var saturn=explorer.Planets[5]; float radius=saturn.Data.visualRadius;
            Rect freeRegion=rig.Portrait?Rect.MinMaxRect(0.025f,0.49f,0.975f,0.83f):Rect.MinMaxRect(0.015f,0.14f,0.65f,0.86f);
            for(int sample=0;sample<128;sample++)
            {
                float angle=sample*Mathf.PI*2/128;
                Vector3 ring=SolarLayout.SaturnTilt*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*2.3f;
                Vector3 point=camera.WorldToViewportPoint(saturn.transform.position+ring);
                Require(point.z>0 && freeRegion.Contains(point),"Saturn's complete rings must fit clear of its article and controls.");
            }
            Vector3 center=camera.WorldToViewportPoint(saturn.transform.position);
            Vector3 edge=camera.WorldToViewportPoint(saturn.transform.position+camera.transform.up*radius);
            float diameter=Mathf.Abs(edge.y-center.y)*2;
            Require(diameter>(rig.Portrait?0.175f:0.31f),"Saturn must have a substantially closer default view.");
            Vector3 initial=camera.transform.position;
            float originalDistance=Vector3.Distance(initial,saturn.transform.position);
            rig.ZoomBy(1.65f); rig.AdvanceMotion(1);
            Require(Vector3.Distance(camera.transform.position,saturn.transform.position)<originalDistance*0.63f,"Saturn's inspection zoom must visibly move closer.");
            rig.ZoomBy(0.001f); rig.AdvanceMotion(1);
            Require(rig.Zoom==1 && Vector3.Distance(initial,camera.transform.position)<0.02f,"Saturn zoom-out must return to, and stop at, its closer default view.");
            rig.ResetView(); rig.Snap();
            // The provider's 8k_saturn filename contains a 4096-wide source image.
            Require(Resources.Load<Texture2D>("Textures/8k_saturn").width>=4096,"Saturn must keep its full-resolution surface map.");
            Require(Resources.Load<Texture2D>("Textures/8k_saturn_ring_alpha").width>=8192,"Saturn must keep its detailed ring profile.");
            Debug.Log("SATURN_FOCUS_PASSED: "+camera.pixelWidth+"x"+camera.pixelHeight+"; visible rings, body diameter "+diameter.ToString("F3")+", closer inspection zoom, capped zoom-out, 4K surface and 8K rings.");
        }

        private static void CheckCinematicFraming(SolarSystemBootstrap explorer,Camera camera)
        {
            Vector3 sun=camera.WorldToViewportPoint(Vector3.zero);
            Require(Mathf.Abs(sun.x-0.335f)<0.025f && Mathf.Abs(sun.y-0.68f)<0.025f,"Sun must keep the approved close composition.");
            foreach(int index in new[]{2,4})
            {
                var planet=explorer.Planets[index];
                var center=camera.WorldToViewportPoint(planet.transform.position);
                var edge=camera.WorldToViewportPoint(planet.transform.position+camera.transform.up*planet.Data.visualRadius);
                float height=2*Mathf.Abs(edge.y-center.y);
                Require(height>(index==2?0.18f:0.32f),"Hero planets must retain their close overview size: "+planet.Data.name);
            }
            Require(QualitySettings.activeColorSpace==ColorSpace.Linear,"Cinematic lighting requires linear color.");
            Require(Resources.Load<Texture2D>("Textures/8k_earth_daymap").width>=8192,"Earth must retain its high-resolution map.");
            // The provider names this file 8k_jupiter, but its actual width is 4096.
            Require(Resources.Load<Texture2D>("Textures/8k_jupiter").width>=4096,"Jupiter must retain its high-resolution map.");
            Debug.Log("Cinematic composition verified: close Sun/Earth/Jupiter framing, linear light, and full-resolution 4K/8K maps.");
        }

        private static void CheckOverviewState(SolarSystemBootstrap explorer)
        {
            explorer.SetUIHover(0); explorer.Interface.RefreshProjectedLabels();
            Require(explorer.Interface.LayoutRoot.Find("Mercury").gameObject.activeSelf,"Hover must reveal a minor planet label.");
            explorer.SetUIHover(1); explorer.Interface.RefreshProjectedLabels();
            Require(explorer.HoveredIndex==1,"Hiding the previous label must not erase the new hover.");
            explorer.ClearUIHover(1); explorer.Interface.RefreshProjectedLabels();
            explorer.ToggleOrbits(); explorer.RefreshVisuals();
            Require(explorer.OrbitOpacity==0,"Orbit toggle must hide the guides.");
            explorer.SelectPlanet(2); explorer.CameraRig.Snap(); explorer.Overview(); explorer.CameraRig.Snap(); explorer.RefreshVisuals();
            Require(explorer.OrbitOpacity==0,"Hidden orbits must stay hidden after returning.");
            explorer.ToggleOrbits(); explorer.RefreshVisuals();
            Require(explorer.OrbitOpacity>0.99f,"Orbit toggle must restore the guides.");
            explorer.SelectPlanet(2); explorer.CameraRig.AdvanceMotion(0.825f); explorer.RefreshVisuals();
            Require(explorer.OrbitOpacity>0.1f && explorer.OrbitOpacity<0.9f,"Orbital guides must fade during camera travel.");
            explorer.CameraRig.Snap(); explorer.RefreshVisuals();
            Require(explorer.OrbitOpacity<0.01f,"Orbital guides must clear the focused view.");
            explorer.Overview(); explorer.CameraRig.Snap(); explorer.RefreshVisuals();
            Debug.Log("Overview state verified: hover handoff, orbit toggle persistence and continuous guide fading.");
        }

        private static void CheckCameraTravel(SolarSystemBootstrap explorer)
        {
            var rig=explorer.CameraRig;
            rig.ReduceMotion=false;
            Vector3 home=rig.transform.position;
            Quaternion heading=rig.transform.rotation;
            for(int i=0;i<9;i++)
            {
                Vector3 start=rig.transform.position;
                explorer.SelectPlanet(i);
                Require(Vector3.Distance(start,rig.transform.position)<0.001f,"Selection must not teleport the camera.");
                SampleFlight(explorer);
                Vector3 framed=rig.GetComponent<Camera>().WorldToViewportPoint(explorer.Bodies[i].transform.position);
                Require(Mathf.Abs(framed.x-0.33f)<0.01f,"Flight must arrive at focus framing.");
                foreach(var planet in explorer.Planets) Require(planet.gameObject.activeSelf,"Worlds must remain present during travel.");
                explorer.Overview(); SampleFlight(explorer);
                Require(Vector3.Distance(home,rig.transform.position)<0.01f && Quaternion.Angle(heading,rig.transform.rotation)<0.01f,"Return must restore the overview pose.");
            }
            explorer.SelectPlanet(2); rig.AdvanceMotion(0.6f);
            Vector3 interrupted=rig.transform.position;
            explorer.SelectPlanet(5); rig.AdvanceMotion(0);
            Require(Vector3.Distance(interrupted,rig.transform.position)<0.001f && explorer.SelectedIndex==2,"A new selection during flight must be ignored.");
            SampleFlight(explorer);
            explorer.Overview(); SampleFlight(explorer);
            for(int from=0;from<9;from++)
            for(int to=0;to<9;to++)
            {
                if(from==to) continue;
                explorer.SelectPlanet(from); rig.Snap();
                explorer.SelectPlanet(to); SampleFlight(explorer);
            }
            explorer.Overview(); SampleFlight(explorer);
            rig.ReduceMotion=true; explorer.SelectPlanet(7);
            Require(!rig.IsTransitioning,"Reduced motion must skip travel.");
            explorer.Overview(); rig.ReduceMotion=false; explorer.Interface.RefreshControls();
            Debug.Log("Camera flights verified: nine round trips, all 72 body pairs, travel input lock, no body intersections and reduced motion.");
        }

        private static void SampleFlight(SolarSystemBootstrap explorer)
        {
            var rig=explorer.CameraRig;
            Vector3 previous=rig.transform.position;
            for(int frame=0;frame<130;frame++)
            {
                rig.AdvanceMotion(1f/60);
                Vector3 point=rig.transform.position;
                Require(!float.IsNaN(point.x) && !float.IsInfinity(point.x),"Camera pose must be finite.");
                Require(Vector3.Distance(previous,point)<15,"Camera must travel without frame-sized jumps.");
                Require(point.magnitude>SolarLayout.SunRadius+0.1f,"Camera crosses Sun at frame "+frame+", selection "+explorer.SelectedIndex+", position "+point);
                foreach(var planet in explorer.Planets)
                    Require(Vector3.Distance(point,planet.transform.position)>planet.Data.visualRadius+0.1f,"Camera intersects "+planet.Data.name);
                previous=point;
            }
            Require(!rig.IsTransitioning,"Flight must settle in its bounded duration.");
        }

        private static void CheckLabels(SolarSystemBootstrap explorer,Camera camera,Canvas canvas)
        {
            var canvasRect=canvas.GetComponent<RectTransform>();
            foreach(var planet in explorer.Planets)
            {
                var label=explorer.Interface.LayoutRoot.Find(planet.Data.name).GetComponent<RectTransform>();
                if(!label.gameObject.activeInHierarchy) continue;
                Vector3 projected=camera.WorldToViewportPoint(planet.transform.position);
                Vector2 point=explorer.Interface.LayoutRoot.InverseTransformPoint(canvasRect.TransformPoint(new Vector2((projected.x-0.5f)*canvasRect.rect.width,(projected.y-0.5f)*canvasRect.rect.height)));
                Require(!new Rect(label.anchoredPosition-label.sizeDelta*0.5f,label.sizeDelta).Contains(point),"Label covers its planet: "+planet.Data.name);
            }
        }

        private static void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
        }
    }
}
