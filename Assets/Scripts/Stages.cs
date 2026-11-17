using System.Collections.Generic;

namespace Day113
{
    /// HTML版 STAGES の文言・設定をそのまま移したもの
    public class StageDef
    {
        public int id; public string name, sub, refNote, hintNote, colorTip;
        public string[] labels; public int[] shapeCls;
        public bool grad, cellVote, blankOK, blackOK; public int gradRows;
        public Dictionary<int, int[]> hintColor = new Dictionary<int, int[]>();
        public (string name, int[] rgb)[] hintExtra = new (string, int[])[0];
        public string[] shapeGood, shapeLost; public Dictionary<int, string[]> shapeTip;
    }

    public static class Stages
    {
        public static readonly StageDef[] All =
        {
            new StageDef
            {
                id = 1, name = "りんごとレモン", sub = "静物・光と影",
                labels = new[] { "壁", "テーブル", "りんご", "レモン" }, shapeCls = new[] { 2, 3 },
                refNote = "影と光の色の変化をよく見てね。",
                hintNote = "影は「青」を少し、光は「白」か「黄」を少し足すとそれっぽくなるよ。黒は藍原先生が嫌うので注意！",
                shapeGood = new[] { "構図がきちんと取れてるわ。丸とレモン形の関係、いいわね。", "位置も大きさも正確。見る目があるわよ。" },
                shapeLost = new[] { "まだ何が描いてあるのか分からないわ…まず場所を決めましょう。", "りんごもレモンも見つからないの。大きな面から置いてみて。" },
                shapeTip = new Dictionary<int, string[]>
                {
                    { 2, new[] { "りんごの輪郭がちょっと甘いかな。左の丸い形をよく見て。", "りんごの位置と大きさが怪しいわ。左の丸、どこから始まってる？" } },
                    { 3, new[] { "レモンのかたちが惜しい！右手前の少し尖った楕円よ。", "レモンはどこに行ったの？右手前の黄色いかたちよ。" } }
                },
                colorTip = "影は赤に青を少し足してみなされ。"
            },
            new StageDef
            {
                id = 2, name = "夕焼けの海", sub = "風景・グラデーション",
                labels = new[] { "空の上", "空の下", "太陽", "海", "反射" }, shapeCls = new[] { 2, 3, 4 },
                grad = true, gradRows = 9, cellVote = true,
                refNote = "空は上から「紺→紫→オレンジ→黄」。太い筆で横にスーッと引こう。",
                hintColor = new Dictionary<int, int[]> { { 4, new[] { 214, 112, 48 } } },
                hintExtra = new[] { ("紫", new[] { 112, 70, 112 }), ("橙→黄", new[] { 240, 160, 64 }) },
                hintNote = "紫＝赤＋青＋白。オレンジと黄のあいだは、両方を1つずつ混ぜた「つなぎ色」をはさむとなめらか。海は青2＋赤1。",
                shapeGood = new[] { "水平線がまっすぐで、太陽と光の道もきれいね。", "構図はかんぺき。太陽の沈み具合までちょうどいいわ。" },
                shapeLost = new[] { "空と海のさかいめが分からないわ。まず水平線を決めて。", "まだ景色になってないの。上を空、下を海でざっくり分けましょう。" },
                shapeTip = new Dictionary<int, string[]>
                {
                    { 2, new[] { "太陽が惜しい！水平線の真ん中に、半分しずんだ丸よ。", "太陽はどこ？水平線のまんなかに、白っぽい半円を置いて。" } },
                    { 3, new[] { "水平線の高さがずれてるわ。画面の下から4割くらいの所よ。", "海が足りないわ。下の4割を暗い紫紺で塗ってね。" } },
                    { 4, new[] { "海に映る光の道が惜しい。太陽の真下に縦にのびるオレンジよ。", "海に映る光の道が見えにくいわ。太陽の真下に、オレンジを縦にはっきり置いて。" } }
                },
                colorTip = "暗い空は赤と青で作れる。"
            },
            new StageDef
            {
                id = 3, name = "八角皿の赤い実", sub = "ほんものの油絵から",
                labels = new[] { "キャンバス", "鉛筆と影", "手前の実", "奥の実" }, shapeCls = new[] { 2, 3 },
                cellVote = true, blankOK = true, blackOK = true,
                refNote = "制作途中のほんものの油絵。白いキャンバスはそのまま残してOK。赤紫の実2つを、青みの影ごと描こう。",
                hintColor = new Dictionary<int, int[]> { { 2, new[] { 150, 40, 70 } }, { 3, new[] { 120, 35, 70 } } },
                hintExtra = new[] { ("実の影", new[] { 70, 40, 90 }), ("実の光", new[] { 215, 90, 110 }) },
                hintNote = "実は赤に青を少し。下の暗い所は青を増やして、光の所は赤＋白。白いキャンバスは塗らなくていいよ。鉛筆の線は灰色（白＋黒少し）。このステージだけは黒もOK！",
                shapeGood = new[] { "実の丸みと重なり、とても正確。お皿の上でちゃんと座ってるわ。", "2つの大きさの違いまでばっちり。奥行きが出てるわね。" },
                shapeLost = new[] { "実がどこにあるのか分からないわ。お皿のまん中より少し上よ。", "まず2つの丸を置きましょう。手前が大きく、右奥が小さいの。" },
                shapeTip = new Dictionary<int, string[]>
                {
                    { 2, new[] { "手前の実の丸が惜しい！真ん中より少し左の大きな丸よ。", "手前の大きな実が見つからないわ。中央左に大きく置いて。" } },
                    { 3, new[] { "奥の実が惜しい！手前の実の右上に、少し小さく重ねるの。", "奥の実はどこ？手前の実の右上に、小さめの丸を重ねて。" } }
                },
                colorTip = "実の暗い所は赤に青を足しなされ。"
            }
        };
    }
}
