using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using static SolarExplorer.InterfaceTheme;

namespace SolarExplorer
{
    [DefaultExecutionOrder(100)]
    public sealed class ExplorerUI : MonoBehaviour
    {
        private SolarSystemBootstrap explorer;
        private Camera view;
        private InterfaceTheme theme;
        private RectTransform canvasRect,root,header,controls,dock,navContent,detailContent,reticle;
        private Text brand,locationText,hint,footer,pauseText,motionText,viewText,pathsText;
        private RectTransform[] labels;
        private ScrollRect navigationScroll;
        private Rect safeViewport=new Rect(0,0,1,1),lastSafe;
        private int lastWidth,lastHeight;
        private bool touchLayout;
        private CanvasGroup interactionGroup;
        private bool wasLocked;
        public Canvas Canvas { get; private set; }
        public RectTransform LayoutRoot => root;
        public RectTransform Details { get; private set; }
        public ScrollRect ArticleScroll { get; private set; }
        public Button CloseButton { get; private set; }
        public Button ResetButton { get; private set; }
        public Button SourceButton { get; private set; }
        public Button[] NavigationButtons { get; private set; }
        public Button GalaxyButton { get; private set; }
        public bool AutomaticLayout { get; set; } = true;
        public bool IsPortrait { get; private set; }

