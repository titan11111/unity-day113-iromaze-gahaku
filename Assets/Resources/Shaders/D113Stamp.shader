Shader "Hidden/D113/Stamp"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 p : TEXCOORD1; float2 cuv : TEXCOORD2; };
        struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 p : TEXCOORD1; float2 cuv : TEXCOORD2; };
        float _Res;
        v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; o.p = v.p; o.cuv = v.cuv; return o; }
        float hash1(float x) { return frac(sin(x * 127.1) * 43758.5453); }
        float hash2(float2 x) { return frac(sin(dot(x, float2(127.1, 311.7))) * 43758.5453); }
        // キャンバスの布目（HTML版 weave と同じ形。周期は解像度に合わせて倍）
        float weave(float2 px)
        {
            float P2 = 3.3 * _Res / 512.0;
            float tx = .5 + .5 * cos(px.x * 6.2832 / P2), ty = .5 + .5 * cos(px.y * 6.2832 / P2);
            float c = fmod(floor(px.x / P2) + floor(px.y / P2), 2.0);
            return (c > .5 ? tx * .7 + ty * .3 : ty * .7 + tx * .3) * .6 + hash2(floor(px)) * .4;
        }
        // p: x=厚みの目標 y=絵の具の残り z=筆ごとの種 w=毛の本数
        float dabAlpha(v2f i, out float edge)
        {
            float d = length(i.uv);
            clip(1 - d);
            edge = d;
            float s = (i.uv.x * .5 + .5) * i.p.w;
            float cell = floor(s), f = frac(s);
            float ld = .45 + .7 * hash1(cell + i.p.z * 17.3);
            float prof = 1 - .55 * pow(abs(f * 2 - 1), 2.0);
            float a = i.color.a * (1 - d * d) * ld * prof;
            float load = i.p.y;
            if (load < .55)
            {
                float thr = (.55 - load) * 2.4;
                float wv = weave(i.cuv * _Res) * .8 + hash1(cell + i.p.z * 3.1) * .3;
                a *= smoothstep(thr - .1, thr + .1, wv);
            }
            return saturate(a);
        }
        ENDCG

        Pass // 0: 色
        {
            Blend SrcAlpha OneMinusSrcAlpha, One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                float e; float a = dabAlpha(i, e);
                float s = (i.uv.x * .5 + .5) * i.p.w;
                float cj = (hash1(floor(s) + i.p.z * 41.7) - .5) * .12;
                return fixed4(saturate(i.color.rgb * (1 + cj)), a);
            }
            ENDCG
        }
        Pass // 1: 厚み(R)とぬれ(G)
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                float e; float a = dabAlpha(i, e);
                float s = (i.uv.x * .5 + .5) * i.p.w;
                float hb = .55 + .6 * hash1(floor(s) + i.p.z * 9.7) + (e > .78 ? .3 : 0);
                return float4(i.p.x * hb, 1, 0, a * .5);
            }
            ENDCG
        }
    }
}
