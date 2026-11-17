#pragma warning disable 0649
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace Day113.EditorTools
{
    /// HTML版と同じ点数が出るかを Golden/ の正解データで確かめる（メニュー Day113/一致テスト）
    public static class GoldenCheck
    {
        [Serializable] class F { public double blackShare, mudShare, grad, cov, avgLen, tex, variety, bold, raw; public int worst, wShape; public double[] J; }
        [Serializable] class E { public int color, shape, feel, total; public F f; }
        [Serializable] class S { public double paintedPx, blackPx, mudPx, strokes, strokeLenSum, mixCount; }
        [Serializable] class C { public string name, baseRGB, h, painted; public S stats; public E expect; }
        [Serializable] class Root { public C[] cases; }
        [Serializable] class Rec { public double[] target; public int[] recipe; }
        [Serializable] class Recs { public Rec[] regions; }
        [Serializable] class RJ { public Recs recipes; }

        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Golden")) + "/";

        [MenuItem("Day113/一致テスト")]
        public static void Menu() => Debug.Log(Run());

        public static string Run()
        {
            var sb = new System.Text.StringBuilder(); int pass = 0, fail = 0; double maxF = 0;
            for (int n = 1; n <= 3; n++)
            {
                var st = Stages.All[n - 1];
                string txt;
                using (var gz = new GZipStream(File.OpenRead(Dir + $"stage{n}_grade.json.gz"), CompressionMode.Decompress))
                using (var r = new StreamReader(gz)) txt = r.ReadToEnd();
                var root = JsonUtility.FromJson<Root>(txt);
                foreach (var c in root.cases)
                {
                    var hb = Convert.FromBase64String(c.h); var h = new float[hb.Length / 4]; Buffer.BlockCopy(hb, 0, h, 0, hb.Length);
                    var s = new Stats { paintedPx = c.stats.paintedPx, blackPx = c.stats.blackPx, mudPx = c.stats.mudPx, strokes = c.stats.strokes, strokeLenSum = c.stats.strokeLenSum, mixCount = c.stats.mixCount };
                    var g = Grader.Grade(Convert.FromBase64String(c.baseRGB), h, Convert.FromBase64String(c.painted), s, st);
                    var e = c.expect; var f = g.f; var ef = e.f; double d = 0;
                    void M(double a, double b) { d = Math.Max(d, Math.Abs(a - b)); }
                    M(f.raw, ef.raw); M(f.cov, ef.cov); M(f.tex, ef.tex); M(f.bold, ef.bold); M(f.variety, ef.variety);
                    M(f.grad, ef.grad); M(f.blackShare, ef.blackShare); M(f.mudShare, ef.mudShare); M(f.avgLen, ef.avgLen);
                    for (int k = 0; k < f.J.Length; k++) M(f.J[k], ef.J[k]);
                    maxF = Math.Max(maxF, d);
                    bool ok = g.color == e.color && g.shape == e.shape && g.feel == e.feel && g.total == e.total && f.worst == ef.worst && f.wShape == ef.wShape && d < 1e-4;
                    if (ok) pass++; else { fail++; sb.AppendLine($"NG s{n} {c.name}: C# {g.color}/{g.shape}/{g.feel}/{g.total} vs HTML {e.color}/{e.shape}/{e.feel}/{e.total} d={d:E2}"); }
                    sb.AppendLine($"  s{n} {c.name,-11} 色{g.color,3} 形{g.shape,3} 感{g.feel,3} 総合{g.total,3} {(ok ? "OK" : "NG")}");
                }
                var rj = JsonUtility.FromJson<RJ>(File.ReadAllText(Dir + $"stage{n}_ref.json"));
                foreach (var rg in rj.recipes.regions)
                {
                    var b = Grader.BestRecipe(rg.target);
                    if (string.Join(",", b) == string.Join(",", rg.recipe)) pass++; else { fail++; sb.AppendLine($"NG recipe s{n} {string.Join(",", b)} vs {string.Join(",", rg.recipe)}"); }
                }
            }
            return $"[Day113 一致テスト] pass={pass} fail={fail} f最大差={maxF:E2}\n" + sb;
        }
    }
}