        public void Initialize(SolarSystemBootstrap owner,Camera camera)
        {
            explorer=owner; view=camera; theme=new InterfaceTheme();
            var canvasObject=new GameObject("Explorer UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false); canvasRect=canvasObject.GetComponent<RectTransform>();
            Canvas=canvasObject.GetComponent<Canvas>(); Canvas.renderMode=RenderMode.ScreenSpaceOverlay; Canvas.pixelPerfect=true;
            canvasObject.GetComponent<CanvasScaler>().enabled=false;
            Box("HUD edge shading",canvasRect).gameObject.AddComponent<HudScrim>().raycastTarget=false;
            root=Box("Safe area",canvasRect);
            interactionGroup=root.gameObject.AddComponent<CanvasGroup>();
            header=Box("Header",root);
            brand=theme.Label("S O L A R  /  E X P L O R E R",header,23,Ink,true);
            locationText=theme.Label("OPEN SPACE / HIGH OVERVIEW",header,12,Accent);
            controls=Box("View controls",header); Row(controls,5,true);
            theme.Button("SYSTEM",controls,explorer.Overview);
            viewText=theme.Button("TOP VIEW",controls,explorer.ToggleView).GetComponentInChildren<Text>();
            pathsText=theme.Button("ORBITS ON",controls,explorer.ToggleOrbits).GetComponentInChildren<Text>();
            pauseText=theme.Button("PAUSE",controls,explorer.TogglePause).GetComponentInChildren<Text>();
            motionText=theme.Button("SMOOTH",controls,explorer.ToggleMotion).GetComponentInChildren<Text>();
            GalaxyButton=theme.Button("GALAXY",controls,()=>explorer.Universe.OpenMilkyWay());
            foreach(var label in controls.GetComponentsInChildren<Text>()) label.fontSize=12;
            BuildDetails(); BuildNavigation();
            reticle=Box("Planet targeting frame",root); reticle.anchorMin=reticle.anchorMax=Vector2.one*0.5f;
            reticle.gameObject.AddComponent<HudReticle>().raycastTarget=false; reticle.SetAsFirstSibling();
            hint=theme.Label("",root,12,Muted);
            footer=theme.Label("MAPS: SOLAR SYSTEM SCOPE / SKY: ESO/S. Brunier · CC BY 4.0",root,10,Muted);
            ApplyLayout(Screen.width,Screen.height,Screen.safeArea,Application.isMobilePlatform || Input.touchSupported);
            ShowOverview();
        }

        private void BuildDetails()
        {
            Details=Box("Planet information",root);
            theme.Surface(Details,Panel).Border=new Color(0.3f,0.8f,0.95f,0.5f);
            ResetButton=theme.Button("RESET VIEW",Details,()=>explorer.CameraRig.ResetView()); ResetButton.name="Reset view";
            Place(ResetButton.GetComponent<RectTransform>(),new Vector2(0,1),Vector2.one,new Vector2(16,-64),new Vector2(-80,-16));
            CloseButton=theme.Button("X",Details,explorer.Overview); CloseButton.name="Close details";
            CloseButton.GetComponentInChildren<Text>().fontSize=26;
            Place(CloseButton.GetComponent<RectTransform>(),Vector2.one,Vector2.one,new Vector2(-64,-64),new Vector2(-16,-16));
            var region=Box("Scrollable facts",Details);
            Place(region,Vector2.zero,Vector2.one,new Vector2(20,20),new Vector2(-26,-80));
            ArticleScroll=region.gameObject.AddComponent<ScrollRect>(); ArticleScroll.horizontal=false;
            ArticleScroll.movementType=ScrollRect.MovementType.Clamped; ArticleScroll.scrollSensitivity=32;
            var viewport=Box("Viewport",region);
            viewport.gameObject.AddComponent<Image>().color=Color.white; viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            detailContent=Box("Facts",viewport);
            Place(detailContent,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); detailContent.pivot=new Vector2(0.5f,1);
            var column=detailContent.gameObject.AddComponent<VerticalLayoutGroup>(); column.spacing=14;
            column.childControlWidth=column.childControlHeight=true; column.childForceExpandWidth=true; column.childForceExpandHeight=false;
            detailContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            ArticleScroll.viewport=viewport; ArticleScroll.content=detailContent;
            var track=Box("Scroll track",Details);
            Place(track,new Vector2(1,0),Vector2.one,new Vector2(-13,20),new Vector2(-7,-80));
            track.gameObject.AddComponent<Image>().color=new Color(0.2f,0.5f,0.6f,0.2f);
            var handle=Box("Scroll position",track); var image=handle.gameObject.AddComponent<Image>(); image.color=Accent;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect=handle; scrollbar.targetGraphic=image;
            scrollbar.direction=Scrollbar.Direction.BottomToTop; scrollbar.navigation=new Navigation {mode=Navigation.Mode.None};
            ArticleScroll.verticalScrollbar=scrollbar;
        }

        private void BuildNavigation()
        {
            int count=explorer.Bodies.Length;
            NavigationButtons=new Button[count]; labels=new RectTransform[count];
            for(int i=0;i<count;i++)
            {
                int index=i;
                var label=theme.Button(explorer.Bodies[i].Data.name,root,()=>explorer.SelectPlanet(index),Color.clear);
                label.GetComponent<HudPanel>().Border=Color.clear;
                label.gameObject.AddComponent<PlanetHover>().Initialize(explorer,index);
                labels[i]=label.GetComponent<RectTransform>(); labels[i].anchorMin=labels[i].anchorMax=Vector2.one*0.5f;
                labels[i].sizeDelta=new Vector2(110,44);
                label.GetComponentInChildren<Text>().fontSize=15; label.GetComponentInChildren<Text>().text=explorer.Bodies[i].Data.name.ToUpperInvariant();
            }
            dock=Box("Planet navigation",root); theme.Surface(dock,new Color(0.01f,0.027f,0.045f,0.94f));
            navigationScroll=dock.gameObject.AddComponent<ScrollRect>(); navigationScroll.horizontal=true; navigationScroll.vertical=false;
            navigationScroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box("Navigation viewport",dock); viewport.offsetMin=new Vector2(6,4); viewport.offsetMax=new Vector2(-6,-4);
            viewport.gameObject.AddComponent<Image>().color=Color.white; viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            navContent=Box("Destinations",viewport); navContent.anchorMin=Vector2.zero; navContent.anchorMax=Vector2.up; navContent.pivot=new Vector2(0,0.5f);
            Row(navContent,4,false); navContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
            navigationScroll.viewport=viewport; navigationScroll.content=navContent;
            for(int slot=0;slot<count;slot++)
            {
                int index=slot==0?SolarSystemBootstrap.SunIndex:slot-1;
                var data=explorer.Bodies[index].Data;
                var button=theme.Button(data.name.ToUpperInvariant(),navContent,()=>explorer.SelectPlanet(index),Color.clear);
                button.name="Navigate "+data.name; NavigationButtons[index]=button;
                button.gameObject.AddComponent<LayoutElement>().preferredWidth=100;
                button.gameObject.AddComponent<PlanetHover>().Initialize(explorer,index);
                var dot=Box("Body color",button.transform);
                Place(dot,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(-13,0),new Vector2(13,2));
                var bar=dot.gameObject.AddComponent<Image>(); bar.color=data.Accent; bar.raycastTarget=false;
            }
            Details.SetAsLastSibling();
        }

        public void ApplyLayout(int width,int height,Rect safe,bool touch)
        {
            lastWidth=width; lastHeight=height; lastSafe=safe; touchLayout=touch;
            IsPortrait=(float)width/Mathf.Max(1,height)<1.2f;
            float logicalWidth=IsPortrait?((float)width/height<0.65f?430:768):(touch?((float)width/height>1.9f?width*430f/height:1024):1440);
            Canvas.scaleFactor=width/logicalWidth;
            safeViewport=new Rect(safe.x/width,safe.y/height,safe.width/width,safe.height/height);
            root.anchorMin=safeViewport.min; root.anchorMax=safeViewport.max; root.offsetMin=root.offsetMax=Vector2.zero;
            float margin=touch || IsPortrait?12:36;
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(margin,IsPortrait?-110:-76),new Vector2(-margin,-12));
            brand.fontSize=IsPortrait?18:23;
            Place(brand.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(0,-27),new Vector2(0,0));
            Place(locationText.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(0,-46),new Vector2(0,-29));
            if(IsPortrait) Place(controls,new Vector2(0,1),Vector2.one,new Vector2(0,-98),new Vector2(0,-50));
            else
            {
                Place(brand.rectTransform,new Vector2(0,1),new Vector2(0.40f,1),new Vector2(0,-30),Vector2.zero);
                Place(controls,new Vector2(0.43f,0),Vector2.one,new Vector2(0,5),new Vector2(0,-5));
            }
            Place(dock,new Vector2(touch || IsPortrait?0:0.16f,0),Vector2.right,new Vector2(margin,27),new Vector2(-margin,85));
            Place(footer.rectTransform,Vector2.zero,Vector2.right,new Vector2(margin,3),new Vector2(-margin,24));
            footer.fontSize=IsPortrait && logicalWidth<500?9:10;
            if(IsPortrait)
            {
                Place(Details,Vector2.zero,new Vector2(1,0.49f),new Vector2(margin,98),new Vector2(-margin,0));
                Place(hint.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(margin,-143),new Vector2(-margin,-121));
            }
            else
            {
                Place(Details,new Vector2(touch?0.62f:0.68f,0),Vector2.one,new Vector2(0,98),new Vector2(-margin,-88));
                Place(hint.rectTransform,Vector2.zero,new Vector2(0.60f,0),new Vector2(margin,89),new Vector2(0,109));
            }
            UnityEngine.Canvas.ForceUpdateCanvases();
            float navWidth=Mathf.Max(84,(dock.rect.width-12-4*8)/9);
            foreach(var button in NavigationButtons) button.GetComponent<LayoutElement>().preferredWidth=navWidth;
            LayoutRebuilder.ForceRebuildLayoutImmediate(navContent); LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
            navigationScroll.horizontalNormalizedPosition=0;
            RefreshControls(); UpdateHint();
        }

