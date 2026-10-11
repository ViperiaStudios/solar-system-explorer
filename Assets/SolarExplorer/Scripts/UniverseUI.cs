using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SolarExplorer.InterfaceTheme;

namespace SolarExplorer
{
    [DefaultExecutionOrder(110)]
    public sealed class UniverseUI : MonoBehaviour
    {
        private UniverseExplorer explorer;
        private Camera view;
        private InterfaceTheme theme;
        private RectTransform root,header,controls,dock,navContent,facts;
        private CanvasGroup interaction;
        private Image curtain;
        private Text location,hint,footer,backText,pauseText,motionText;
        private ScrollRect navigation;
        private readonly List<KeyValuePair<AtlasTarget,RectTransform>> markers=new List<KeyValuePair<AtlasTarget,RectTransform>>();
        private int lastWidth,lastHeight;
        private Rect lastSafe,safeViewport=new Rect(0,0,1,1);
        public Canvas Canvas { get; private set; }
        public RectTransform Details { get; private set; }
        public Button PrimaryButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Button BackButton { get; private set; }
        public Button SourceButton { get; private set; }
        public ScrollRect ArticleScroll { get; private set; }
        public readonly List<Button> NavigationButtons=new List<Button>();
        public bool AutomaticLayout { get; set; }=true;
        public bool Portrait { get; private set; }

        public void Initialize(UniverseExplorer owner,Camera camera)
        {
            explorer=owner; view=camera; theme=new InterfaceTheme();
            var obj=new GameObject("Universe UI",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)); obj.transform.SetParent(transform,false);
            Canvas=obj.GetComponent<Canvas>(); Canvas.renderMode=RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder=10; Canvas.pixelPerfect=true;
            root=Box("Universe safe area",obj.transform); interaction=root.gameObject.AddComponent<CanvasGroup>();
            Box("Atlas edge shading",root).gameObject.AddComponent<HudScrim>().raycastTarget=false;
            header=Box("Universe header",root);
            var brand=theme.Label("U N I V E R S E  /  E X P L O R E R",header,20,Ink,true);
            Place(brand.rectTransform,new Vector2(0,1),new Vector2(0.43f,1),new Vector2(0,-28),Vector2.zero);
            brand.name="Brand";
            location=theme.Label("",header,12,Accent);
            Place(location.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(0,-48),new Vector2(0,-28));
            controls=Box("Atlas controls",header); Row(controls,true);
            BackButton=Control("UP",controls,explorer.Back); backText=BackButton.GetComponentInChildren<Text>();
            Control("HOME",controls,explorer.Home);
            Control("RESET",controls,()=>explorer.Owner.CameraRig.ResetView());
            pauseText=Control("PAUSE",controls,explorer.Owner.TogglePause).GetComponentInChildren<Text>();
            motionText=Control("SMOOTH",controls,explorer.Owner.ToggleMotion).GetComponentInChildren<Text>();
            Details=Box("Atlas information",root); theme.Surface(Details,Panel);
            PrimaryButton=Control("EXPLORE",Details,()=>{});
            Place(PrimaryButton.GetComponent<RectTransform>(),new Vector2(0,1),Vector2.one,new Vector2(16,-64),new Vector2(-80,-16));
            CloseButton=Control("X",Details,explorer.Back); CloseButton.GetComponentInChildren<Text>().fontSize=25;
            Place(CloseButton.GetComponent<RectTransform>(),Vector2.one,Vector2.one,new Vector2(-64,-64),new Vector2(-16,-16));
            var region=Box("Atlas scroll region",Details); Place(region,Vector2.zero,Vector2.one,new Vector2(20,20),new Vector2(-26,-80));
            ArticleScroll=region.gameObject.AddComponent<ScrollRect>(); ArticleScroll.horizontal=false; ArticleScroll.scrollSensitivity=32; ArticleScroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box("Atlas viewport",region); viewport.gameObject.AddComponent<Image>().color=Color.white; viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            facts=Box("Atlas facts",viewport); Place(facts,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); facts.pivot=new Vector2(0.5f,1);
            var column=facts.gameObject.AddComponent<VerticalLayoutGroup>(); column.spacing=14; column.childControlWidth=column.childControlHeight=true; column.childForceExpandWidth=true; column.childForceExpandHeight=false;
            facts.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            ArticleScroll.viewport=viewport; ArticleScroll.content=facts;
            var track=Box("Atlas scroll track",Details); Place(track,new Vector2(1,0),Vector2.one,new Vector2(-13,20),new Vector2(-7,-80));
            track.gameObject.AddComponent<Image>().color=new Color(0.2f,0.5f,0.6f,0.2f);
            var handle=Box("Atlas scroll thumb",track); var image=handle.gameObject.AddComponent<Image>(); image.color=Accent;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect=handle; scrollbar.targetGraphic=image; scrollbar.direction=Scrollbar.Direction.BottomToTop;
            ArticleScroll.verticalScrollbar=scrollbar;
            dock=Box("Atlas destinations",root); theme.Surface(dock,Panel);
            navigation=dock.gameObject.AddComponent<ScrollRect>(); navigation.horizontal=true; navigation.vertical=false; navigation.movementType=ScrollRect.MovementType.Clamped;
            var navViewport=Box("Atlas destinations viewport",dock); navViewport.offsetMin=new Vector2(6,4); navViewport.offsetMax=new Vector2(-6,-4);
            navViewport.gameObject.AddComponent<Image>().color=Color.white; navViewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            navContent=Box("Atlas destination buttons",navViewport); navContent.anchorMin=Vector2.zero; navContent.anchorMax=Vector2.up; navContent.pivot=new Vector2(0,0.5f);
            Row(navContent,false); navContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
            navigation.viewport=navViewport; navigation.content=navContent;
            hint=theme.Label("",root,11,Muted);
            footer=theme.Label("ILLUSTRATED ATLAS / SIZES AND SPACING ADJUSTED",root,10,Muted);
            var overlay=Box("Scale transition",obj.transform); curtain=overlay.gameObject.AddComponent<Image>(); curtain.color=Color.clear; curtain.raycastTarget=false;
            ApplyLayout(Screen.width,Screen.height,Screen.safeArea);
            Canvas.gameObject.SetActive(false);
        }

