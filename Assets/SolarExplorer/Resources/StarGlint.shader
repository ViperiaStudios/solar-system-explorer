Shader "SolarExplorer/StarGlint"
{
    Properties { _AmbientTime ("Animation clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Background+10" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _AmbientTime;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv*2-1; o.color=v.color; return o; }
            half4 frag(v2f i):SV_Target
            {
                float r=length(i.uv);
                float core=exp(-r*r*22);
                float spikes=exp(-abs(i.uv.x)*32)*exp(-abs(i.uv.y)*3)+exp(-abs(i.uv.y)*32)*exp(-abs(i.uv.x)*3);
                float glint=(core+spikes*0.20)*(1-smoothstep(0.65,1,r));
                float pulse=0.92+0.08*sin(_AmbientTime*0.4+i.color.a*18);
                return half4(i.color.rgb*pulse,glint);
            }
            ENDCG
        }
    }
}
