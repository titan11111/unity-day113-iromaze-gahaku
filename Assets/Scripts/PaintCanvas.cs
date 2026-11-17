using System.Collections.Generic;
using UnityEngine;

namespace Day113
{
    /// GPUで描く油絵キャンバス。座標は「256座標系・Y上向き」で受け取り、Res(1024)pxのRenderTextureにスタンプする。
    /// 筆の考え方はHTML版 Painter（毛の筋・絵の具が減ってかすれる・ふちが盛り上がる）をシェーダーへ移したもの。
    public class PaintCanvas : System.IDisposable
    {
        public const int Res = 1024;
        public const float K = Res / 256f;
        const int MaxUndo = 12;

        public RenderTexture Color { get; private set; }
        public RenderTexture Height { get; private set; } // R=絵の具の厚み G=ぬれ
        public RenderTexture Display { get; private set; }
        RenderTexture colorTmp, heightTmp;
        readonly Material stamp, bleed, lit;
        readonly Mesh mesh = new Mesh();
        readonly List<Vector3> vv = new List<Vector3>();
        readonly List<UnityEngine.Color> cc = new List<UnityEngine.Color>();
        readonly List<Vector2> u0 = new List<Vector2>();
        readonly List<Vector4> u1 = new List<Vector4>();
        readonly List<Vector2> u2 = new List<Vector2>();
        readonly List<int> ix = new List<int>();
        Texture2D blankTex;

        // 採点用の統計（HTML版と同じ256座標系の単位）
        public float PaintedPx, BlackPx, MudPx, StrokeLenSum;
        public int Strokes;
        public readonly HashSet<string> MixKeys = new HashSet<string>();
        public int MixCount => MixKeys.Count;
        public readonly byte[] Painted = new byte[256 * 256]; // 行0が上

        class Snap
        {
            public RenderTexture col, h;
            public float pp, bp, mp, sls; public int st;
            public byte[] painted = new byte[256 * 256];
            public string[] keys;
        }
        readonly List<Snap> undo = new List<Snap>();
        readonly Stack<Snap> pool = new Stack<Snap>();
        public int UndoCount => undo.Count;
        public float Load => active ? load : 1f;

        // 筆の状態
        bool active, black, mud, dirty = true;
        Vector3 mix; float R, load, run, len, seed, bristles;
        Vector2 pos, lastDir = Vector2.right;
        float wet, bleedAcc;

        public PaintCanvas()
        {
            var hf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGB32;
            Color = NewRT(RenderTextureFormat.ARGB32); colorTmp = NewRT(RenderTextureFormat.ARGB32);
            Height = NewRT(hf); heightTmp = NewRT(hf);
            Display = NewRT(RenderTextureFormat.ARGB32);
            stamp = new Material(Shader.Find("Hidden/D113/Stamp"));
            bleed = new Material(Shader.Find("Hidden/D113/Bleed"));
            lit = new Material(Shader.Find("Hidden/D113/Lit"));
            stamp.SetFloat("_Res", Res); lit.SetFloat("_Res", Res);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            MakeBlank();
            Clear();
        }

        static RenderTexture NewRT(RenderTextureFormat f)
        {
            var rt = new RenderTexture(Res, Res, 0, f, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, useMipMap = false };
            rt.Create();
            return rt;
        }

        void MakeBlank()
        {
            blankTex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Res * Res];
            var rnd = new System.Random(113);
            for (int i = 0; i < px.Length; i++)
            {
                int n = (int)((rnd.NextDouble() - .5) * 3);
                px[i] = new Color32((byte)(243 + n), (byte)(238 + n), (byte)(226 + n), 255);
            }
            blankTex.SetPixels32(px); blankTex.Apply(false, true);
        }

        void Clear()
        {
            vv.Clear(); cc.Clear(); u0.Clear(); u1.Clear(); u2.Clear(); ix.Clear();
            Graphics.Blit(blankTex, Color);
            var prev = RenderTexture.active;
            RenderTexture.active = Height; GL.Clear(false, true, UnityEngine.Color.clear);
            RenderTexture.active = prev;
            PaintedPx = BlackPx = MudPx = StrokeLenSum = 0; Strokes = 0; MixKeys.Clear();
            System.Array.Clear(Painted, 0, Painted.Length);
            wet = 0; dirty = true;
        }

        /// まっさら（1筆もどすで戻せる）
        public void Reset() { PushUndo(); Clear(); }

        public bool BeginStroke(int[] counts, float r256, Vector2 p, float widthMul)
        {
            if (!MixCore.MixRGB(counts, out var r, out var g, out var b)) return false;
            PushUndo();
            int tot = MixCore.Total(counts);
            mix = new Vector3((float)(r / 255), (float)(g / 255), (float)(b / 255));
            black = counts[4] / (float)tot >= .12f;
            mud = counts[0] / (float)tot >= .2f && counts[1] / (float)tot >= .2f && counts[2] / (float)tot >= .2f;
            MixKeys.Add(string.Join(",", counts));
            R = r256; load = 1; run = 0; len = 0; pos = p; active = true;
            seed = Random.value * 100f;
            bristles = Mathf.Clamp(Mathf.Round(R * 1.4f), 6, 40);
            Dab(p, lastDir, widthMul);
            return true;
        }

