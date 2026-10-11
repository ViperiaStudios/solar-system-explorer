Shader "SolarExplorer/PlanetRings"
{
    Properties { _MainTex ("Ring map",2D)="white" {} _Center ("Planet center",Vector)=(0,0,0,0) _Radius ("Planet radius",Float)=3.8 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float3 _Center; float _Radius;
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float3 world:TEXCOORD1; };
            v2f vert(appdata_base v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float r=i.uv.x;
                float edge=smoothstep(0,0.015,r)*(1-smoothstep(0.97,1,r));
                float4 map=tex2D(_MainTex,float2(r,0.5));
                float3 sun=normalize(-i.world), relative=i.world-_Center;
                float along=dot(relative,sun);
                float perpendicular=length(relative-along*sun);
                float shadow=(1-smoothstep(_Radius*0.88,_Radius*1.12,perpendicular))*step(along,0);
                float3 normal=normalize(UnityObjectToWorldNormal(float3(0,1,0)));
                float3 eye=normalize(_WorldSpaceCameraPos-i.world);
                float sunlight=0.66+0.54*abs(dot(normal,sun));
                float transmission=pow(saturate(dot(-sun,eye)),4)*(1-map.a)*0.35;
                float resolved=1-smoothstep(0.3,1.5,fwidth(r*900));
                float fineBands=1+sin(r*900)*0.025*resolved;
                float3 color=map.rgb*float3(1.40,1.32,1.18)*fineBands*(sunlight+transmission);
                return half4(color*lerp(1,0.10,shadow),map.a*edge);
            }
            ENDCG
        }
    }
}
