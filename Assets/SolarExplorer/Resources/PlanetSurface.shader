Shader "SolarExplorer/PlanetSurface"
{
    Properties
    {
        _Color ("Primary",Color)=(0.3,0.6,0.9,1)
        _Secondary ("Secondary",Color)=(0.1,0.3,0.2,1)
        _Kind ("Planet index",Float)=2
        _MainTex ("Surface map",2D)="white" {}
        _UseTexture ("Use texture",Float)=0
        _NightTex ("Earth night lights",2D)="black" {}
        _NormalTex ("Earth terrain normals",2D)="bump" {}
        _SpecularTex ("Earth ocean mask",2D)="black" {}
        _CloudTex ("Cloud shadows",2D)="black" {}
        _RingTex ("Ring shadows",2D)="white" {}
        _AmbientTime ("Animation clock",Float)=0
        _Overview ("Overview fill",Float)=1
    }
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
            struct v2f { float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; float2 uv:TEXCOORD3; };
            sampler2D _MainTex,_NightTex,_NormalTex,_SpecularTex,_CloudTex,_RingTex;
            float4 _MainTex_TexelSize,_Color,_Secondary;
            float _Kind,_UseTexture,_Overview,_AmbientTime;
            float hash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float noise(float3 p)
            {
                float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
            }
            float fbm(float3 p) { return noise(p)*0.53+noise(p*2.07)*0.27+noise(p*4.13)*0.13+noise(p*8.31)*0.07; }
            v2f vert(appdata_base v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.local=v.vertex.xyz*2;
                o.normal=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.uv=v.texcoord.xy; return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float3 p=normalize(i.local),n=normalize(i.normal),eye=normalize(_WorldSpaceCameraPos-i.world);
                float2 uv=i.uv;
                float3 albedo=lerp(lerp(_Secondary.rgb,_Color.rgb,fbm(p*9)),tex2D(_MainTex,uv).rgb,_UseTexture);
                if(_Kind>7.5)
                {
                    float3 drift=float3(_AmbientTime*0.12,_AmbientTime*0.035,0);
                    float3 warp=float3(fbm(p*4+drift),fbm(p*4+13+drift),fbm(p*4+31+drift));
                    float cells=fbm((p*7+warp*2.8)*5+drift);
                    float2 flow=(warp.xy-0.5)*0.016;
                    albedo=tex2D(_MainTex,uv+flow).rgb;
                    float filaments=pow(saturate(1-abs(cells-0.51)*12),3);
                    float edge=pow(1-saturate(dot(n,eye)),3);
                    float3 plasma=albedo*float3(1.1,1.4,1.6)*(0.62+cells*0.72);
                    plasma+=float3(1.2,0.7,0.12)*(filaments*0.16+edge*0.28);
                    return half4(plasma,1);
                }
                float3 localTangent=normalize(float3(-p.z,0,p.x)+float3(0.0001,0,0));
                float3 tangent=normalize(UnityObjectToWorldDir(localTangent));
                float3 bitangent=normalize(UnityObjectToWorldDir(cross(localTangent,p)));
                bool earth=_Kind>1.5 && _Kind<2.5;
                if(earth && _UseTexture>0.5)
                {
                    float3 terrain=tex2D(_NormalTex,uv).rgb*2-1;
                    n=normalize(n*max(0.3,terrain.z)+tangent*terrain.x*0.34+bitangent*terrain.y*0.34);
                }
                else if(_Kind<0.5 || (_Kind>2.5 && _Kind<3.5))
                {
                    float h=dot(albedo,float3(0.25,0.55,0.2));
                    float hu=dot(tex2D(_MainTex,uv+float2(_MainTex_TexelSize.x*2,0)).rgb,float3(0.25,0.55,0.2));
                    float hv=dot(tex2D(_MainTex,uv+float2(0,_MainTex_TexelSize.y*2)).rgb,float3(0.25,0.55,0.2));
                    n=normalize(n-tangent*(hu-h)*2.7-bitangent*(hv-h)*2.7);
                }
                bool saturn=_Kind>4.5 && _Kind<5.5;
                if(saturn && _UseTexture>0.5)
                {
                    // Preserve the mapped belts, with restrained local contrast and
                    // filtered, gently advecting cloud structure for close inspection.
                    float3 surrounding=(tex2D(_MainTex,uv+float2(0,0.004)).rgb+tex2D(_MainTex,uv-float2(0,0.004)).rgb)*0.5;
                    albedo=max(0,albedo+(albedo-surrounding)*0.55);
                    float turbulence=fbm(p*float3(48,8,48)+float3(_AmbientTime*0.025,0,0));
                    float latitude=p.y*190+turbulence*2.5;
                    float resolved=1-smoothstep(0.4,1.8,fwidth(latitude));
                    albedo*=1+(sin(latitude)*0.022+(turbulence-0.5)*0.035)*resolved;
                }
                float3 sun=normalize(normalize(-i.world)+float3(-0.22,0.65,-0.2)*_Overview);
                float ndl=dot(n,sun),day=saturate(ndl);
                float3 fill=float3(0.012,0.019,0.035)+float3(0.009,0.014,0.025)*_Overview;
                if(saturn) fill+=float3(0.028,0.030,0.034);
                float3 lightColor=saturn?float3(1.26,1.20,1.10):float3(1.35,1.12,0.88);
                float3 lighting=fill+lightColor*pow(day,0.90);
                if(saturn)
                {
                    albedo*=float3(1.03,1.04,1.05);
                    float3 axis=normalize(UnityObjectToWorldDir(float3(0,1,0)));
                    float3 center=unity_ObjectToWorld._m03_m13_m23;
                    float3 relative=i.world-center,lightRay=normalize(-i.world);
                    float denominator=dot(axis,lightRay);
                    if(abs(denominator)>0.001)
                    {
                        float t=-dot(relative,axis)/denominator;
                        float radius=length(unity_ObjectToWorld._m00_m10_m20)*0.5;
                        float ring=length(relative+lightRay*t)/radius-1.3;
                        if(t>0 && ring>0 && ring<1)
                            lighting=fill+(lighting-fill)*(1-tex2D(_RingTex,float2(ring,0.5)).a*0.8);
                    }
                }
                if(earth)
                {
                    float cloud=tex2D(_CloudTex,uv+float2(_AmbientTime*0.0008+0.002,0.0015)).r;
                    lighting*=1-smoothstep(0.30,0.88,cloud)*0.20*day;
                }
                float3 color=albedo*lighting;
                float facing=saturate(dot(n,eye));
                float rim=pow(1-facing,4);
                float scatter=pow(saturate(ndl+0.32),1.4);
                float3 rimColor=earth?float3(0.18,0.48,1.2):lerp(_Color.rgb,float3(1,0.71,0.38),0.45);
                color+=rimColor*rim*scatter*0.12;
                if(earth)
                {
                    color+=tex2D(_NightTex,uv).rgb*(1-smoothstep(-0.03,0.15,ndl))*lerp(1.4,0.3,_Overview);
                    float ocean=tex2D(_SpecularTex,uv).r;
                    float3 halfDirection=normalize(sun+eye);
                    float spec=pow(saturate(dot(n,halfDirection)),95);
                    color+=float3(1.1,1.35,1.6)*spec*ocean*day*0.35;
                }
                return half4(color,1);
            }
            ENDCG
        }
    }
}
