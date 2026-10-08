using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolarExplorer.Editor
{
    // Exercise production gestures and uGUI controls without requiring an attached touch device.
    public static class InteractionChecks
    {
        public static void Gestures()
        {
            int taps=0,orbits=0,pinches=0; float ratio=1;
            var gestures=new TouchGestures(p=>p.x>700,p=>taps++,p=>orbits++,r=>{pinches++;ratio=r;});
            Vector2 size=new Vector2(1000,800);
            Action<float,TouchContact[]> send=(time,contacts)=>gestures.Process(contacts,contacts.Length,time,size);
            send(0,new[]{Finger(0,300,300,TouchPhase.Began)});
            send(0.15f,new[]{Finger(0,302,301,TouchPhase.Ended)});
            Require(taps==1 && orbits==0 && pinches==0,"A stationary short touch must select exactly once.");
            send(1,new[]{Finger(0,300,300,TouchPhase.Began)});
            send(1.1f,new[]{Finger(0,360,325,TouchPhase.Moved)});
            send(1.2f,new[]{Finger(0,360,325,TouchPhase.Ended)});
            Require(taps==1 && orbits>0,"A drag must orbit without selecting.");
            send(2,new[]{Finger(0,300,300,TouchPhase.Began),Finger(1,400,300,TouchPhase.Began)});
            send(2.1f,new[]{Finger(0,270,300,TouchPhase.Moved),Finger(1,430,300,TouchPhase.Moved)});
            Require(pinches==1 && Mathf.Abs(ratio-1.6f)<0.001f,"Pinch spread must report the change in finger distance.");
            send(2.2f,new[]{Finger(0,270,300,TouchPhase.Ended),Finger(1,430,300,TouchPhase.Stationary)});
            send(2.3f,new[]{Finger(1,430,300,TouchPhase.Ended)});
            Require(taps==1,"Lifting a pinch must never select a planet.");
            int oldOrbits=orbits,oldPinches=pinches;
            send(3,new[]{Finger(0,800,300,TouchPhase.Began)});
            send(3.1f,new[]{Finger(0,600,400,TouchPhase.Moved),Finger(1,300,300,TouchPhase.Began)});
            send(3.2f,new[]{Finger(0,550,450,TouchPhase.Moved),Finger(1,250,300,TouchPhase.Moved)});
            send(3.3f,new[]{Finger(0,550,450,TouchPhase.Ended),Finger(1,250,300,TouchPhase.Ended)});
            Require(taps==1 && orbits==oldOrbits && pinches==oldPinches,"UI-owned touches must never leak into camera gestures.");
            send(4,new[]{Finger(0,300,300,TouchPhase.Began)});
            send(4.1f,new[]{Finger(0,300,300,TouchPhase.Canceled)});
            Require(taps==1,"Canceled touches must not select.");
            send(5,new[]{Finger(0,300,300,TouchPhase.Began)});
            send(5.8f,new[]{Finger(0,300,300,TouchPhase.Ended)});
            Require(taps==1,"A long hold must not turn into an accidental tap.");
            send(6,new[]{Finger(0,300,300,TouchPhase.Began),Finger(1,400,300,TouchPhase.Began)});
            send(6.1f,new[]{Finger(0,300,300,TouchPhase.Stationary),Finger(1,400,300,TouchPhase.Stationary),Finger(2,500,300,TouchPhase.Began)});
            send(6.2f,new[]{Finger(0,300,300,TouchPhase.Stationary),Finger(1,500,300,TouchPhase.Moved),Finger(2,500,300,TouchPhase.Ended)});
            Require(pinches==oldPinches,"Dropping a third finger must rebase the pinch without a jump.");
            gestures.Cancel();
            send(7,new[]{Finger(0,300,300,TouchPhase.Began)});
            send(7.1f,new[]{Finger(0,300,300,TouchPhase.Ended)});
            Require(taps==2,"A fresh gesture after cancel must work normally.");
            Debug.Log("TOUCH_GESTURES_PASSED: taps, drag, pinch, finger lift, UI ownership, cancellation, holds and third-finger rebasing.");
        }

        public static void Article(SolarSystemBootstrap explorer,Camera overlay,Rect safe)
        {
            var ui=explorer.Interface; var scroll=ui.ArticleScroll;
            Canvas.ForceUpdateCanvases();
            // A synchronous batch test has not yet reached the normal canvas render phase.
            // Render once so newly activated graphics receive their UI raycast depths.
            foreach(var child in ui.Canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=5;
            overlay.Render();
            Require(scroll.content.rect.height>scroll.viewport.rect.height+200,"Articles must contain substantial scrollable information.");
            Require(ui.SourceButton.transform.GetSiblingIndex()==scroll.content.childCount-1,"NASA link must be the final article item.");
            Require(!Within(scroll.viewport,ui.SourceButton.GetComponent<RectTransform>().position),"NASA link must start below the visible article.");
            Vector3 cameraPosition=explorer.CameraRig.transform.position; Quaternion cameraRotation=explorer.CameraRig.transform.rotation;
            var center=RectTransformUtility.WorldToScreenPoint(overlay,scroll.viewport.TransformPoint(scroll.viewport.rect.center));
            Require(ExplorerInput.OverInterface(center),"Article must receive UI input and block world selection. Point="+center+", screen="+Screen.width+"x"+Screen.height+", viewport depth="+scroll.viewport.GetComponent<Image>().depth+", canvas="+ui.Canvas.pixelRect);
            var pointer=new PointerEventData(EventSystem.current) { button=PointerEventData.InputButton.Left,position=center,pressPosition=center };
            pointer.pointerPressRaycast=new RaycastResult { module=ui.Canvas.GetComponent<GraphicRaycaster>(),gameObject=scroll.gameObject };
            scroll.OnInitializePotentialDrag(pointer); scroll.OnBeginDrag(pointer);
            pointer.position=center+Vector2.up*80; pointer.delta=Vector2.up*80;
            scroll.OnDrag(pointer); scroll.OnEndDrag(pointer); scroll.StopMovement();
            Require(scroll.verticalNormalizedPosition<0.999f,"Swiping up must scroll the actual article.");
            Require(cameraPosition==explorer.CameraRig.transform.position && cameraRotation==explorer.CameraRig.transform.rotation,"Article scrolling must not change the camera.");
            scroll.verticalNormalizedPosition=0; Canvas.ForceUpdateCanvases();
            Require(Within(scroll.viewport,ui.SourceButton.GetComponent<RectTransform>().position),"NASA link must be reachable at the end.");
            scroll.verticalNormalizedPosition=1; scroll.StopMovement(); Canvas.ForceUpdateCanvases();
            CheckSafe(ui.CloseButton.GetComponent<RectTransform>(),overlay,safe);
            CheckSafe(ui.ResetButton.GetComponent<RectTransform>(),overlay,safe);
            Require(ui.CloseButton.GetComponent<RectTransform>().rect.width>=44 && ui.CloseButton.GetComponent<RectTransform>().rect.height>=44,"Close needs a comfortable touch target.");
            Require(ui.Details.rect.width>=250 && scroll.viewport.rect.height>=90,"Information panel must remain usable at this aspect ratio.");
        }

        public static void Controls(SolarSystemBootstrap explorer,Camera overlay)
        {
            var ui=explorer.Interface; var rig=explorer.CameraRig;
            for(int i=0;i<9;i++)
            {
                ui.NavigationButtons[i].onClick.Invoke(); rig.Snap();
                Require(explorer.SelectedIndex==i && rig.Target==explorer.Bodies[i],"Navigation must switch directly between all nine bodies.");
                Vector3 initial=rig.transform.position; Quaternion initialRotation=rig.transform.rotation;
                rig.Orbit(new Vector2(35,12)); rig.ZoomBy(2); rig.AdvanceMotion(1);
                Require(Vector3.Distance(initial,rig.transform.position)>1 && rig.Zoom<1,"Focused world must support orbit and zoom.");
                rig.ZoomBy(0.001f); Require(rig.Zoom==1,"Zooming out must stop at the initial focus distance.");
                rig.ZoomBy(10000); Require(Mathf.Abs(rig.Zoom-0.35f)<0.001f,"Zooming in must stop before entering the body.");
                ui.ResetButton.onClick.Invoke(); rig.AdvanceMotion(1);
                Require(Vector3.Distance(initial,rig.transform.position)<0.01f && Quaternion.Angle(initialRotation,rig.transform.rotation)<0.01f,"Reset must restore both initial focus angle and distance.");
            }
            ui.CloseButton.onClick.Invoke(); rig.Snap();
            Require(explorer.SelectedIndex==-1 && !ui.Details.gameObject.activeSelf,"X must close the article and return to the overview.");
            Physics.SyncTransforms(); Canvas.ForceUpdateCanvases(); ui.RefreshProjectedLabels();
            var camera=rig.GetComponent<Camera>(); var input=explorer.GetComponent<ExplorerInput>();
            Vector3 sun=camera.WorldToScreenPoint(explorer.Bodies[8].transform.position);
            input.SelectAt(sun); rig.Snap();
            Require(explorer.SelectedIndex==8,"Clicking or tapping the Sun must select its article.");
            bool switched=false;
            for(int from=0;from<9 && !switched;from++)
            {
                explorer.SelectPlanet(from); rig.Snap(); Canvas.ForceUpdateCanvases(); ui.RefreshProjectedLabels();
                for(int to=0;to<9 && !switched;to++)
                {
                    if(from==to) continue;
                    var point=camera.WorldToScreenPoint(explorer.Bodies[to].transform.position);
                    if(point.z<=0 || point.x<0 || point.x>camera.pixelWidth || point.y<0 || point.y>camera.pixelHeight || ExplorerInput.OverInterface(point)) continue;
                    if(!Physics.Raycast(camera.ScreenPointToRay(point),out var hit,1400) || hit.collider.GetComponentInParent<PlanetBody>()!=explorer.Bodies[to]) continue;
                    input.SelectAt(point); switched=explorer.SelectedIndex==to;
                }
            }
            Require(switched,"A visible neighboring body must be selectable during another body's focused view.");
            rig.Snap(); explorer.Overview(); rig.Snap();
            var nav=ui.NavigationButtons[0].GetComponentInParent<ScrollRect>();
            if(nav.content.rect.width>nav.viewport.rect.width+1)
            {
                nav.horizontalNormalizedPosition=1; Canvas.ForceUpdateCanvases();
                Require(Within(nav.viewport,ui.NavigationButtons[7].transform.position),"Swiping the navigation must expose Neptune.");
                nav.horizontalNormalizedPosition=0; Canvas.ForceUpdateCanvases();
                Require(Within(nav.viewport,ui.NavigationButtons[8].transform.position),"Sun must be the first destination.");
            }
            Debug.Log("INTERACTION_LAYOUT_PASSED: "+camera.pixelWidth+"x"+camera.pixelHeight+"; article drag, final source, safe-area X/reset, all destinations, zoom limits, reset, direct body switching.");
        }

        public static void NativeTouchOrbit(SolarSystemBootstrap explorer,Camera overlay)
        {
            var rig=explorer.CameraRig; var camera=rig.GetComponent<Camera>();
            var input=explorer.GetComponent<ExplorerInput>(); var ui=explorer.Interface;
            Vector2 size=new Vector2(camera.pixelWidth,camera.pixelHeight);
            float unit=Mathf.Min(size.x,size.y);
            Action<float,TouchContact[]> send=(time,contacts)=>input.ProcessTouches(contacts,contacts.Length,time,size);
            // Desktop batch editors without a touch device report false even after
            // setting this platform capability; the native mobile runtime must honor it.
            if(Application.isMobilePlatform) Require(Input.multiTouchEnabled,"Native multi-touch must be enabled.");
            for(int index=0;index<9;index++)
            {
                explorer.SelectPlanet(index); rig.Snap(); ui.RefreshProjectedLabels(); Canvas.ForceUpdateCanvases(); overlay.Render();
                Quaternion defaultAngle=rig.transform.rotation;
                Vector2 center=camera.WorldToScreenPoint(explorer.Bodies[index].transform.position);
                Require(!ExplorerInput.OverInterface(center),"A finger on the selected body's center must reach the world: "+index);
                send(0,new[]{new TouchContact(0,center,TouchPhase.Began)});
                for(int frame=1;frame<=18;frame++)
                {
                    send(frame/60f,new[]{new TouchContact(0,center+new Vector2(frame*unit*0.008f,frame*unit*0.003f),TouchPhase.Moved)});
                    rig.AdvanceMotion(1f/60);
                }
                send(0.35f,new[]{new TouchContact(0,center+new Vector2(18*unit*0.008f,18*unit*0.003f),TouchPhase.Ended)});
                rig.AdvanceMotion(0.3f);
                Require(Quaternion.Angle(defaultAngle,rig.transform.rotation)>15 && explorer.SelectedIndex==index,"One-finger native drag must rotate around the selected body without switching: "+index);
                send(1,new[]{new TouchContact(0,center-Vector2.right*unit*0.04f,TouchPhase.Began),new TouchContact(1,center+Vector2.right*unit*0.04f,TouchPhase.Began)});
                send(1.1f,new[]{new TouchContact(0,center-Vector2.right*unit*0.08f,TouchPhase.Moved),new TouchContact(1,center+Vector2.right*unit*0.08f,TouchPhase.Moved)});
                send(1.2f,new[]{new TouchContact(0,center-Vector2.right*unit*0.08f,TouchPhase.Ended),new TouchContact(1,center+Vector2.right*unit*0.08f,TouchPhase.Ended)});
                Require(rig.Zoom<0.6f && explorer.SelectedIndex==index,"Native pinch must zoom the current world without changing selection.");
                rig.ResetView(); rig.Snap();
                Require(Quaternion.Angle(defaultAngle,rig.transform.rotation)<0.01f,"Reset must undo native touch orbit.");

                // Travel is deliberately non-interactive, including its UI and direct selection.
                explorer.Overview(); rig.Snap(); explorer.SelectPlanet(index); rig.AdvanceMotion(0.6f);
                Require(explorer.InteractionLocked && !ui.CloseButton.IsInteractable() && !ui.ArticleScroll.enabled,"Travel must lock both camera and UI controls.");
                bool paused=explorer.Paused,orbits=explorer.OrbitsVisible,motion=rig.ReduceMotion,overhead=rig.IsOverhead;
                rig.Orbit(new Vector2(90,45)); rig.ZoomBy(2); rig.ResetView();
                explorer.SelectPlanet((index+1)%9); explorer.Overview(); explorer.TogglePause(); explorer.ToggleOrbits(); explorer.ToggleMotion(); explorer.ToggleView();
                ui.NavigationButtons[(index+1)%9].onClick.Invoke(); ui.CloseButton.onClick.Invoke(); ui.ResetButton.onClick.Invoke();
                Vector2 start=Vector2.Scale(rig.FocusViewport,size);
                input.SelectAt(start);
                send(2,new[]{new TouchContact(0,start,TouchPhase.Began)});
                send(2.1f,new[]{new TouchContact(0,start+Vector2.right*unit*0.2f,TouchPhase.Moved)});
                Require(explorer.SelectedIndex==index && rig.Zoom==1 && explorer.Paused==paused && explorer.OrbitsVisible==orbits && rig.ReduceMotion==motion && rig.IsOverhead==overhead,"Input must not redirect travel or change its state.");
                rig.AdvanceMotion(2);
                Require(Quaternion.Angle(defaultAngle,rig.transform.rotation)<0.01f && !explorer.InteractionLocked && ui.CloseButton.IsInteractable(),"Arrival must preserve the default framing and unlock controls.");
                send(2.2f,new[]{new TouchContact(0,start+Vector2.right*unit*0.3f,TouchPhase.Moved)});
                send(2.3f,new[]{new TouchContact(0,start+Vector2.right*unit*0.3f,TouchPhase.Ended)});
                rig.AdvanceMotion(1);
                Require(Quaternion.Angle(defaultAngle,rig.transform.rotation)<0.01f && explorer.SelectedIndex==index,"A touch started during travel must stay ignored until lifted.");
                send(3,Array.Empty<TouchContact>());
                Vector3 position=rig.transform.position;
                Vector2 panel=RectTransformUtility.WorldToScreenPoint(overlay,ui.ArticleScroll.viewport.position);
                send(4,new[]{new TouchContact(0,panel,TouchPhase.Began)});
                send(4.1f,new[]{new TouchContact(0,panel+Vector2.up*unit*0.2f,TouchPhase.Moved)});
                send(4.2f,new[]{new TouchContact(0,panel+Vector2.up*unit*0.2f,TouchPhase.Ended)});
                rig.AdvanceMotion(1);
                Require(Vector3.Distance(position,rig.transform.position)<0.01f && explorer.SelectedIndex==index,"Touching the article must not rotate or select the world behind it.");
            }
            explorer.Overview();
            Require(explorer.InteractionLocked,"Returning to the overview must also lock controls.");
            explorer.SelectPlanet(4); rig.Orbit(new Vector2(90,30)); rig.ZoomBy(2);
            Require(explorer.SelectedIndex==-1,"Return travel cannot be redirected by a new selection.");
            rig.Snap();
            Vector3 home=rig.transform.position; Quaternion homeRotation=rig.transform.rotation;
            rig.Orbit(new Vector2(120,40)); rig.ZoomBy(2); rig.AdvanceMotion(1);
            Require(Vector3.Distance(home,rig.transform.position)<0.01f && Quaternion.Angle(homeRotation,rig.transform.rotation)<0.01f,"Overview must stay fixed; zoom and orbit belong only to a selected world.");
            Debug.Log("NATIVE_TOUCH_ORBIT_PASSED: "+camera.pixelWidth+"x"+camera.pixelHeight+"; nine worlds, finger-on-surface drag, pinch, reset, UI ownership, locked travel/arrival, and fixed overview.");
        }

        private static TouchContact Finger(int id,float x,float y,TouchPhase phase) => new TouchContact(id,new Vector2(x,y),phase);
        private static bool Within(RectTransform viewport,Vector3 point) => viewport.rect.Contains(viewport.InverseTransformPoint(point));
        private static void CheckSafe(RectTransform rect,Camera camera,Rect safe)
        {
            var corners=new Vector3[4]; rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                Require(point.x>=safe.xMin-1 && point.x<=safe.xMax+1 && point.y>=safe.yMin-1 && point.y<=safe.yMax+1,"Controls must stay inside device safe area: "+rect.name);
            }
        }
        private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    }
}
