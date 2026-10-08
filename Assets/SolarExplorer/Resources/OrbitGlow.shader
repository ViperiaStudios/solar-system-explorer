Shader "SolarExplorer/OrbitGlow"
{
    Properties
    {
        _Color ("Orbit light",Color)=(0.3,0.72,1,1)
        _Opacity ("Visibility",Float)=1
        _Hover ("Highlight",Float)=0
        _AmbientTime ("Animation clock",Float)=0
    }
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
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color; float _Opacity,_Hover,_AmbientTime;
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float edge=abs(i.uv.y*2-1);
                float core=1-smoothstep(0.10,0.60+fwidth(edge)*0.25,edge);
                float halo=pow(saturate(1-edge),2)*0.24;
                float glint=pow(saturate(cos(i.uv.x*6.28318-_AmbientTime*0.11)),24)*0.13;
                float strength=(0.50+_Hover*0.43+glint)*_Opacity;
                return float4(lerp(_Color.rgb,float3(0.66,0.93,1),core*0.65),saturate(core+halo)*strength);
            }
            ENDCG
        }
    }
}
