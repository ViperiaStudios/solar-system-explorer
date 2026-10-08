Shader "SolarExplorer/CinematicBloom"
{
    Properties { _MainTex ("Scene",2D)="black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex,_BloomTex;
        float4 _Direction;
        half4 threshold(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb;
            half peak=max(c.r,max(c.g,c.b));
            half weight=max(peak-1.25,0)/max(peak,0.001);
            return half4(c*weight,1);
        }
        half4 blur(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb*0.227027;
            c+=(tex2D(_MainTex,i.uv+_Direction.xy*1.384615).rgb+tex2D(_MainTex,i.uv-_Direction.xy*1.384615).rgb)*0.316216;
            c+=(tex2D(_MainTex,i.uv+_Direction.xy*3.230769).rgb+tex2D(_MainTex,i.uv-_Direction.xy*3.230769).rgb)*0.070270;
            return half4(c,1);
        }
        half4 composite(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb+tex2D(_BloomTex,i.uv).rgb*0.14;
            // Filmic highlight rolloff retains solar detail without clipping the Sun white.
            // The scene remains full resolution; only the light bloom is blurred.
            c=max(c,0);
            c=saturate((c*(2.51*c+0.03))/(c*(2.43*c+0.59)+0.14));
            return half4(c,1);
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment threshold
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blur
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment composite
            ENDCG }
    }
}
