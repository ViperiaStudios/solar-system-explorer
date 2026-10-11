Shader "SolarExplorer/ExoplanetSurface"
{
    Properties { _Color ("Color",Color)=(0.6,0.4,0.25,1) _Secondary ("Secondary",Color)=(0.13,0.11,0.10,1) _Seed ("Seed",Float)=0 _Star ("Star",Float)=0 _Clock ("Clock",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color,_Secondary; float _Seed,_Star,_Clock;
            struct v2f { float4 pos:SV_POSITION; float3 p:TEXCOORD0; float3 n:TEXCOORD1; float3 world:TEXCOORD2; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.p=v.vertex.xyz*2; o.n=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p)
            {
                float3 c=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(c),hash(c+float3(1,0,0)),f.x),lerp(hash(c+float3(0,1,0)),hash(c+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(c+float3(0,0,1)),hash(c+float3(1,0,1)),f.x),lerp(hash(c+float3(0,1,1)),hash(c+1),f.x),f.y),f.z);
            }
            float fbm(float3 p) { return noise(p)*0.52+noise(p*2.03)*0.26+noise(p*4.09)*0.14+noise(p*8.17)*0.08; }
            half4 frag(v2f i):SV_Target
            {
                float3 p=normalize(i.p),n=normalize(i.n),eye=normalize(_WorldSpaceCameraPos-i.world);
                float broad=fbm(p*3.5+_Seed*13);
                float terrain=fbm(p*10+broad*3+_Seed*13),fine=fbm(p*48+_Seed);
                if(_Star>0.5)
                {
                    float cells=fbm(p*25+float3(_Clock*0.09,0,_Clock*0.035));
                    return half4(_Color.rgb*(0.7+cells*2.5)+float3(1,0.25,0.04)*pow(saturate(cells*1.5),6),1);
                }
                float ridges=1-abs(terrain*2-1);
                float3 rock=lerp(_Secondary.rgb,_Color.rgb,smoothstep(0.26,0.72,broad*0.7+terrain*0.3));
                rock*=0.89+fine*0.22+ridges*0.10;
                float day=saturate(dot(n,normalize(-i.world)));
                float facing=saturate(dot(n,eye));
                float3 color=rock*(float3(0.035,0.042,0.060)+float3(1.22,1.02,0.86)*pow(day,0.82));
                color+=_Color.rgb*pow(1-facing,4)*saturate(day+0.12)*0.13;
                return half4(color,1);
            }
            ENDCG
        }
    }
}
