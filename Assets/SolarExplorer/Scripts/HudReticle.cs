using UnityEngine;
using UnityEngine.UI;

namespace SolarExplorer
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudReticle : MaskableGraphic
    {
        public HudReticle() { useLegacyMeshGeneration=false; }
        public bool BracketsOnly;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r=GetPixelAdjustedRect();
            float radius=Mathf.Min(r.width,r.height)*0.5f;
            if(BracketsOnly)
            {
                for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2)
                {
                    Vector2 corner=r.center+new Vector2(x,y)*radius;
                    Line(vh,corner,corner-new Vector2(x*17,0),1.5f);
                    Line(vh,corner,corner-new Vector2(0,y*17),1.5f);
                }
                return;
            }
            // Broken arcs and cardinal marks frame the selected world without covering it.
            for(int i=0;i<120;i++)
            {
                if(i%30>19) continue;
                float a=i*Mathf.PI*2/120,b=(i+1)*Mathf.PI*2/120;
                Line(vh,r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,r.center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,0.8f);
            }
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*0.5f;
                Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Line(vh,r.center+p*(radius+6),r.center+p*(radius+15),1.5f);
            }
        }
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*0.5f; int start=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero); vh.AddVert(b-n,color,Vector2.zero);
            vh.AddVert(b+n,color,Vector2.zero); vh.AddVert(a+n,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
    }
}