        /// speed01：0=ゆっくり 1=速い。速いほど細く・かすれやすい
        public void StrokeTo(Vector2 p, float widthMul, float speed01)
        {
            if (!active) return;
            var d = p - pos; float dist = d.magnitude;
            if (dist < 1e-4f) return;
            var dir = d / dist; lastDir = dir;
            float step = Mathf.Max(.5f, R * .12f);
            int n = Mathf.Max(1, Mathf.CeilToInt(dist / step));
            float sd = dist / n;
            for (int i = 1; i <= n; i++)
            {
                run += sd;
                // HTML版：load -= sd/(260+R*14)（512座標）→ 256座標に直し、速さでさらに減らす
                load = Mathf.Max(.12f, load - sd * 2f / (260f + R * 28f) * (1f + speed01 * .8f));
                Dab(pos + d * (i / (float)n), dir, widthMul);
            }
            len += dist; pos = p;
        }

        public void EndStroke()
        {
            if (!active) return;
            Strokes++; StrokeLenSum += len; active = false;
        }

        void Dab(Vector2 p, Vector2 dir, float wm)
        {
            float ramp = Mathf.Min(1f, .35f + run * 2f / (6f + R * .6f));
            float a = Mathf.Clamp01(load * ramp * 1.15f);
            if (a < .04f) return;
            float rad = R * wm;
            var c = p * K; var ax = new Vector2(-dir.y, dir.x) * rad * K; var ay = dir * rad * K;
            int b0 = vv.Count;
            vv.Add(c - ax - ay); vv.Add(c + ax - ay); vv.Add(c + ax + ay); vv.Add(c - ax + ay);
            u0.Add(new Vector2(-1, -1)); u0.Add(new Vector2(1, -1)); u0.Add(new Vector2(1, 1)); u0.Add(new Vector2(-1, 1));
            var col = new UnityEngine.Color(mix.x, mix.y, mix.z, a);
            var pr = new Vector4(.84f * (.55f + .45f * load), load, seed, bristles);
            for (int k = 0; k < 4; k++) { cc.Add(col); u1.Add(pr); u2.Add((Vector2)vv[b0 + k] / Res); }
            ix.Add(b0); ix.Add(b0 + 1); ix.Add(b0 + 2); ix.Add(b0); ix.Add(b0 + 2); ix.Add(b0 + 3);

            // 統計：HTML版は「毛1本の円×本数×0.25」を足していく。同じ量になるように見積もる
            float R512 = rad * 2f;
            float nb = Mathf.Clamp(Mathf.Round(R512 * 2.2f), 10, 110);
            float w = 1f + R512 * .045f + .55f;
            float add = .25f * nb * Mathf.PI * (w + .5f) * (w + .5f) * .85f * (load < .55f ? .6f : 1f);
            PaintedPx += add; if (black) BlackPx += add; if (mud) MudPx += add;
            MarkPainted(p, rad * .9f);
            wet = 1.6f;
        }

