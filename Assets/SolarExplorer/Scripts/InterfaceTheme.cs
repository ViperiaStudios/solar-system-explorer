using UnityEngine;
using UnityEngine.UI;

namespace SolarExplorer
{
    public sealed class InterfaceTheme
    {
        public static readonly Color Ink = new Color(0.87f,0.96f,1);
        public static readonly Color Muted = new Color(0.51f,0.67f,0.74f);
        public static readonly Color Accent = new Color(0.38f,0.88f,0.98f);
        public static readonly Color Panel = new Color(0.018f,0.055f,0.079f,0.985f);
        public readonly Font Regular = Resources.Load<Font>("Fonts/Barlow-Regular");
        public readonly Font Semibold = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");

        public HudPanel Surface(RectTransform rect, Color color, bool raycast=true)
        {
            var image=rect.gameObject.AddComponent<HudPanel>();
            image.color=color; image.raycastTarget=raycast; return image;
        }

        public Text Label(string value,Transform parent,int size,Color color,bool strong=false)
        {
            var rect=Box(value.Length>32?value.Substring(0,32):value,parent);
            var text=rect.gameObject.AddComponent<Text>();
            text.font=strong?Semibold:Regular;
            if(text.font==null) text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=value; text.fontSize=size; text.color=color; text.lineSpacing=1.12f;
            text.supportRichText=false; text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow;
            return text;
        }

        public Button Button(string value,Transform parent,UnityEngine.Events.UnityAction action,Color? color=null)
        {
            var rect=Box(value,parent); var image=Surface(rect,color??Panel);
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(1.5f,1.6f,1.7f);
            colors.pressedColor=new Color(0.6f,0.95f,1); button.colors=colors;
            button.navigation=new Navigation {mode=Navigation.Mode.None}; button.onClick.AddListener(action);
            var text=Label(value,rect,16,Ink,true); text.alignment=TextAnchor.MiddleCenter;
            text.rectTransform.offsetMin=new Vector2(8,3); text.rectTransform.offsetMax=new Vector2(-8,-3);
            return button;
        }

        public static RectTransform Box(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); Place(rect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); return rect;
        }

        public static void Place(RectTransform rect,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=offsetMin; rect.offsetMax=offsetMax;
        }

        public static RectTransform Rule(Transform parent,Vector2 from,Vector2 to,Color color,float width=1)
        {
            var rect=Box("HUD line",parent); rect.anchorMin=rect.anchorMax=Vector2.zero; rect.pivot=new Vector2(0,0.5f);
            rect.anchoredPosition=from; rect.sizeDelta=new Vector2((to-from).magnitude,width);
            rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);
            var image=rect.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false;
            return rect;
        }
        public void Dispose() { }
    }
}