        public void ShowPlanet(PlanetBody body)
        {
            ArticleScroll.StopMovement();
            for(int i=detailContent.childCount-1;i>=0;i--)
            { var child=detailContent.GetChild(i); child.SetParent(null); Destroy(child.gameObject); }
            var data=body.Data;
            FactText(data.category.ToUpperInvariant(),13,Accent,true);
            FactText(data.name.ToUpperInvariant(),44,Ink,true); FactText(data.description,18,Ink);
            var group=Box("Key statistics",detailContent); group.gameObject.AddComponent<LayoutElement>().preferredHeight=188;
            if(data.statistics!=null && data.statistics.Length==4)
            { for(int i=0;i<4;i++) Stat(group,data.statistics[i].label,data.statistics[i].value,data.statistics[i].unit,i%2,i/2); }
            else
            {
                Stat(group,"DIAMETER",data.diameterKm.ToString("N0",CultureInfo.InvariantCulture),"km · equatorial",0,0);
                Stat(group,"GRAVITY",data.gravity.ToString("0.0",CultureInfo.InvariantCulture),"m/s² · effective",1,0);
                Stat(group,"ONE YEAR",data.orbitalDays.ToString("N1",CultureInfo.InvariantCulture),"Earth days",0,1);
                Stat(group,"FROM THE SUN",data.distanceMillionKm.ToString("N1",CultureInfo.InvariantCulture),"million km · mean",1,1);
            }
            if(data.sections!=null) foreach(var section in data.sections)
            { FactText(section.heading.ToUpperInvariant(),16,Accent,true); FactText(section.body,18,Ink); }
            FactText("IN PERSPECTIVE",14,Accent,true); FactText(data.highlight,18,Ink);
            FactText("Source: NASA. Rewritten for this explorer; reviewed October 8, 2026. Values are rounded. Scale, colors and animation speeds are illustrative."+(body.Index==8?"":" Giant-planet gravity refers to the 1-bar atmospheric level."),13,Muted);
            SourceButton=theme.Button("READ THE NASA ARTICLE  >",detailContent,()=>{ if(!explorer.InteractionLocked) Application.OpenURL(data.sourceUrl); });
            SourceButton.name="NASA source"; SourceButton.gameObject.AddComponent<LayoutElement>().preferredHeight=52;
            Details.gameObject.SetActive(true); locationText.text="CELESTIAL ATLAS / "+data.name.ToUpperInvariant();
            UnityEngine.Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
            ArticleScroll.verticalNormalizedPosition=1; RefreshControls(); UpdateHint();
        }