        private Button Control(string title,Transform parent,Action action)
        {
            var button=theme.Button(title,parent,()=>{if(!explorer.Owner.InteractionLocked) action();});
            button.GetComponentInChildren<Text>().fontSize=13; return button;
        }
        private static void Row(RectTransform rect,bool expand)
        {
            var row=rect.gameObject.AddComponent<HorizontalLayoutGroup>(); row.spacing=5;
            row.childControlHeight=row.childControlWidth=true; row.childForceExpandHeight=true; row.childForceExpandWidth=expand;
        }
        private static void Clear(RectTransform parent)
        {
            for(int i=parent.childCount-1;i>=0;i--) { var child=parent.GetChild(i); child.SetParent(null); Destroy(child.gameObject); }
        }
        private Text TextBlock(string text,int size,Color color,bool strong=false)
        {
            var label=theme.Label(text,facts,size,color,strong); label.gameObject.AddComponent<LayoutElement>().minHeight=size+5; return label;
        }

        public void Show(AtlasEntry article,string actionTitle,Action action,List<AtlasNavigation> destinations)
        {
            SetHUDVisible(true); Clear(facts); Clear(navContent); NavigationButtons.Clear(); ArticleScroll.StopMovement();
            TextBlock(article.category.ToUpperInvariant(),13,Accent,true);
            TextBlock(article.name.ToUpperInvariant(),36,Ink,true);
            TextBlock(article.summary,18,Ink);
            TextBlock(article.facts,17,Accent,true);
            foreach(var section in article.sections) { TextBlock(section.heading.ToUpperInvariant(),15,Accent,true); TextBlock(section.body,18,Ink); }
            TextBlock("ABOUT THIS VIEW",14,Accent,true);
            TextBlock("Original procedural artwork. Galaxy shapes, positions and sizes are illustrative; exoplanet surfaces are imagined. Facts are original summaries of NASA sources, reviewed October 8, 2026. Values are rounded.",13,Muted);
            SourceButton=Control("READ THE NASA SOURCE  >",facts,()=>Application.OpenURL(article.sourceUrl));
            SourceButton.gameObject.AddComponent<LayoutElement>().preferredHeight=52;
            PrimaryButton.onClick.RemoveAllListeners(); PrimaryButton.onClick.AddListener(()=>{ if(!explorer.Owner.InteractionLocked) action(); });
            PrimaryButton.GetComponentInChildren<Text>().text=actionTitle;
            foreach(var item in destinations)
            {
                string id=item.Id;
                var button=Control(item.Label,navContent,()=>explorer.Select(id)); button.name="Atlas navigate "+id;
                button.gameObject.AddComponent<LayoutElement>().preferredWidth=Mathf.Clamp(item.Label.Length*7.8f+25,65,205);
                NavigationButtons.Add(button);
            }
            location.text=explorer.InStarSystem?"MILKY WAY / "+explorer.SystemId.Replace('-',' ').ToUpperInvariant():"COSMIC ATLAS / "+article.name.ToUpperInvariant();
            foreach(var marker in markers) if(marker.Value!=null) { marker.Value.gameObject.SetActive(false); marker.Value.SetParent(null); Destroy(marker.Value.gameObject); }
            markers.Clear();
            if(explorer.CurrentScene!=null) foreach(var target in explorer.CurrentScene.Targets)
            {
                string id=target.Id; string title=explorer.LabelFor(id);
                if(explorer.InStarSystem) title=title.Replace("TRAPPIST-1 ","").Replace("PROXIMA CENTAURI B","PROXIMA B");
                var label=Control(title,root,()=>explorer.Select(id));
                label.GetComponentInChildren<Text>().fontSize=title.Length>19?11:13;
                var panel=label.GetComponent<HudPanel>(); panel.color=new Color(0.012f,0.043f,0.065f,0.38f); panel.Border=new Color(0.3f,0.8f,1,0.24f);
                var rect=label.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=Vector2.one*0.5f; rect.sizeDelta=new Vector2(Mathf.Clamp(title.Length*7+20,44,184),44); rect.SetSiblingIndex(1);
                markers.Add(new KeyValuePair<AtlasTarget,RectTransform>(target,rect));
            }
            UnityEngine.Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(facts); LayoutRebuilder.ForceRebuildLayoutImmediate(navContent);
            ArticleScroll.verticalNormalizedPosition=1; navigation.horizontalNormalizedPosition=0; Refresh(); RefreshMarkers();
        }

