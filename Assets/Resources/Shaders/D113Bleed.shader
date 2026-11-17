Shader "Hidden/D113/Bleed"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex; sampler2D _Height; float4 _MainTex_TexelSize; float _K; float _Decay;
        ENDCG
        Pass // 0: ぬれている所どうしの色をにじませる
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 h = tex2D(_Height, i.uv).rg;
                if (h.g < .01 || h.r < .01) return c;
                float2 t = _MainTex_TexelSize.xy * 1.5;
                float3 sum = c.rgb; float ws = 1;
                float2 o[4] = { float2(t.x, 0), float2(-t.x, 0), float2(0, t.y), float2(0, -t.y) };
                for (int k = 0; k < 4; k++)
                {
                    float2 hn = tex2D(_Height, i.uv + o[k]).rg;
                    float w = step(.01, hn.r) * hn.g;
                    sum += tex2D(_MainTex, i.uv + o[k]).rgb * w; ws += w;
                }
                return fixed4(lerp(c.rgb, sum / ws, saturate(h.g * _K)), c.a);
            }
            ENDCG
        }
        Pass // 1: ぬれをかわかす
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                float4 h = tex2D(_MainTex, i.uv);
                return float4(h.r, max(0, h.g - _Decay), 0, 1);
            }
            ENDCG
        }
    }
}
