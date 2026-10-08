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
        public float TransitionProgress => duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
        public float FocusBlend => current.frame / 0.34f;
        private Camera view;
        private SolarSystemBootstrap system;
        private float yaw, pitch = 18, zoom = 1;
        private float overviewYaw = 0, overviewPitch = SolarLayout.OverviewPitch, overviewZoom = 1;
        private float elapsed, duration;
        public bool Portrait => view != null && view.aspect < 1.2f;
        public float Zoom => Target != null ? zoom : overviewZoom;
        public Vector2 FocusViewport => Portrait ? new Vector2(0.5f,0.66f) : new Vector2(0.33f,0.5f);
        private Pose current, departure;
        private readonly System.Collections.Generic.List<Vector3> framingPoints=new System.Collections.Generic.List<Vector3>(128);

        private struct Pose
        {
            public Vector3 pivot;
            public Quaternion rotation;
            public float distance, frame;
        }

        private void Awake() { view = GetComponent<Camera>(); system=GetComponentInParent<SolarSystemBootstrap>(); }

        public void Focus(PlanetBody planet)
        {
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
            if(planet.Index==5) yaw=-65;
            pitch = planet.Index == 5 ? 42 : 52;
            zoom = 1;
        }

        public void ResetView()
        {
            if(IsTransitioning || Target==null) return;
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
            if(IsTransitioning || Target==null) return;
            yaw+=deltaDegrees.x; pitch=Mathf.Clamp(pitch-deltaDegrees.y,-85,85);
        }

        public void ZoomBy(float ratio)
        {
            if(IsTransitioning || Target==null) return;
            if (ratio <= 0 || float.IsNaN(ratio) || float.IsInfinity(ratio)) return;
            zoom=Mathf.Clamp(zoom/ratio,0.35f,1);
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
            position=AvoidSphere(position,Vector3.zero,SolarLayout.SunRadius+0.5f);
            if(system!=null && system.Planets!=null)
                foreach(var body in system.Planets) position=AvoidSphere(position,body.transform.position,body.Data.visualRadius+0.4f);
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
            float bounds = Target.Data.visualRadius*(Target.Index==5?2.35f:1.15f);
            return new Pose {
                pivot=Target.transform.position,rotation=Quaternion.Euler(pitch,yaw,0),frame=0.34f,
                distance=bounds/(tangent*(Portrait?Mathf.Min(0.27f,view.aspect*0.82f):Mathf.Min(0.56f,Mathf.Max(0.2f,view.aspect*0.42f))))*zoom
            };
        }

        private static void AddSphereBounds(System.Collections.Generic.List<Vector3> points,Quaternion inverse,Vector3 position,float radius)
        {
            Vector3 center=inverse*position;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                points.Add(center+new Vector3(x,y,z)*radius);
        }
    }
}
