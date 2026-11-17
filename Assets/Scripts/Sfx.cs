using UnityEngine;

namespace Day113
{
    /// 効果音：外部素材を使わず、起動時にPCMで合成する（WebGLでも動くようAudioClip.Createのデータ方式）。
    public class Sfx : MonoBehaviour
    {
        const int SR = 44100;
        AudioSource brush, one;
        AudioClip squish, plop, tap, wash, chime, low, bon; AudioClip[] voice;
        float brushTarget, brushVol, brushPitch = 1;

        void Awake()
        {
            brush = gameObject.AddComponent<AudioSource>(); brush.loop = true; brush.volume = 0; brush.playOnAwake = false;
            one = gameObject.AddComponent<AudioSource>(); one.playOnAwake = false;
            brush.clip = MakeBrush(); brush.Play();
            squish = MakeSquish(); plop = MakePlop(); tap = MakeTap(); wash = MakeWash();
            chime = MakeNotes("chime", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, .11f); low = MakeNotes("low", new[] { 330f, 262f }, .09f);
            bon = MakeBon();
            // 先生の声「ポ」：藍原（低い）・方円（中）・ふわり（高い）
            voice = new[] { MakePo("po0", 150f), MakePo("po1", 245f), MakePo("po2", 360f) };
        }

        public void Squish() { one.PlayOneShot(squish, .55f); }
        public void Plop() { one.PlayOneShot(plop, .45f); }
        public void Tap() { one.PlayOneShot(tap, .35f); }
        public void Wash() { one.PlayOneShot(wash, .5f); }
        public void Chime() { one.PlayOneShot(chime, .5f); }
        public void Locked() { one.PlayOneShot(low, .45f); }
        /// しゃべり声「ポポポ」：1文字ぶん。高さを少し揺らして単調にしない
        public void Voice(int who)
        {
            if (voice == null || who < 0 || who >= voice.Length) return;
            one.PlayOneShot(voice[who], .42f + Random.value * .08f);
        }
        /// 札を上げる「ボン」
        public void Bon() { one.PlayOneShot(bon, .7f); }

        /// 筆の速さ（0〜1）。0で無音。絵の具が減るとかすれた高い音に寄る
        public void Brush(float speed01, float load)
        {
            brushTarget = speed01 <= 0 ? 0 : Mathf.Lerp(.08f, .55f, speed01);
            brushPitch = Mathf.Lerp(.8f, 1.35f, speed01) + (1 - load) * .25f;
        }

        void Update()
        {
            brushVol = Mathf.MoveTowards(brushVol, brushTarget, Time.deltaTime * (brushTarget > brushVol ? 6f : 3f));
            brush.volume = brushVol;
            brush.pitch = Mathf.Lerp(brush.pitch, brushPitch, Time.deltaTime * 10f);
            brushTarget *= Mathf.Pow(.02f, Time.deltaTime); // 動きが止まると自然に消える
        }

        // ---- 合成 ----
        static AudioClip Clip(string n, float[] d) { var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); return c; }

