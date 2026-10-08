Shader "SolarExplorer/GalaxySky"
{
    Properties { _MainTex ("Milky Way", 2D) = "black" {} _AmbientTime ("Animation clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            sampler2D _MainTex;
            float _AmbientTime;
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p)
            {
                float3 a=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);
                float3 p=float3(d.x, d.y*0.5+d.z*0.866, -d.y*0.866+d.z*0.5);
                p.xy=float2(p.x*0.866+p.y*0.5,-p.x*0.5+p.y*0.866);
                float2 uv=float2(atan2(p.z,p.x)/6.2831853+0.33,asin(clamp(p.y,-1,1))/3.14159265+0.5);
                float3 stars=tex2D(_MainTex,uv).rgb;
                float drift=_AmbientTime*0.0015;
                float cloud=noise(p*5+drift)*0.57+noise(p*11+4)*0.28+noise(p*23)*0.15;
                float dust=pow(saturate(1-abs(p.y+0.12)*2),3)*smoothstep(0.28,0.78,cloud);
                float3 nebula=lerp(float3(0.03,0.065,0.16),float3(0.16,0.055,0.16),noise(p*3+17));
                // Do not amplify the JPEG's near-black compression noise.
                stars=pow(max(stars-0.001,0),1.35)*0.45*float3(0.68,0.84,1.2);
                return float4(float3(0.0004,0.0006,0.0016)+stars+nebula*dust*0.02,1);
            }
            ENDCG
        }
    }
}
