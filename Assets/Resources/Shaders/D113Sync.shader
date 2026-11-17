Shader "Hidden/D113/Sync"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex; float _Res;
        float hash2(float2 x) { return frac(sin(dot(x, float2(127.1, 311.7))) * 43758.5453); }
        float weave(float2 px)
        {
            float P2 = 3.3 * _Res / 512.0;
            float tx = .5 + .5 * cos(px.x * 6.2832 / P2), ty = .5 + .5 * cos(px.y * 6.2832 / P2);
            float c = fmod(floor(px.x / P2) + floor(px.y / P2), 2.0);
            return (c > .5 ? tx * .7 + ty * .3 : ty * .7 + tx * .3) * .6 + hash2(floor(px)) * .4;
        }
        ENDCG
        Pass // 0: 色を4×4の平均で256へ
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 s = 0; float t = 1.0 / _Res;
                [unroll] for (int y = 0; y < 4; y++) [unroll] for (int x = 0; x < 4; x++)
                    s += tex2Dlod(_MainTex, float4(i.uv + float2(x - 1.5, y - 1.5) * t, 0, 0)).rgb;
                return fixed4(s / 16, 1);
            }
            ENDCG
        }
        Pass // 1: 高さ（HTML版 hgt と sync の h=.3+平均×1.1）
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                float s = 0; float t = 1.0 / _Res;
                [unroll] for (int y = 0; y < 4; y++) [unroll] for (int x = 0; x < 4; x++)
                {
                    float2 uv = i.uv + float2(x - 1.5, y - 1.5) * t;
                    float th = tex2Dlod(_MainTex, float4(uv, 0, 0)).r;
                    s += weave(uv * _Res) * .16 * max(.3, 1 - th * 2.2) + th;
                }
                return float4(.3 + s / 16 * 1.1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
