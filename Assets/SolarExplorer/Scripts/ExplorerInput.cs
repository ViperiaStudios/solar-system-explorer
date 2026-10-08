using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SolarExplorer
{
    // uGUI owns fingers that start on its panels; the world owns all other gestures.
    public sealed class ExplorerInput : MonoBehaviour
    {
        private SolarSystemBootstrap explorer;
        private TouchGestures gestures;
        private readonly TouchContact[] contacts=new TouchContact[10];
        private static readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        private bool oldSimulateMouse,oldMultiTouch,mouseHeld,mouseBlocked,mouseDragged,rightHeld;
        private Vector2 mouseOrigin,mousePrevious,touchScreenSize;
        private bool waitForTouchRelease;

        private void Awake()
        {
            oldSimulateMouse=Input.simulateMouseWithTouches; oldMultiTouch=Input.multiTouchEnabled;
            Input.simulateMouseWithTouches=false; Input.multiTouchEnabled=true;
        }
        public void Initialize(SolarSystemBootstrap owner)
        {
            explorer=owner;
            gestures=new TouchGestures(OverInterface,SelectAt,owner.CameraRig.Orbit,owner.CameraRig.ZoomBy);
        }

        // Native input and integration checks share this complete routing path.
        public void ProcessTouches(TouchContact[] samples,int count,float now,Vector2 screenSize)
        {
            if(explorer.InteractionLocked)
            { gestures.Cancel(); waitForTouchRelease=count>0; return; }
            // A finger placed during travel cannot turn into a tap/drag on arrival.
            if(waitForTouchRelease)
            { gestures.Cancel(); if(count==0) waitForTouchRelease=false; return; }
            // Device rotation changes the coordinate system; rebase an existing gesture.
            if(touchScreenSize!=screenSize) { gestures.Cancel(); touchScreenSize=screenSize; }
            gestures.Process(samples,count,now,screenSize);
            if(count>0) mouseHeld=rightHeld=false;
        }

        public static bool OverInterface(Vector2 position)
        {
            if(EventSystem.current==null) return false;
            var pointer=new PointerEventData(EventSystem.current) { position=position };
            uiHits.Clear(); EventSystem.current.RaycastAll(pointer,uiHits);
            return uiHits.Count>0;
        }

        public void SelectAt(Vector2 position)
        {
            if(explorer.InteractionLocked) return;
            if(OverInterface(position)) return;
            var camera=explorer.CameraRig.GetComponent<Camera>();
            RaycastHit hit;
            if(Physics.Raycast(camera.ScreenPointToRay(position),out hit,1400))
            {
                var body=hit.collider.GetComponentInParent<PlanetBody>();
                if(body!=null) { explorer.SelectPlanet(body.Index); return; }
            }
            // Give tiny, visible worlds a comfortable touch target without selecting through other bodies.
            float best=Mathf.Max(22,Screen.height*0.023f);
            PlanetBody nearest=null;
            foreach(var body in explorer.Bodies)
            {
                Vector3 projected=camera.WorldToScreenPoint(body.transform.position);
                if(projected.z<=0) continue;
                float distance=Vector2.Distance(position,projected);
                if(distance>=best) continue;
                if(Physics.Raycast(camera.ScreenPointToRay(projected),out hit,1400) && hit.collider.GetComponentInParent<PlanetBody>()==body)
                { best=distance; nearest=body; }
            }
            if(nearest!=null) explorer.SelectPlanet(nearest.Index);
        }

        private void Update()
        {
            if(explorer==null) return;
            int count=Mathf.Min(Input.touchCount,contacts.Length);
            for(int i=0;i<count;i++)
            {
                var touch=Input.GetTouch(i);
                contacts[i]=new TouchContact(touch.fingerId,touch.position,touch.phase);
            }
            ProcessTouches(contacts,count,Time.unscaledTime,new Vector2(Screen.width,Screen.height));
            if(explorer.InteractionLocked) { mouseHeld=rightHeld=false; return; }
            if(count>0) return;
            Vector2 point=Input.mousePosition;
            if(Input.GetMouseButtonDown(0))
            { mouseHeld=true; mouseOrigin=mousePrevious=point; mouseBlocked=OverInterface(point); mouseDragged=false; }
            if(Input.GetMouseButtonDown(1)) { rightHeld=!OverInterface(point); mousePrevious=point; }
            if(Input.GetMouseButtonUp(1)) rightHeld=false;
            if(mouseHeld && Vector2.Distance(mouseOrigin,point)>8) mouseDragged=true;
            if(rightHeld || (mouseHeld && mouseDragged && !mouseBlocked))
                explorer.CameraRig.Orbit((point-mousePrevious)*(180f/Mathf.Max(1,Screen.height)));
            if(Input.GetMouseButtonUp(0))
            {
                if(mouseHeld && !mouseBlocked && !mouseDragged) SelectAt(point);
                mouseHeld=false;
            }
            if(!OverInterface(point) && Input.mouseScrollDelta.y!=0)
                explorer.CameraRig.ZoomBy(Mathf.Exp(Input.mouseScrollDelta.y*0.1f));
            mousePrevious=point;
        }

        private void OnApplicationFocus(bool focused)
        { if(!focused) { gestures?.Cancel(); mouseHeld=rightHeld=false; } }
        private void OnApplicationPause(bool paused)
        { if(paused) { gestures?.Cancel(); mouseHeld=rightHeld=false; } }
        private void OnDestroy()
        { Input.simulateMouseWithTouches=oldSimulateMouse; Input.multiTouchEnabled=oldMultiTouch; }
    }

    public struct TouchContact
    {
        public int id; public Vector2 position; public TouchPhase phase;
        public TouchContact(int id,Vector2 position,TouchPhase phase) { this.id=id; this.position=position; this.phase=phase; }
    }

    // Platform-independent gesture state machine; tested with the same events that native touch feeds it.
    public sealed class TouchGestures
    {
        private sealed class Finger { public Vector2 origin,previous,position; public float started; public bool dragged; }
        private readonly Dictionary<int,Finger> fingers=new Dictionary<int,Finger>();
        private readonly List<int> ended=new List<int>();
        private readonly Func<Vector2,bool> overUI;
        private readonly Action<Vector2> tap,orbit;
        private readonly Action<float> pinch;
        private bool blocked,multiple;
        private int pairA=-1,pairB=-1;
        private float previousDistance;

        public TouchGestures(Func<Vector2,bool> overUI,Action<Vector2> tap,Action<Vector2> orbit,Action<float> pinch)
        { this.overUI=overUI; this.tap=tap; this.orbit=orbit; this.pinch=pinch; }
        public void Cancel() { fingers.Clear(); blocked=multiple=false; pairA=pairB=-1; previousDistance=0; }

        public void Process(TouchContact[] samples,int count,float now,Vector2 size)
        {
            if(count==0) { Cancel(); return; }
            float threshold=Mathf.Max(9,Mathf.Min(size.x,size.y)*0.015f);
            ended.Clear();
            for(int i=0;i<count;i++)
            {
                var sample=samples[i];
                Finger finger;
                if(!fingers.TryGetValue(sample.id,out finger))
                {
                    finger=new Finger { origin=sample.position,previous=sample.position,position=sample.position,started=now };
                    fingers.Add(sample.id,finger); blocked|=overUI(sample.position);
                }
                finger.previous=finger.position; finger.position=sample.position;
                finger.dragged|=Vector2.Distance(finger.origin,finger.position)>threshold;
                if(sample.phase==TouchPhase.Ended || sample.phase==TouchPhase.Canceled) ended.Add(sample.id);
            }
            if(fingers.Count>1) multiple=true;
            // A pinch can never become a tap when either finger is lifted.
            for(int i=0;i<count;i++)
            {
                var sample=samples[i]; var finger=fingers[sample.id];
                if(sample.phase==TouchPhase.Ended && !blocked && !multiple && !finger.dragged && now-finger.started<0.6f && !overUI(sample.position)) tap(sample.position);
            }
            foreach(int id in ended) fingers.Remove(id);
            if(fingers.Count==0) { Cancel(); return; }
            if(blocked) return;
            if(fingers.Count==1)
            {
                pairA=pairB=-1;
                foreach(var finger in fingers.Values)
                    if(finger.dragged) orbit((finger.position-finger.previous)*(180f/Mathf.Max(1,Mathf.Min(size.x,size.y))));
            }
            else if(fingers.Count==2)
            {
                int a=-1,b=-1; Vector2 first=default,second=default;
                foreach(var entry in fingers) { if(a<0) { a=entry.Key; first=entry.Value.position; } else { b=entry.Key; second=entry.Value.position; } }
                float distance=Vector2.Distance(first,second);
                if(pairA==a && pairB==b && previousDistance>10 && distance>10) pinch(distance/previousDistance);
                pairA=a; pairB=b; previousDistance=distance;
            }
            else { pairA=pairB=-1; previousDistance=0; }
        }
    }
}
