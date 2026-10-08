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
                float antialias=1-saturate(fwidth(r)*65);
                float band=0.75+0.13*sin(r*100)*antialias+0.06*sin(r*230)*antialias;
                float gap=smoothstep(0.60,0.62,r)*(1-smoothstep(0.67,0.69,r));
                float edge=smoothstep(0,0.015,r)*(1-smoothstep(0.97,1,r));
                float4 map=tex2D(_MainTex,float2(r,0.5));
                float3 sun=normalize(-i.world), relative=i.world-_Center;
                float along=dot(relative,sun);
                float perpendicular=length(relative-along*sun);
                float shadow=(1-smoothstep(_Radius*0.88,_Radius*1.12,perpendicular))*step(along,0);
                return half4(map.rgb*float3(1.6,1.25,0.86)*lerp(1,0.12,shadow),map.a*edge);
            }
            ENDCG
        }
    }
}
