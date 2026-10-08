Shader "SolarExplorer/AsteroidSurface"
{
    Properties { _AmbientTime ("Animation clock",Float)=0 _Opacity ("Visibility",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _AmbientTime,_Opacity;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; float4 center:TEXCOORD0; float4 spin:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float3 rock:TEXCOORD2; float4 color:COLOR; };
            float3 rotateAxis(float3 p,float3 axis,float angle)
            {
                float s=sin(angle),c=cos(angle);
                return p*c+cross(axis,p)*s+axis*dot(axis,p)*(1-c);
            }
            v2f vert(appdata v)
            {
                v2f o; o.rock=v.vertex.xyz-v.center.xyz;
                float3 p=rotateAxis(o.rock,v.spin.xyz,_AmbientTime*v.spin.w);
                float a=_AmbientTime*v.center.w,s=sin(a),c=cos(a);
                float2 ellipse=float2(v.center.x,v.center.z/0.55);
                p+=float3(ellipse.x*c-ellipse.y*s,v.center.y,(ellipse.x*s+ellipse.y*c)*0.55);
                o.pos=UnityObjectToClipPos(float4(p,1)); o.world=mul(unity_ObjectToWorld,float4(p,1)).xyz;
                float3 n=rotateAxis(v.normal,v.spin.xyz,_AmbientTime*v.spin.w);
                o.normal=UnityObjectToWorldNormal(n); o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(_Opacity-0.001);
                float day=saturate(dot(normalize(i.normal),normalize(-i.world)));
                float grain=frac(sin(dot(floor(i.rock*13),float3(12.9898,78.233,37.719)))*43758.5453);
                float3 col=i.color.rgb*(0.018+day*0.75)*float3(0.65,0.49,0.32)*(0.70+grain*0.5);
                return float4(col,_Opacity*i.color.a);
            }
            ENDCG
        }
    }
}
