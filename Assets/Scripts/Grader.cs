using System;
using System.Collections.Generic;
using UnityEngine;

namespace Day113
{
    public class Stats { public double paintedPx, blackPx, mudPx, strokes, strokeLenSum, mixCount; }
    public class GradeF { public double blackShare, mudShare, grad, cov, avgLen, tex, variety, bold, raw; public int worst, wShape; public double[] J; }
    public class GradeResult { public int color, shape, feel, total; public GradeF f; }

    public class RefData
    {
        public int K; public byte[] rgb, cls, cellCls; public double[] cellRGB;
        public double[][] meanRGB, meanLab, shapeLab, cellLab, rowLab; public double[][][] rowShape;
    }

    /// HTML版 core の getRef / bestRecipe / grade をそのまま移植。JSのnumberと同じdoubleで計算する。
    public static class Grader
    {
        public const int W = 256, H = 256, G = 16, CS = 16;
        [Serializable] class RefJson { public int id; public int K; public string rgb, cls, cellCls; }
        static readonly Dictionary<int, RefData> refs = new Dictionary<int, RefData>();

        static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
        public static int JsRound(double x) => (int)Math.Floor(x + .5);

        public static double[] Rgb2Lab(double r, double g, double b)
        {
            r /= 255; g /= 255; b /= 255;
            r = r > .04045 ? Math.Pow((r + .055) / 1.055, 2.4) : r / 12.92;
            g = g > .04045 ? Math.Pow((g + .055) / 1.055, 2.4) : g / 12.92;
            b = b > .04045 ? Math.Pow((b + .055) / 1.055, 2.4) : b / 12.92;
            double x = (r * .4124 + g * .3576 + b * .1805) / .95047, y = (r * .2126 + g * .7152 + b * .0722), z = (r * .0193 + g * .1192 + b * .9505) / 1.08883;
            double F(double t) => t > .008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116;
            x = F(x); y = F(y); z = F(z);
            return new[] { 116 * y - 16, 500 * (x - y), 200 * (y - z) };
        }
        public static double DE(double[] a, double[] b) { double d0 = a[0] - b[0], d1 = a[1] - b[1], d2 = a[2] - b[2]; return Math.Sqrt(d0 * d0 + d1 * d1 + d2 * d2); }

        public static double[] CellAvg(byte[] rgb)
        {
            var o = new double[G * G * 3];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                int c = (y / CS) * G + (x / CS), i = (y * W + x) * 3;
                o[c * 3] += rgb[i]; o[c * 3 + 1] += rgb[i + 1]; o[c * 3 + 2] += rgb[i + 2];
            }
            for (int k = 0; k < o.Length; k++) o[k] /= CS * CS;
            return o;
        }
        static double[][] CellsToLab(double[] c)
        {
            var o = new double[G * G][];
            for (int i = 0; i < G * G; i++) o[i] = Rgb2Lab(c[i * 3], c[i * 3 + 1], c[i * 3 + 2]);
            return o;
        }
        static int Nearest(double[] lab, double[][] means)
        {
            int best = 0; double bd = 1e9;
            for (int k = 0; k < means.Length; k++) { double d = DE(lab, means[k]); if (d < bd) { bd = d; best = k; } }
            return best;
        }
        static double[][] RowLab(double[] cellRGB, int rows)
        {
            var o = new double[rows][];
            for (int r = 0; r < rows; r++)
            {
                double a0 = 0, a1 = 0, a2 = 0;
                for (int x = 0; x < G; x++) { int c = (r * G + x) * 3; a0 += cellRGB[c]; a1 += cellRGB[c + 1]; a2 += cellRGB[c + 2]; }
                o[r] = Rgb2Lab(a0 / G, a1 / G, a2 / G);
            }
            return o;
        }

