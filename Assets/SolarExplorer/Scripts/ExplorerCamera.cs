using UnityEngine;

namespace SolarExplorer
{
    [RequireComponent(typeof(Camera))]
    public sealed class ExplorerCamera : MonoBehaviour
    {
        public PlanetBody Target { get; private set; }
        public bool ReduceMotion { get; set; }
        public bool IsOverhead { get; private set; }
        public bool IsTransitioning => elapsed < duration;
        public bool MapMode { get; private set; }
        public float TransitionProgress => duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
        public float FocusBlend => MapMode?0:current.frame / 0.34f;
        private Camera view;
        private SolarSystemBootstrap system;
        private float yaw, pitch = 18, zoom = 1;
        private float overviewYaw = 0, overviewPitch = SolarLayout.OverviewPitch, overviewZoom = 1;
        private float elapsed, duration;
        public bool Portrait => view != null && view.aspect < 1.2f;
        public float Zoom => Target != null || MapMode ? zoom : overviewZoom;
        public Vector2 FocusViewport => Portrait ? new Vector2(0.5f,0.66f) : new Vector2(0.33f,0.5f);
        private Pose current, departure;
        private readonly System.Collections.Generic.List<Vector3> framingPoints=new System.Collections.Generic.List<Vector3>(128);
        private const float SaturnYaw=-205, SaturnPitch=15;
        private Vector3 mapPivot;
        private float mapRadius,mapYaw,mapPitch;
        private bool mapPlanet,scaleDeparture;
        private Pose scaleDestination;

        private struct Pose
        {
            public Vector3 pivot;
            public Quaternion rotation;
            public float distance, frame;
        }

        private void Awake() { view = GetComponent<Camera>(); system=GetComponentInParent<SolarSystemBootstrap>(); }

        public void Focus(PlanetBody planet)
        {
            MapMode=false;
            if (Target == planet) return;
            Target = planet;
            DefaultFocusPose();
            BeginTravel(1.65f);
        }

        private void DefaultFocusPose()
        {
            var planet = Target;
            Vector3 awayFromSun = planet.transform.position.normalized;
            yaw = Mathf.Atan2(awayFromSun.x, awayFromSun.z) * Mathf.Rad2Deg - 24;
            if(planet.Index==5) yaw=SaturnYaw;
            pitch = planet.Index == 5 ? SaturnPitch : 52;
            zoom = 1;
        }

        public void ResetView()
        {
            if(IsTransitioning || (system!=null && system.InteractionLocked)) return;
            if(MapMode) { yaw=mapYaw; pitch=mapPitch; zoom=1; BeginTravel(0.75f); return; }
            if(Target==null) return;
            DefaultFocusPose();
            BeginTravel(0.75f);
        }

        public void Overview()
        {
            if (Target == null) return;
            Target = null;
            // Preserve the user's previous system angle and zoom.
            BeginTravel(1.8f);
        }

        public void ToggleOverhead()
        {
            IsOverhead = !IsOverhead;
            overviewPitch = IsOverhead ? 88 : SolarLayout.OverviewPitch;
            overviewZoom = 1;
            BeginTravel(1.2f);
        }

        private void BeginTravel(float seconds)
        {
            departure = current;
            elapsed = 0;
            duration = seconds;
            if (ReduceMotion) Snap();
            system?.Interface?.RefreshInteractionState();
        }

        public void Snap()
        {
            current = Destination();
            elapsed = duration;
            ApplyPose();
        }

        private void LateUpdate()
        {
            AdvanceMotion(Time.unscaledDeltaTime);
        }

        public void Orbit(Vector2 deltaDegrees)
        {
            if(IsTransitioning || (system!=null && system.InteractionLocked) || (!MapMode && Target==null)) return;
            yaw+=deltaDegrees.x; pitch=Mathf.Clamp(pitch-deltaDegrees.y,MapMode && !mapPlanet?12:-85,85);
        }

        public void ZoomBy(float ratio)
        {
            if(IsTransitioning || (system!=null && system.InteractionLocked) || (!MapMode && Target==null)) return;
            if (ratio <= 0 || float.IsNaN(ratio) || float.IsInfinity(ratio)) return;
            zoom=Mathf.Clamp(zoom/ratio,MapMode && !mapPlanet?0.55f:0.35f,MapMode && !mapPlanet?1.6f:1);
        }

        public void BeginScaleDeparture()
        {
            MapMode=true; Target=null; scaleDeparture=true;
            scaleDestination=current; scaleDestination.distance*=2.2f;
            BeginTravel(0.7f);
        }