        public void ShowOverview()
        { Details.gameObject.SetActive(false); locationText.text="OPEN SPACE / HIGH OVERVIEW"; RefreshControls(); UpdateHint(); }
        private void UpdateHint()
        {
            if(hint==null) return;
            if(explorer.InteractionLocked) hint.text="TRAVELING / CONTROLS RESUME ON ARRIVAL";
            else if(explorer.SelectedIndex<0) hint.text=touchLayout?"TAP A WORLD TO EXPLORE":"SELECT A WORLD TO EXPLORE";
            else hint.text=touchLayout?"ONE FINGER: ROTATE / TWO FINGERS: PINCH ZOOM":
                (explorer.SelectedIndex<0?"SELECT A WORLD / DRAG TO ORBIT / SCROLL TO ZOOM":"DRAG TO ORBIT / SCROLL TO ZOOM / ESC TO RETURN");
            hint.fontSize=IsPortrait?11:12;
        }

        public void RefreshControls()
        {
            RefreshInteractionState();
            pauseText.text=explorer.Paused?"RESUME":"PAUSE"; motionText.text=explorer.CameraRig.ReduceMotion?"INSTANT":"SMOOTH";
            viewText.text=explorer.CameraRig.IsOverhead?"ANGLED":"TOP VIEW";
            pathsText.text=IsPortrait?(explorer.OrbitsVisible?"ORBITS":"NO ORBITS"):(explorer.OrbitsVisible?"ORBITS ON":"ORBITS OFF");
            for(int i=0;i<NavigationButtons.Length;i++)
            {
                bool selected=i==explorer.SelectedIndex; var panel=NavigationButtons[i].GetComponent<HudPanel>();
                panel.color=selected?new Color(0.04f,0.22f,0.28f,0.85f):new Color(0.015f,0.045f,0.066f,0.3f);
                panel.Border=selected?Accent:Color.clear; panel.SetVerticesDirty();
                NavigationButtons[i].GetComponentInChildren<Text>().color=selected?Accent:Ink;
            }
        }

