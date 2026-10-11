Shader "SolarExplorer/GalaxyDisk"
{
    Properties { _Seed ("Seed",Float)=1 _Arms ("Arms",Float)=4 _Irregular ("Irregular",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend One One Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Seed,_Arms,_Irregular;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy*2-1; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            float fbm(float2 p) { return noise(p)*0.52+noise(p*2.03)*0.26+noise(p*4.07)*0.14+noise(p*8.13)*0.08; }
            half4 frag(v2f i):SV_Target
            {
                float2 p=i.uv; float r=length(p),a=atan2(p.y,p.x);
                float grain=fbm(p*38+_Seed*11),cloud=fbm(p*8+_Seed);
                float winding=(a-log(r+0.09)*2.0+_Seed)*_Arms;
                float arm=pow(saturate(0.5+0.5*cos(winding+(cloud-0.5)*2)),5);
                float dust=pow(saturate(0.5+0.5*cos(winding+0.50+(cloud-0.5)*2)),12);
                float edge=1-smoothstep(0.60,0.98,r);
                float envelope=exp(-r*2.9)*edge;
                float disk=(0.10+arm*1.15)*(0.25+grain*1.5)*envelope;
                disk*=1-dust*0.78;
                float core=exp(-length(p*float2(1.1,2.1))*15);
                float irregular=pow(cloud,2)*exp(-length(p*float2(1.15,0.80))*3.5)*2.6*edge;
                disk=lerp(disk,irregular,_Irregular); core*=1-_Irregular*0.82;
                core*=edge;
                float3 color=lerp(float3(0.35,0.55,1.1),float3(1.0,0.72,0.42),exp(-r*7))*disk;
                color+=float3(1.6,1.20,0.72)*core;
                color+=float3(0.8,0.14,0.27)*pow(saturate(grain-0.48)*2.2,4)*arm*envelope*0.5;
                return half4(color,1);
            }
            ENDCG
        }
    }
}
