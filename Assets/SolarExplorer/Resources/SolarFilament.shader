Shader "SolarExplorer/SolarFilament"
{
    Properties { _AmbientTime ("Animation clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            half4 frag(v2f i):SV_Target
            {
                float edge=pow(saturate(1-abs(i.uv.y*2-1)),2);
                float packet=pow(saturate(0.5+0.5*sin(i.uv.x*20-_AmbientTime*9)),4);
                float flow=0.14+packet*0.86;
                return half4(i.color.rgb*3,edge*flow*i.color.a);
            }
            ENDCG
        }
    }
}