        public static RefData GetRef(StageDef st, string json = null)
        {
            if (json == null && refs.TryGetValue(st.id, out var cached)) return cached;
            json ??= Resources.Load<TextAsset>("Refs/stage" + st.id).text;
            var j = JsonUtility.FromJson<RefJson>(json);
            var R = new RefData { K = st.labels.Length, rgb = Convert.FromBase64String(j.rgb), cls = Convert.FromBase64String(j.cls), cellCls = Convert.FromBase64String(j.cellCls) };
            int K = R.K;
            var sum = new double[K, 3]; var cnt = new double[K];
            for (int i = 0; i < W * H; i++) { int k = R.cls[i]; sum[k, 0] += R.rgb[i * 3]; sum[k, 1] += R.rgb[i * 3 + 1]; sum[k, 2] += R.rgb[i * 3 + 2]; cnt[k]++; }
            R.meanRGB = new double[K][]; R.meanLab = new double[K][];
            for (int k = 0; k < K; k++) { R.meanRGB[k] = new[] { sum[k, 0] / cnt[k], sum[k, 1] / cnt[k], sum[k, 2] / cnt[k] }; R.meanLab[k] = Rgb2Lab(R.meanRGB[k][0], R.meanRGB[k][1], R.meanRGB[k][2]); }
            R.cellRGB = CellAvg(R.rgb); R.cellLab = CellsToLab(R.cellRGB);
            R.shapeLab = R.meanLab;
            if (st.cellVote)
            {
                R.shapeLab = new double[K][];
                for (int k = 0; k < K; k++)
                {
                    double a0 = 0, a1 = 0, a2 = 0; int n = 0;
                    for (int c = 0; c < G * G; c++) if (R.cellCls[c] == k) { a0 += R.cellRGB[c * 3]; a1 += R.cellRGB[c * 3 + 1]; a2 += R.cellRGB[c * 3 + 2]; n++; }
                    R.shapeLab[k] = n > 0 ? Rgb2Lab(a0 / n, a1 / n, a2 / n) : R.meanLab[k];
                }
                R.rowShape = new double[G][][];
                for (int ry = 0; ry < G; ry++)
                {
                    R.rowShape[ry] = new double[K][];
                    for (int k = 0; k < K; k++)
                    {
                        double b0 = 0, b1 = 0, b2 = 0; int m = 0;
                        for (int rx = 0; rx < G; rx++) { int c = ry * G + rx; if (R.cellCls[c] == k) { b0 += R.cellRGB[c * 3]; b1 += R.cellRGB[c * 3 + 1]; b2 += R.cellRGB[c * 3 + 2]; m++; } }
                        R.rowShape[ry][k] = m > 0 ? Rgb2Lab(b0 / m, b1 / m, b2 / m) : new double[] { 999, 999, 999 };
                    }
                }
            }
            if (st.grad) R.rowLab = RowLab(R.cellRGB, st.gradRows);
            refs[st.id] = R;
            return R;
        }

        /// ヒント用：目標色に近い配合（赤・黄・青・白）を総当り
        public static int[] BestRecipe(double[] target)
        {
            var tl = Rgb2Lab(target[0], target[1], target[2]); int[] best = null; double bd = 1e9; var c = new int[5];
            for (int r = 0; r <= 6; r++) for (int y = 0; y <= 6; y++) for (int b = 0; b <= 6; b++) for (int w = 0; w <= 6; w++)
            {
                int n = r + y + b + w; if (n < 1 || n > 10) continue;
                c[0] = r; c[1] = y; c[2] = b; c[3] = w; c[4] = 0;
                MixCore.MixRGB(c, out var cr, out var cg, out var cb);
                double d = DE(Rgb2Lab(cr, cg, cb), tl) + n * .15;
                if (d < bd) { bd = d; best = new[] { r, y, b, w }; }
            }
            return best;
        }