        void MarkPainted(Vector2 p, float r)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(p.x - r)), x1 = Mathf.Min(255, Mathf.CeilToInt(p.x + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(p.y - r)), y1 = Mathf.Min(255, Mathf.CeilToInt(p.y + r));
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
            {
                float dy = y + .5f - p.y; int row = (255 - y) * 256;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + .5f - p.x;
                    if (dx * dx + dy * dy <= r2) Painted[row + x] = 1;
                }
            }
        }

        void Flush()
        {
            if (vv.Count == 0) return;
            mesh.Clear(false);
            mesh.SetVertices(vv); mesh.SetColors(cc); mesh.SetUVs(0, u0); mesh.SetUVs(1, u1); mesh.SetUVs(2, u2);
            mesh.SetTriangles(ix, 0, false);
            var prev = RenderTexture.active;
            GL.PushMatrix();
            RenderTexture.active = Color; GL.LoadPixelMatrix(0, Res, 0, Res);
            stamp.SetPass(0); Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            RenderTexture.active = Height; GL.LoadPixelMatrix(0, Res, 0, Res);
            stamp.SetPass(1); Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            GL.PopMatrix();
            RenderTexture.active = prev;
            vv.Clear(); cc.Clear(); u0.Clear(); u1.Clear(); u2.Clear(); ix.Clear();
            dirty = true;
        }

        /// 毎フレーム呼ぶ：ぬれた絵の具のにじみ（30Hz固定）と、表示用の光の計算
        public void Tick(float dt)
        {
            Flush();
            if (wet > 0)
            {
                bleedAcc += dt;
                while (bleedAcc >= 1f / 30f && wet > 0)
                {
                    bleedAcc -= 1f / 30f; wet -= 1f / 30f;
                    bleed.SetTexture("_Height", Height); bleed.SetFloat("_K", .09f);
                    Graphics.Blit(Color, colorTmp, bleed, 0); (Color, colorTmp) = (colorTmp, Color);
                    bleed.SetFloat("_Decay", .022f);
                    Graphics.Blit(Height, heightTmp, bleed, 1); (Height, heightTmp) = (heightTmp, Height);
                    dirty = true;
                }
            }
            else bleedAcc = 0;
            if (!dirty) return;
            lit.SetTexture("_Height", Height);
            Graphics.Blit(Color, Display, lit);
            dirty = false;
        }

        void PushUndo()
        {
            Flush();
            Snap s;
            if (undo.Count >= MaxUndo) { s = undo[0]; undo.RemoveAt(0); }
            else if (pool.Count > 0) s = pool.Pop();
            else s = new Snap { col = NewRT(RenderTextureFormat.ARGB32), h = NewRT(Height.format) };
            Graphics.Blit(Color, s.col); Graphics.Blit(Height, s.h);
            s.pp = PaintedPx; s.bp = BlackPx; s.mp = MudPx; s.sls = StrokeLenSum; s.st = Strokes;
            System.Buffer.BlockCopy(Painted, 0, s.painted, 0, Painted.Length);
            s.keys = new string[MixKeys.Count]; MixKeys.CopyTo(s.keys);
            undo.Add(s);
        }

        public bool Undo()
        {
            if (undo.Count == 0) return false;
            vv.Clear(); cc.Clear(); u0.Clear(); u1.Clear(); u2.Clear(); ix.Clear();
            var s = undo[undo.Count - 1]; undo.RemoveAt(undo.Count - 1);
            Graphics.Blit(s.col, Color); Graphics.Blit(s.h, Height);
            PaintedPx = s.pp; BlackPx = s.bp; MudPx = s.mp; StrokeLenSum = s.sls; Strokes = s.st;
            System.Buffer.BlockCopy(s.painted, 0, Painted, 0, Painted.Length);
            MixKeys.Clear(); foreach (var k in s.keys) MixKeys.Add(k);
            pool.Push(s); active = false; wet = 0; dirty = true;
            return true;
        }

        // ---------- 採点用：今の絵を256×256に縮めて読み出す（行0が上）----------
        Material syncMat; RenderTexture sc, sh; Texture2D tc, th;
        public void Sync(byte[] rgb, float[] h)
        {
            Flush();
            if (syncMat == null)
            {
                syncMat = new Material(Shader.Find("Hidden/D113/Sync")); syncMat.SetFloat("_Res", Res);
                bool rf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);
                sc = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Point }; sc.Create();
                sh = new RenderTexture(256, 256, 0, rf ? RenderTextureFormat.RFloat : RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Point }; sh.Create();
                tc = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
                th = new Texture2D(256, 256, rf ? TextureFormat.RFloat : TextureFormat.RGBAFloat, false, true);
            }
            Graphics.Blit(Color, sc, syncMat, 0);
            Graphics.Blit(Height, sh, syncMat, 1);
            var prev = RenderTexture.active;
            RenderTexture.active = sc; tc.ReadPixels(new Rect(0, 0, 256, 256), 0, 0, false); tc.Apply(false);
            RenderTexture.active = sh; th.ReadPixels(new Rect(0, 0, 256, 256), 0, 0, false); th.Apply(false);
            RenderTexture.active = prev;
            var px = tc.GetPixels32(); var hp = th.GetPixels();
            for (int y = 0; y < 256; y++)
            {
                int src = (255 - y) * 256, dst = y * 256;
                for (int x = 0; x < 256; x++) { var c = px[src + x]; int j = (dst + x) * 3; rgb[j] = c.r; rgb[j + 1] = c.g; rgb[j + 2] = c.b; h[dst + x] = hp[src + x].r; }
            }
        }

        public Stats GetStats() => new Stats { paintedPx = PaintedPx, blackPx = BlackPx, mudPx = MudPx, strokes = Strokes, strokeLenSum = StrokeLenSum, mixCount = MixCount };

        /// ステージを変えるとき：もどす履歴も消して白紙に
        public void ResetHard()
        {
            Flush();
            foreach (var s in undo) pool.Push(s);
            undo.Clear(); active = false; Clear();
        }

        /// 額に入れる用：光なしの色だけの絵
        public Texture2D Snapshot()
        {
            Flush();
            var t = new Texture2D(Res, Res, TextureFormat.RGB24, false);
            var prev = RenderTexture.active; RenderTexture.active = Display;
            t.ReadPixels(new Rect(0, 0, Res, Res), 0, 0, false); t.Apply(false);
            RenderTexture.active = prev; return t;
        }

        public void Dispose()
        {
            foreach (var rt in new[] { Color, colorTmp, Height, heightTmp, Display }) if (rt) { rt.Release(); Object.Destroy(rt); }
            foreach (var s in undo) { s.col.Release(); s.h.Release(); }
            foreach (var s in pool) { s.col.Release(); s.h.Release(); }
            if (sc) { sc.Release(); sh.Release(); Object.Destroy(tc); Object.Destroy(th); Object.Destroy(syncMat); }
            Object.Destroy(stamp); Object.Destroy(bleed); Object.Destroy(lit); Object.Destroy(mesh); Object.Destroy(blankTex);
        }
    }
}
