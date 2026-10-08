Shader "SolarExplorer/Atmosphere"
{
    Properties { _Color ("Scattering", Color)=(0.2,0.5,1,1) _Overview ("Overview fill",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Blend SrcAlpha One
        Cull Front ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Overview;
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal), eye=normalize(_WorldSpaceCameraPos-i.world);
                float angle=abs(dot(n,eye));
                float limb=pow(saturate(1-angle),4)*smoothstep(0,0.22,angle);
                float daylight=saturate(dot(n,normalize(-i.world))*1.5+0.5);
                return float4(_Color.rgb*2,limb*max(daylight,_Overview*0.45)*_Color.a);
            }
            ENDCG
        }
    }
}
