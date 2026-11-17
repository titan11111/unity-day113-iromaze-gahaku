Shader "Hidden/D113/Lit"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; sampler2D _Height; float4 _MainTex_TexelSize; float _Res;
            float hash2(float2 x) { return frac(sin(dot(x, float2(127.1, 311.7))) * 43758.5453); }
            float weave(float2 px)
            {
                float P2 = 3.3 * _Res / 512.0;
                float tx = .5 + .5 * cos(px.x * 6.2832 / P2), ty = .5 + .5 * cos(px.y * 6.2832 / P2);
                float c = fmod(floor(px.x / P2) + floor(px.y / P2), 2.0);
                return (c > .5 ? tx * .7 + ty * .3 : ty * .7 + tx * .3) * .6 + hash2(floor(px)) * .4;
            }
            // HTML版 Painter.hgt：布目＋絵の具の厚み
            float hgt(float2 uv)
            {
                float t = tex2D(_Height, uv).r;
                return weave(uv * _Res) * .26 * max(.25, 1 - t * 2.2) + t;
            }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                float t = tex2D(_Height, i.uv).r;
                float2 d = _MainTex_TexelSize.xy * (2.0 * _Res / 512.0);
                float nx = (hgt(i.uv - float2(d.x, 0)) - hgt(i.uv + float2(d.x, 0))) * 1.5;
                float ny = (hgt(i.uv - float2(0, d.y)) - hgt(i.uv + float2(0, d.y))) * 1.5;
                float3 n = normalize(float3(nx, ny, 1));
                // 左上からの光（画面はY上向き）
                float3 L = float3(-.5, .6, .62);
                float3 Hv = normalize(float3(L.x, L.y, L.z + 1));
                float dif = .6 + .5 * max(0, dot(n, L));
                // 麻布の繊維むら（塗っていない所ほど見える）
                float fib = (hash2(floor(i.uv * _Res * float2(.5, 3))) + hash2(floor(i.uv * _Res * float2(3, .5)))) * .5 - .5;
                c *= 1 + fib * .06 * saturate(1 - t * 4);
                float sp = pow(max(0, dot(n, Hv)), 36.0) * min(1, t * 2) * (40.0 / 255.0);
                return fixed4(saturate(c * dif + sp), 1);
            }
            ENDCG
        }
    }
}
