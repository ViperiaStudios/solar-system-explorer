using UnityEngine;

namespace SolarExplorer
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicBloom : MonoBehaviour
    {
        private Material material;
        private void Awake()
        {
            var shader=Resources.Load<Shader>("CinematicBloom");
            if(shader!=null && shader.isSupported) material=new Material(shader);
        }
        private void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material==null) { Graphics.Blit(source,destination); return; }
            int w=Mathf.Max(1,source.width/2),h=Mathf.Max(1,source.height/2);
            var small=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGBHalf);
            var blur=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGBHalf);
            small.filterMode=blur.filterMode=FilterMode.Bilinear;
            try
            {
                Graphics.Blit(source,small,material,0);
                for(int i=0;i<3;i++)
                {
                    material.SetVector("_Direction",new Vector4(1f/w*(1+i),0,0,0));
                    Graphics.Blit(small,blur,material,1);
                    material.SetVector("_Direction",new Vector4(0,1f/h*(1+i),0,0));
                    Graphics.Blit(blur,small,material,1);
                }
                material.SetTexture("_BloomTex",small);
                Graphics.Blit(source,destination,material,2);
            }
            finally { RenderTexture.ReleaseTemporary(small); RenderTexture.ReleaseTemporary(blur); }
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