        public void ApplyLayout(int width,int height,Rect safe)
        {
            lastWidth=width; lastHeight=height; lastSafe=safe; Portrait=(float)width/height<1.2f;
            float logical=Portrait?((float)width/height<0.65f?430:768):((float)width/height>1.95f?width*430f/height:1440);
            Canvas.scaleFactor=width/logical; safeViewport=new Rect(safe.x/width,safe.y/height,safe.width/width,safe.height/height);
            root.anchorMin=safeViewport.min; root.anchorMax=safeViewport.max; root.offsetMin=root.offsetMax=Vector2.zero;
            float margin=Portrait?12:30;
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(margin,Portrait?-110:-76),new Vector2(-margin,-12));
            var brand=header.Find("Brand").GetComponent<Text>(); brand.fontSize=Portrait?17:20;
            Place(brand.rectTransform,new Vector2(0,1),new Vector2(Portrait?1:0.43f,1),new Vector2(0,-28),Vector2.zero);
            if(Portrait) Place(controls,new Vector2(0,1),Vector2.one,new Vector2(0,-98),new Vector2(0,-50));
            else Place(controls,new Vector2(0.46f,0),Vector2.one,new Vector2(0,5),new Vector2(0,-5));
            Place(dock,Vector2.zero,Vector2.right,new Vector2(margin,28),new Vector2(-margin,85));
            Place(footer.rectTransform,Vector2.zero,Vector2.right,new Vector2(margin,3),new Vector2(-margin,25)); footer.fontSize=Portrait?9:10;
            if(Portrait)
            {
                Place(Details,Vector2.zero,new Vector2(1,0.49f),new Vector2(margin,98),new Vector2(-margin,0));
                Place(hint.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(margin,-143),new Vector2(-margin,-120));
            }
            else
            {
                Place(Details,new Vector2(0.68f,0),Vector2.one,new Vector2(0,98),new Vector2(-margin,-88));
                Place(hint.rectTransform,Vector2.zero,new Vector2(0.66f,0),new Vector2(margin,89),new Vector2(0,112));
            }
            UnityEngine.Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(facts); Refresh();
        }

        public void BeginCrossing() { Canvas.gameObject.SetActive(true); SetHUDVisible(explorer.View!=UniverseView.SolarSystem); SetCurtain(0); Refresh(); }
        public void SetHUDVisible(bool visible) { root.gameObject.SetActive(visible); }
        public void SetCurtain(float alpha) { curtain.color=new Color(0.0015f,0.003f,0.009f,alpha); curtain.raycastTarget=explorer.IsCrossing; }
        public void Refresh()
        {
            bool locked=explorer.Owner.InteractionLocked;
            interaction.interactable=!locked; interaction.blocksRaycasts=!locked;
            if(locked) { ArticleScroll.StopMovement(); navigation.StopMovement(); }
            ArticleScroll.enabled=navigation.enabled=!locked;
            var mode=explorer.View;
            backText.text=mode==UniverseView.Planet?"SYSTEM":mode==UniverseView.StarSystem?"NEARBY STARS":mode==UniverseView.Neighborhood?"MILKY WAY":mode==UniverseView.LocalGroup?"SOLAR SYSTEM":"LOCAL GROUP";
            pauseText.text=explorer.Owner.Paused?"RESUME":"PAUSE"; motionText.text=explorer.Owner.CameraRig.ReduceMotion?"INSTANT":"SMOOTH";
            hint.text=locked?"TRAVELING / CONTROLS RESUME ON ARRIVAL":"DRAG TO ROTATE / PINCH OR SCROLL TO ZOOM";
        }
        public void RefreshMarkers()
        {
            for(int i=0;i<markers.Count;i++)
            {
                var pair=markers[i]; Vector3 p=view.WorldToViewportPoint(pair.Key.transform.position);
                bool visible=p.z>0 && !(explorer.View==UniverseView.Planet && explorer.SelectedId==pair.Key.Id);
                float offset=29;
                if(explorer.InStarSystem)
                {
                    Vector3 edge=view.WorldToViewportPoint(pair.Key.transform.position+view.transform.up*pair.Key.Radius);
                    offset+=Mathf.Abs(edge.y-p.y)*root.rect.height/safeViewport.height;
                }
                Vector2 point=new Vector2(((p.x-safeViewport.x)/safeViewport.width-0.5f)*root.rect.width,((p.y-safeViewport.y)/safeViewport.height-0.5f)*root.rect.height)-Vector2.up*offset;
                if(Portrait) visible&=p.x>0.05f && p.x<0.95f && p.y>0.54f && p.y<0.82f;
                else visible&=p.x>0.04f && p.x<0.62f && p.y>0.19f && p.y<0.86f;
                var bounds=new Rect(point-pair.Value.sizeDelta*0.5f,pair.Value.sizeDelta);
                for(int j=0;j<i;j++) if(markers[j].Value.gameObject.activeSelf)
                {
                    var previous=markers[j].Value;
                    if(bounds.Overlaps(new Rect(previous.anchoredPosition-previous.sizeDelta*0.5f,previous.sizeDelta))) visible=false;
                }
                pair.Value.gameObject.SetActive(visible); pair.Value.anchoredPosition=point;
            }
        }
        private void LateUpdate()
        {
            if(Canvas==null || !Canvas.gameObject.activeInHierarchy) return;
            if(AutomaticLayout && (lastWidth!=Screen.width || lastHeight!=Screen.height || lastSafe!=Screen.safeArea)) ApplyLayout(Screen.width,Screen.height,Screen.safeArea);
            Refresh(); RefreshMarkers();
        }
        private void OnDestroy() { theme?.Dispose(); }
    }
}
