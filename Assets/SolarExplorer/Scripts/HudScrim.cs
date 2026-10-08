using UnityEngine;
using UnityEngine.UI;

namespace SolarExplorer
{
    // Soft edge shading keeps controls readable when a bright world passes behind them.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudScrim : MaskableGraphic
    {
        public HudScrim() { useLegacyMeshGeneration=false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r=GetPixelAdjustedRect();
            float[] positions={0,0.04f,0.16f,0.82f,0.96f,1};
            float[] opacity={0.82f,0.74f,0,0,0.74f,0.82f};
            for(int i=0;i<positions.Length;i++)
            {
                float y=Mathf.Lerp(r.yMin,r.yMax,positions[i]);
                var tint=new Color(0.003f,0.01f,0.018f,opacity[i]);
                vh.AddVert(new Vector3(r.xMin,y),tint,Vector2.zero);
                vh.AddVert(new Vector3(r.xMax,y),tint,Vector2.zero);
                if(i==0) continue;
                int n=i*2;
                vh.AddTriangle(n-2,n-1,n); vh.AddTriangle(n-1,n+1,n);
            }
        }
    }
}
