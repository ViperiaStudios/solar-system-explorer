Shader "SolarExplorer/GalaxyStars"
{
    Properties { _Clock ("Clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Clock;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 size:TEXCOORD1; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o; float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                float scale=length(unity_ObjectToWorld._m00_m10_m20);
                world+=(UNITY_MATRIX_I_V._m00_m10_m20*v.uv.x+UNITY_MATRIX_I_V._m01_m11_m21*v.uv.y)*v.size.x*scale;
                o.pos=mul(UNITY_MATRIX_VP,float4(world,1)); o.uv=v.uv;
                o.color=v.color*(0.94+0.06*sin(_Clock*0.4+v.size.y)); return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float r=dot(i.uv,i.uv);
                float glow=exp(-r*5)*smoothstep(1,0.4,r);
                return half4(i.color.rgb*glow,1);
            }
            ENDCG
        }
    }
}