        private void LateUpdate()
        {
            if(Canvas==null || !Canvas.gameObject.activeInHierarchy) return;
            RefreshInteractionState();
            if(AutomaticLayout && (lastWidth!=Screen.width || lastHeight!=Screen.height || lastSafe!=Screen.safeArea))
                ApplyLayout(Screen.width,Screen.height,Screen.safeArea,Application.isMobilePlatform || Input.touchSupported);
            RefreshProjectedLabels();
        }
        public void RefreshInteractionState()
        {
            bool locked=explorer.InteractionLocked;
            if(interactionGroup==null || (wasLocked==locked && interactionGroup.interactable==!locked)) return;
            wasLocked=locked;
            interactionGroup.interactable=!locked; interactionGroup.blocksRaycasts=!locked;
            if(ArticleScroll!=null) { ArticleScroll.StopMovement(); ArticleScroll.enabled=!locked; }
            if(navigationScroll!=null) { navigationScroll.StopMovement(); navigationScroll.enabled=!locked; }
            if(locked && UnityEngine.EventSystems.EventSystem.current!=null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UpdateHint();
        }
        private Vector2 CanvasPoint(Vector3 point)
        {
            return new Vector2(((point.x-safeViewport.x)/safeViewport.width-0.5f)*root.rect.width,
                ((point.y-safeViewport.y)/safeViewport.height-0.5f)*root.rect.height);
        }
        public void RefreshProjectedLabels()
        {
            if(explorer==null) return;
            bool focused=explorer.SelectedIndex>=0;
            reticle.gameObject.SetActive(focused || explorer.CameraRig.FocusBlend<0.1f);
            if(reticle.gameObject.activeSelf)
            {
                int index=focused?explorer.SelectedIndex:(explorer.HoveredIndex>=0?explorer.HoveredIndex:2);
                var body=explorer.Bodies[index]; var center=view.WorldToViewportPoint(body.transform.position);
                float padding=index==5?2.35f:(focused?1.18f:1.035f);
                var edge=view.WorldToViewportPoint(body.transform.position+view.transform.up*body.Data.visualRadius*padding);
                reticle.anchoredPosition=CanvasPoint(center);
                reticle.sizeDelta=Vector2.one*(Mathf.Abs(edge.y-center.y)*root.rect.height/safeViewport.height*2+12);
                if(index==5)
                {
                    // The ring ellipse needs a rectangular frame, not a sphere-sized square.
                    Vector2 minimum=Vector2.one*float.PositiveInfinity,maximum=Vector2.one*float.NegativeInfinity;
                    for(int sample=0;sample<32;sample++)
                    {
                        float angle=sample*Mathf.PI*2/32;
                        Vector3 ring=SolarLayout.SaturnTilt*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*body.Data.visualRadius*2.35f;
                        Vector2 point=CanvasPoint(view.WorldToViewportPoint(body.transform.position+ring));
                        minimum=Vector2.Min(minimum,point); maximum=Vector2.Max(maximum,point);
                        Vector3 surface=(view.transform.right*Mathf.Cos(angle)+view.transform.up*Mathf.Sin(angle))*body.Data.visualRadius*1.05f;
                        point=CanvasPoint(view.WorldToViewportPoint(body.transform.position+surface));
                        minimum=Vector2.Min(minimum,point); maximum=Vector2.Max(maximum,point);
                    }
                    reticle.anchoredPosition=(minimum+maximum)*0.5f;
                    reticle.sizeDelta=maximum-minimum+Vector2.one*12;
                }
                var graphic=reticle.GetComponent<HudReticle>(); graphic.BracketsOnly=true;
                graphic.color=new Color(Accent.r,Accent.g,Accent.b,focused?0.20f:0.66f); graphic.SetVerticesDirty();
            }
            for(int i=0;i<labels.Length;i++)
            {
                bool visible=!focused && explorer.CameraRig.FocusBlend<0.1f && (i==2 || i==4 || i==5 || i==8 || explorer.HoveredIndex==i);
                labels[i].gameObject.SetActive(visible); if(!visible) continue;
                var body=explorer.Bodies[i]; var point=view.WorldToViewportPoint(body.transform.position);
                if(point.z<=0) { labels[i].gameObject.SetActive(false); continue; }
                var rim=view.WorldToViewportPoint(body.transform.position-view.transform.up*body.Data.visualRadius*(i==5?1.85f:1.04f));
                Vector2 position=CanvasPoint(rim)-Vector2.up*20;
                position.x=Mathf.Clamp(position.x,-root.rect.width*0.5f+55,root.rect.width*0.5f-55);
                position.y=Mathf.Clamp(position.y,-root.rect.height*0.5f+125,root.rect.height*0.5f-150);
                for(int pass=0;pass<9;pass++) for(int j=0;j<i;j++)
                    if(labels[j].gameObject.activeSelf && Mathf.Abs(position.x-labels[j].anchoredPosition.x)<108 && Mathf.Abs(position.y-labels[j].anchoredPosition.y)<42)
                        position.y=labels[j].anchoredPosition.y+43;
                // In a short phone view, retain the destination tab instead of laying a label over its world.
                if(new Rect(position-labels[i].sizeDelta*0.5f,labels[i].sizeDelta).Contains(CanvasPoint(point))
                    || position.y>root.rect.height*0.5f-(IsPortrait?145:80))
                { labels[i].gameObject.SetActive(false); continue; }
                labels[i].anchoredPosition=position;
            }
        }
        private void FactText(string value,int size,Color color,bool strong=false)
        { var text=theme.Label(value,detailContent,size,color,strong); text.gameObject.AddComponent<LayoutElement>().minHeight=size+5; }
        private void Stat(RectTransform parent,string title,string value,string unit,int x,int y)
        {
            var box=Box(title,parent);
            Place(box,new Vector2(x*0.5f,1-(y+1)*0.5f),new Vector2((x+1)*0.5f,1-y*0.5f),new Vector2(x==0?0:5,5),new Vector2(x==0?-5:0,-5));
            theme.Surface(box,new Color(0.035f,0.105f,0.13f,0.8f),false).Cut=6;
            var label=theme.Label(title,box,11,Muted,true);
            Place(label.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(10,-26),new Vector2(-6,-8));
            var number=theme.Label(value,box,26,Ink,true);
            Place(number.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(10,-59),new Vector2(-6,-28));
            var units=theme.Label(unit,box,12,Muted);
            Place(units.rectTransform,Vector2.zero,Vector2.right,new Vector2(10,8),new Vector2(-6,27));
        }
        private static void Row(RectTransform rect,float spacing,bool expand)
        {
            var row=rect.gameObject.AddComponent<HorizontalLayoutGroup>(); row.spacing=spacing;
            row.childControlWidth=row.childControlHeight=true; row.childForceExpandWidth=expand; row.childForceExpandHeight=true;
        }
        private void OnDestroy() { theme?.Dispose(); }
    }
}