        public void ShowMap(Vector3 pivot,float radius,Vector2 angle,bool planet,bool reveal=false,bool outward=false)
        {
            MapMode=true; scaleDeparture=false; Target=null; mapPivot=pivot; mapRadius=radius;
            mapPitch=pitch=angle.x; mapYaw=yaw=angle.y; mapPlanet=planet; zoom=1;
            if(reveal)
            {
                current=Destination(); current.distance*=outward?0.65f:1.5f; ApplyPose();
            }
            BeginTravel(reveal?1.25f:1.5f);
        }

        public void RevealSolarSystem()
        {
            MapMode=false; scaleDeparture=false; Target=null;
            current=Destination(); current.distance*=1.6f; ApplyPose(); BeginTravel(1.25f);
        }

        // Editor checks sample the same motion used at runtime.
        public void AdvanceMotion(float deltaTime)
        {
            var destination = Destination();
            if (ReduceMotion) { current = destination; elapsed = duration; }
            else if (IsTransitioning)
            {
                elapsed = Mathf.Min(elapsed + Mathf.Max(0, deltaTime), duration);
                float t = TransitionProgress;
                // Quintic easing arrives with zero speed and zero acceleration.
                float eased = t*t*t*(t*(t*6-15)+10);
                current = Blend(departure, destination, eased);
                // Lift the orbit over the ecliptic during travel so inner-planet
                // approaches clear the Sun while the framing pivot stays stable.
                float arc = Mathf.Sin(t*Mathf.PI);
                current.rotation *= Quaternion.Euler(32*arc*arc,0,0);
            }
            else current = Blend(current, destination, 1-Mathf.Exp(-12*Mathf.Max(0,deltaTime)));
            ApplyPose();
        }

        private static Pose Blend(Pose from, Pose to, float t)
        {
            return new Pose {
                pivot = Vector3.Lerp(from.pivot,to.pivot,t),
                rotation = Quaternion.Slerp(from.rotation,to.rotation,t),
                distance = Mathf.Lerp(from.distance,to.distance,t),
                frame = Mathf.Lerp(from.frame,to.frame,t)
            };
        }

        private void ApplyPose()
        {
            float tangent=Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*0.5f);
            Vector2 offset=(Vector2.one*0.5f-FocusViewport)*2*(current.frame/0.34f);
            float frameTangent=tangent*view.aspect*offset.x, verticalTangent=tangent*offset.y;
            Vector3 position=current.pivot+current.rotation*new Vector3(current.distance*frameTangent,current.distance*verticalTangent,-current.distance);
            // A smooth safety envelope also covers interrupted flights and user-chosen
            // orbit angles; it bends approaches before a surface reaches the near plane.
            if(!MapMode)
            {
                position=AvoidSphere(position,Vector3.zero,SolarLayout.SunRadius+0.5f);
                if(system!=null && system.Planets!=null)
                    foreach(var body in system.Planets) position=AvoidSphere(position,body.transform.position,body.Data.visualRadius+0.4f);
            }
            else if(system!=null && system.Universe!=null && system.Universe.InStarSystem && system.Universe.CurrentScene!=null)
            {
                foreach(var body in system.Universe.CurrentScene.Targets)
                    position=AvoidSphere(position,body.transform.position,body.Radius+0.4f);
            }
            // The off-axis pivot is projected into the free region beside/above the article.
            Quaternion rotation=Quaternion.LookRotation(current.pivot-position,current.rotation*Vector3.up)
                *Quaternion.FromToRotation(Vector3.forward,new Vector3(frameTangent,verticalTangent,1));
            transform.SetPositionAndRotation(position,rotation);
            system?.Interface?.RefreshInteractionState();
        }

        private static Vector3 AvoidSphere(Vector3 point,Vector3 center,float safeRadius)
        {
            Vector3 offset=point-center;
            float distance=offset.magnitude, influence=safeRadius+2;
            if(distance>=influence) return point;
            // Value and slope match the unchanged path at the influence boundary.
            float radius=safeRadius+2*Mathf.Exp((distance-influence)*0.5f);
            return center+(distance>0.001f?offset/distance:Vector3.up)*radius;
        }

