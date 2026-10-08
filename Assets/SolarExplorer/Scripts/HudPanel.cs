using UnityEngine;
using UnityEngine.UI;

namespace SolarExplorer
{
    // Resolution independent cut-corner glass, with a thin inset outline.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudPanel : MaskableGraphic
    {
        public HudPanel() { useLegacyMeshGeneration=false; }
        public Color Border = new Color(0.23f,0.7f,0.8f,0.3f);
        public float Cut = 10;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r=GetPixelAdjustedRect();
            float cut=Mathf.Min(Cut,Mathf.Min(r.width,r.height)*0.25f);
            Vector2[] points={new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax-cut),
                new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin+cut)};
            vh.AddVert(r.center,color,Vector2.zero);
            for(int i=0;i<6;i++) vh.AddVert(points[i],color,Vector2.zero);
            for(int i=0;i<6;i++) vh.AddTriangle(0,i+1,(i+1)%6+1);
            for(int i=0;i<6;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%6],n=new Vector2(-(b-a).y,(b-a).x).normalized;
                int start=vh.currentVertCount;
                vh.AddVert(a,Border,Vector2.zero); vh.AddVert(b,Border,Vector2.zero);
                vh.AddVert(b+n,Border,Vector2.zero); vh.AddVert(a+n,Border,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
