Shader "SolarExplorer/EarthClouds"
{
    Properties { _MainTex ("Cloud map",2D)="black" {} _AmbientTime ("Animation clock",Float)=0 _Overview ("Overview light",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _AmbientTime,_Overview;
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.uv=v.texcoord.xy; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float cloud=tex2D(_MainTex,i.uv+float2(_AmbientTime*0.0008,0)).r;
                float3 sun=normalize(normalize(-i.world)+float3(-0.22,0.65,-0.2)*_Overview);
                float day=saturate(dot(normalize(i.normal),sun));
                return float4(float3(0.91,0.97,1)*(0.12+1.3*pow(day,0.7)),smoothstep(0.12,0.90,cloud)*0.96);
            }
            ENDCG
        }
    }
}