        static AudioClip MakeBrush()
        {
            // 毛が布目をこするザラザラ：帯域ノイズ＋細かい粒（毛先の引っかかり）。2秒、つなぎ目はクロスフェード
            int n = SR * 2, fade = SR / 10; var d = new float[n + fade]; var r = new System.Random(7);
            float lp = 0, lp2 = 0, grain = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float w = (float)(r.NextDouble() * 2 - 1);
                lp += (w - lp) * .35f; lp2 += (lp - lp2) * .08f;
                float band = lp - lp2;
                if (r.NextDouble() < .004) grain = 1f;
                grain *= .985f;
                d[i] = band * (.55f + .45f * Mathf.Sin(i * .0021f) * Mathf.Sin(i * .00037f)) * (1 + grain * 1.6f);
            }
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = d[i] * .9f;
            for (int i = 0; i < fade; i++) { float t = i / (float)fade; o[i] = d[i] * t * .9f + d[n + i] * (1 - t) * .9f; }
            return Clip("brush", o);
        }

        static AudioClip MakeSquish()
        {
            // チューブを絞る「むにゅ」：下がる低音＋湿ったノイズ
            int n = (int)(SR * .22f); var d = new float[n]; var r = new System.Random(3); float lp = 0, ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, f = Mathf.Lerp(260, 120, t);
                ph += f / SR; lp += ((float)(r.NextDouble() * 2 - 1) - lp) * .12f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1, t * 3)) * (1 - t);
                d[i] = (Mathf.Sin(ph * 6.2832f) * .6f + lp * 1.2f) * env * .7f;
            }
            return Clip("squish", d);
        }

        static AudioClip MakePlop()
        {
            // パレットに落ちる「ぽとっ」：上がってすぐ消える丸い音
            int n = (int)(SR * .16f); var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, f = Mathf.Lerp(220, 640, Mathf.Sqrt(t));
                ph += f / SR; d[i] = Mathf.Sin(ph * 6.2832f) * Mathf.Exp(-t * 9f) * Mathf.Min(1, i / 60f) * .8f;
            }
            return Clip("plop", d);
        }

        static AudioClip MakeTap()
        {
            int n = (int)(SR * .06f); var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n; ph += 1100f / SR; d[i] = Mathf.Sin(ph * 6.2832f) * Mathf.Exp(-t * 14f) * .5f; }
            return Clip("tap", d);
        }

        static AudioClip MakeNotes(string name, float[] f, float step)
        {
            int n = (int)(SR * (step * f.Length + .5f)); var d = new float[n];
            for (int k = 0; k < f.Length; k++)
            {
                int s0 = (int)(SR * step * k);
                for (int i = s0; i < n; i++)
                {
                    float t = (i - s0) / (float)SR; float e = Mathf.Exp(-t * 5f) * Mathf.Min(1, t * 300);
                    d[i] += (Mathf.Sin(6.2832f * f[k] * t) + .3f * Mathf.Sin(6.2832f * f[k] * 2 * t)) * e * .28f;
                }
            }
            return Clip(name, d);
        }

        static AudioClip MakePo(string name, float f0)
        {
            // 口を開けて閉じる短い母音っぽい音（矩形寄りの波＋素早い減衰）。70ミリ秒
            int n = (int)(SR * .07f); var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, f = f0 * (1.08f - .12f * t); ph += f / SR;
                float w = Mathf.Sin(ph * 6.2832f) + .35f * Mathf.Sin(ph * 2 * 6.2832f) + .18f * Mathf.Sin(ph * 3 * 6.2832f);
                float env = Mathf.Min(1, i / 120f) * Mathf.Pow(1 - t, 1.6f);
                d[i] = Mathf.Clamp(w * 1.2f, -1, 1) * env * .5f;
            }
            return Clip(name, d);
        }

        static AudioClip MakeBon()
        {
            // 「ボン」：低い胴鳴り＋札の紙が鳴る短いノイズ
            int n = (int)(SR * .35f); var d = new float[n]; var r = new System.Random(5); float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, f = Mathf.Lerp(150, 70, Mathf.Sqrt(t)); ph += f / SR;
                float body = Mathf.Sin(ph * 6.2832f) * Mathf.Exp(-t * 7f);
                float paper = (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 40f) * .45f;
                d[i] = (body + paper) * Mathf.Min(1, i / 40f) * .9f;
            }
            return Clip("bon", d);
        }

        static AudioClip MakeWash()
        {
            // 筆洗いの「ちゃぷちゃぷ」：こもったノイズ＋小さな泡
            int n = (int)(SR * .7f); var d = new float[n]; var r = new System.Random(11); float lp = 0, ph = 0, bf = 0, be = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                lp += ((float)(r.NextDouble() * 2 - 1) - lp) * .05f;
                if (r.NextDouble() < .0009) { bf = 500 + (float)r.NextDouble() * 700; be = 1; }
                be *= .9993f; bf *= 1.00004f; ph += bf / SR;
                d[i] = (lp * 2.2f * Mathf.Sin(Mathf.PI * t) + Mathf.Sin(ph * 6.2832f) * be * .35f) * (1 - t * .6f);
            }
            return Clip("wash", d);
        }
    }
}
