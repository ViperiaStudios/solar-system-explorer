Shader "SolarExplorer/SunGlow"
{
    Properties { _AmbientTime ("Animation clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _AmbientTime;
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                float3 center=unity_ObjectToWorld._m03_m13_m23;
                float scale=length(unity_ObjectToWorld._m00_m10_m20);
                float3 world=center+mul((float3x3)UNITY_MATRIX_I_V,float3(v.vertex.xy,0))*scale;
                o.vertex=mul(UNITY_MATRIX_VP,float4(world,1)); o.uv=v.texcoord.xy*2-1; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float radius=length(i.uv);
                float angle=atan2(i.uv.y,i.uv.x);
                float rays=pow(saturate(sin(angle*23+sin(angle*11+_AmbientTime*0.10)*2)),4);
                float glow=exp(-radius*radius*18)*(1-smoothstep(0.3,0.8,radius));
                float corona=exp(-abs(radius-0.255)*42)*(0.65+rays*0.35);
                float streak=rays*exp(-radius*radius*24)*0.06;
                return fixed4(1,0.42,0.075,(glow*0.17+corona*0.32+streak)*(1-smoothstep(0.7,1,radius)));
            }
            ENDCG
        }
    }
}
