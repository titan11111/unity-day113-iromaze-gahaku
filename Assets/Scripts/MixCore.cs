using System;

namespace Day113
{
    /// HTML版 core の混色をそのまま移植（重み付き幾何平均）。JSのnumberと同じdoubleで計算する。
    public static class MixCore
    {
        // 0赤 1黄 2青 3白 4黒
        public static readonly double[][] PIG =
        {
            new[] { .85, .10, .09 }, new[] { .96, .80, .08 }, new[] { .10, .22, .60 },
            new[] { .97, .96, .93 }, new[] { .07, .07, .08 }
        };
        public const int MaxDrops = 12;

        /// 配合 c[5] から RGB(0〜255) を出す。全部0なら false。
        public static bool MixRGB(int[] c, out double r, out double g, out double b)
        {
            r = g = b = 0;
            double t = 0;
            for (int i = 0; i < 5; i++) t += c[i];
            if (t == 0) return false;
            double lr = 0, lg = 0, lb = 0;
            for (int i = 0; i < 5; i++)
            {
                double w = c[i] / t;
                if (w == 0) continue;
                lr += w * Math.Log(PIG[i][0]); lg += w * Math.Log(PIG[i][1]); lb += w * Math.Log(PIG[i][2]);
            }
            r = Math.Exp(lr) * 255; g = Math.Exp(lg) * 255; b = Math.Exp(lb) * 255;
            return true;
        }

        public static int Total(int[] c) { int t = 0; for (int i = 0; i < 5; i++) t += c[i]; return t; }
    }
}