        static (double h, double s, double v) RgbToHue(double r, double g, double b)
        {
            r /= 255; g /= 255; b /= 255;
            double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)), d = mx - mn, hh;
            if (d == 0) return (0, 0, mx);
            if (mx == r) hh = ((g - b) / d) % 6; else if (mx == g) hh = (b - r) / d + 2; else hh = (r - g) / d + 4;
            hh *= 60; if (hh < 0) hh += 360;
            return (hh, d / mx, mx);
        }

        /// baseRGB: 256×256×3（行0が上） h: 高さ painted: 0/1
        public static GradeResult Grade(byte[] baseRGB, float[] hmap, byte[] painted, Stats stats, StageDef st)
        {
            var R = GetRef(st); int K = R.K, N = G * G;
            var prgb = CellAvg(baseRGB); var pl = CellsToLab(prgb);
            // 色彩
            double sum = 0; var cd = new double[K]; var cn = new double[K];
            for (int c = 0; c < N; c++)
            {
                double d = DE(pl[c], R.cellLab[c]); sum += Clamp(1 - d / 50, 0, 1);
                cd[R.cellCls[c]] += d; cn[R.cellCls[c]]++;
            }
            double raw = sum / N, tot = Math.Max(1, stats.paintedPx);
            double blackShare = stats.blackPx / tot, mudShare = stats.mudPx / tot;
            double color = 100 * Clamp((raw - .40) / .52, 0, 1);
            double gradS = -1;
            if (st.grad)
            {
                var pr = RowLab(prgb, st.gradRows); double err = 0, jump = 0;
                for (int r = 0; r < st.gradRows - 1; r++)
                {
                    double dp = DE(pr[r], pr[r + 1]), dr = DE(R.rowLab[r], R.rowLab[r + 1]);
                    err += Math.Abs(dp - dr); jump = Math.Max(jump, dp - dr);
                }
                double ev = 0;
                for (int r = 0; r < st.gradRows; r++) for (int x = 0; x < G; x++) ev += Math.Min(30, DE(pl[r * G + x], pr[r]));
                ev /= st.gradRows * G;
                double match = 0; for (int r = 0; r < st.gradRows; r++) match += Clamp(1 - DE(pr[r], R.rowLab[r]) / 40, 0, 1);
                match /= st.gradRows;
                gradS = Clamp(1 - err / (st.gradRows - 1) / 16, 0, 1) * .4 + Clamp(1 - Math.Max(0, jump - 6) / 30, 0, 1) * .2 + Clamp(1 - (ev - 4) / 18, 0, 1) * .15 + match * .25;
                color = color * .7 + 100 * gradS * .3;
            }
            if (!st.blackOK) color -= Math.Min(18, blackShare * 60);
            color = Clamp(color, 0, 100);
            int worst = 0; double wv = -1;
            for (int k = 0; k < K; k++) { double av = cn[k] > 0 ? cd[k] / cn[k] : 0; if (av > wv) { wv = av; worst = k; } }
            // 形
            var inter = new double[K]; var uni = new double[K];
            for (int c = 0; c < N; c++)
            {
                int pc = Nearest(pl[c], R.rowShape != null ? R.rowShape[c / G] : R.shapeLab), tc = R.cellCls[c];
                if (pc == tc) inter[tc]++;
                uni[pc]++; if (tc != pc) uni[tc]++;
            }
            var J = new double[K]; double jsum = 0; int wShape = st.shapeCls[0];
            for (int k = 0; k < K; k++) J[k] = uni[k] > 0 ? inter[k] / uni[k] : 0;
            foreach (var q in st.shapeCls) { jsum += J[q]; if (J[q] < J[wShape]) wShape = q; }
            double shape = 100 * Clamp((jsum / st.shapeCls.Length - .05) / .75, 0, 1);
            // 感性
            double pcount = 0, hs = 0, hs2 = 0, blankKeep = 0;
            for (int i = 0; i < W * H; i++) if (painted[i] != 0) { pcount++; hs += hmap[i]; hs2 += (double)hmap[i] * hmap[i]; }
            if (st.blankOK) for (int i = 0; i < W * H; i++) if (painted[i] == 0 && R.cls[i] == 0) blankKeep++;
            double cov = (pcount + blankKeep) / (W * H), mean = pcount > 0 ? hs / pcount : 0, std = pcount > 0 ? Math.Sqrt(Math.Max(0, hs2 / pcount - mean * mean)) : 0;
            double avgLen = stats.strokeLenSum / Math.Max(1, stats.strokes);
            double tex = Clamp((std - .05) / .12, 0, 1), bold = Clamp((avgLen - 30) / 90, 0, 1);
            var bins = new int[12];
            for (int c = 0; c < N; c++)
            {
                var hv = RgbToHue(prgb[c * 3], prgb[c * 3 + 1], prgb[c * 3 + 2]);
                if (hv.s > .25 && hv.v > .2) bins[(int)Math.Floor(hv.h / 30) % 12]++;
            }
            int nb = 0; for (int i = 0; i < 12; i++) if (bins[i] >= 2) nb++;
            double variety = .5 * Clamp(nb / 6.0, 0, 1) + .5 * Clamp(stats.mixCount / 8, 0, 1);
            double feel = 100 * (.30 * Clamp((cov - .5) / .45, 0, 1) + .25 * tex + .25 * bold + .20 * variety);
            return new GradeResult
            {
                color = JsRound(color), shape = JsRound(shape), feel = JsRound(feel), total = JsRound((color + shape + feel) / 3),
                f = new GradeF { blackShare = blackShare, mudShare = mudShare, worst = worst, J = J, wShape = wShape, grad = gradS, cov = cov, avgLen = avgLen, tex = tex, variety = variety, bold = bold, raw = raw }
            };
        }
    }
}
