namespace Day113
{
    /// HTML版 commentColor / commentShape / commentFeel をそのまま移植
    public static class Judges
    {
        public static readonly string[] Name = { "藍原先生", "方円先生", "ふわり先生" };
        public static readonly string[] Role = { "色彩", "形", "感性" };
        static readonly System.Random rnd = new System.Random();
        static string Pick(params string[] a) => a[rnd.Next(a.Length)];

        public static string Color(GradeResult g, StageDef st)
        {
            int s = g.color; var f = g.f; string w = st.labels[f.worst], t;
            if (s >= 88) t = Pick("…見事じゃ。色が呼吸しておる。", "この色の置き方、わしは嫌いではない。");
            else if (s >= 70) t = Pick("悪くない。じゃが「" + w + "」の色がまだ遠いのう。", "筋は良い。「" + w + "」をもう一段詰めてみなさい。");
            else if (s >= 45) t = Pick("「" + w + "」の色が離れておる。混ぜる比率をよう見なさい。", "色が手探りじゃな。お手本を睨みなさい。");
            else t = Pick("…まだ絵の具が出ただけじゃな。", "色が迷子じゃ。3原色の性格を知りなさい。");
            if (f.grad >= 0)
            {
                if (f.grad < .55) t = Pick("空が段々になっておる。オレンジと黄のあいだに、両方を混ぜた色をはさみなされ。", "空の色が上から下へ素直に移っておらん。紺→紫→オレンジ→黄じゃ。");
                else if (f.grad < .75 && s < 88) t += " 空のつなぎ目に、もうひと色はさむと良い。";
                else if (s >= 70) t += " 空のぼかし、なめらかじゃ。";
            }
            if (f.blackShare > .08 && !st.blackOK) t += " 黒に頼るでない。" + st.colorTip;
            else if (st.blackOK && s < 70) t += " " + st.colorTip;
            return t;
        }

        public static string Shape(GradeResult g, StageDef st)
        {
            int s = g.shape; var f = g.f;
            if (s >= 85) return Pick(st.shapeGood);
            if (s >= 60) return st.shapeTip[f.wShape][0];
            if (s >= 30) return st.shapeTip[f.wShape][1];
            return Pick(st.shapeLost);
        }

        public static string Feel(GradeResult g, StageDef st)
        {
            var f = g.f;
            if (f.cov < .6) return "まだ白いとこ、いっぱいあるね〜。もっとドバーッて塗っちゃお？";
            if (f.avgLen < 40) return st.grad ? "ちまちま塗りだね〜。空はいちばん太い筆で、横にシューッて引いてみて！" : "ちまちま塗りだね〜。かわいいけど、腕ごとシュッて動かしてみて！";
            if (g.feel >= 80) return Pick("わあ、絵の具がもりもり！触りたくなる〜。", "筆が元気！見てるだけで楽しくなっちゃう。");
            return Pick("いい感じ〜。もうちょっと色の種類を増やしたらにぎやかになるよ！", "勢いはあるよ〜。いろんな混ぜ色、試してみて！");
        }

        public static string Verdict(int tt) => tt >= 85 ? "大行列まちがいなし！" : tt >= 65 ? "行列ができそう！" : tt >= 40 ? "ちらほら人が来そう" : "ガラガラかも…でも、ここから！";
    }
}