        private Pose Destination()
        {
            float tangent = Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*0.5f);
            if(MapMode)
            {
                if(scaleDeparture) return scaleDestination;
                float fit=Portrait?Mathf.Min(0.245f,view.aspect*0.78f):Mathf.Min(0.52f,view.aspect*0.45f);
                return new Pose {pivot=mapPivot,rotation=Quaternion.Euler(pitch,yaw,0),distance=mapRadius/(tangent*fit)*zoom,frame=0.34f};
            }
            if (Target == null)
            {
                Quaternion rotation = Quaternion.Euler(overviewPitch,overviewYaw,0);
                // The cinematic view is composed to the approved image, not fitted to
                // the complete orbital disk. Top view remains a full-system inspection.
                if(!IsOverhead)
                    return new Pose { pivot=SolarLayout.OverviewPivot, rotation=rotation,
                        distance=SolarLayout.OverviewDistance*Mathf.Max(1,1.72f/view.aspect)*overviewZoom, frame=0 };
                Quaternion inverse = Quaternion.Inverse(rotation);
                // Fit the actual worlds, not the full outer orbital paths. The outer
                // arcs deliberately extend beyond the screen like the approved mockup.
                var points=framingPoints; points.Clear();
                AddSphereBounds(points,inverse,Vector3.zero,SolarLayout.SunRadius);
                foreach(var body in system.Planets)
                {
                    AddSphereBounds(points,inverse,body.transform.position,body.Data.visualRadius*1.07f);
                    if(body.Index==5)
                        for(int i=0;i<32;i++)
                        {
                            float a=i*Mathf.PI*2/32;
                            Vector3 ring=SolarLayout.SaturnTilt*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*body.Data.visualRadius*2.3f;
                            points.Add(inverse*(body.transform.position+ring));
                        }
                }
                var overviewBounds=new Bounds(points[0],Vector3.zero);
                foreach(var point in points) overviewBounds.Encapsulate(point);
                Vector3 center=new Vector3(overviewBounds.center.x,overviewBounds.center.y,0);
                float distance=0;
                foreach(var point in points)
                {
                    Vector3 local=point-center;
                    distance=Mathf.Max(distance,Mathf.Abs(local.x)/(tangent*view.aspect*0.92f)-local.z);
                    distance=Mathf.Max(distance,Mathf.Abs(local.y)/(tangent*0.77f)-local.z);
                }
                // Place the world composition above the navigation, leaving breathing room below.
                center.y-=distance*tangent*0.13f;
                return new Pose { pivot=rotation*center,rotation=rotation,distance=distance*overviewZoom,frame=0 };
            }
            float bounds = Target.Data.visualRadius*1.15f;
            float focusDistance=Target.Index==5?SaturnFocusDistance(tangent):
                bounds/(tangent*(Portrait?Mathf.Min(0.27f,view.aspect*0.82f):Mathf.Min(0.56f,Mathf.Max(0.2f,view.aspect*0.42f))));
            return new Pose {
                pivot=Target.transform.position,rotation=Quaternion.Euler(pitch,yaw,0),frame=0.34f,
                distance=focusDistance*zoom
            };
        }

        private float SaturnFocusDistance(float tangent)
        {
            // Fit the projected ring ellipse, rather than an oversized sphere around it.
            // Use the arrival angle even during orbit, so rotation never changes zoom.
            Quaternion inverse=Quaternion.Inverse(Quaternion.Euler(SaturnPitch,SaturnYaw,0));
            var points=framingPoints; points.Clear();
            float radius=Target.Data.visualRadius;
            AddSphereBounds(points,Quaternion.identity,Vector3.zero,radius*1.04f);
            for(int i=0;i<64;i++)
            {
                float angle=i*Mathf.PI*2/64;
                points.Add(inverse*(SolarLayout.SaturnTilt*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*2.3f));
            }
            Vector2 center=FocusViewport;
            Vector2 minimum=Portrait?new Vector2(0.055f,0.505f):new Vector2(0.035f,0.17f);
            Vector2 maximum=Portrait?new Vector2(0.945f,0.815f):new Vector2(0.625f,0.83f);
            Vector2 projection=new Vector2(tangent*view.aspect,tangent);
            Vector2 offset=Vector2.Scale((Vector2.one*0.5f-center)*2,projection);
            float distance=0;
            foreach(var point in points)
            {
                Vector2 projected=new Vector2(point.x,point.y)+offset*point.z;
                float horizontal=projected.x<0?center.x-minimum.x:maximum.x-center.x;
                float vertical=projected.y<0?center.y-minimum.y:maximum.y-center.y;
                distance=Mathf.Max(distance,Mathf.Abs(projected.x)/(2*projection.x*horizontal)-point.z);
                distance=Mathf.Max(distance,Mathf.Abs(projected.y)/(2*projection.y*vertical)-point.z);
            }
            return distance;
        }

        private static void AddSphereBounds(System.Collections.Generic.List<Vector3> points,Quaternion inverse,Vector3 position,float radius)
        {
            Vector3 center=inverse*position;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                points.Add(center+new Vector3(x,y,z)*radius);
        }
    }
}
