using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UPointer = UnityEngine.UIElements.PointerType;

namespace Day113
{
    /// アトリエ画面。UI Toolkitだけで組む。文字は最小限。
    public class Atelier : MonoBehaviour
    {
        static readonly string[] TubeName = { "赤", "黄", "青", "白", "黒" };
        static readonly float[] SizesN = { 4, 9, 17, 28 }, SizesW = { 5, 10, 20, 32 };
        const int SubmitMin = 4000, ClearConfirm = 3000, StockMax = 6, ClearScore = 50;
        public StageDef Stage { get; private set; }
        public GradeResult LastGrade { get; private set; }
        float[] sizes = SizesN; bool hlock; float lockY;
        readonly int[] bests = new int[3];
        VisualElement refEl, refBigImg, selPanel, hintPanel, resultPanel; Label stName, stSub; Button lockBtn;

        public PaintCanvas Canvas { get; private set; }
        Sfx sfx;
        public readonly int[] Counts = new int[5];
        int sizeIdx = 2;
        readonly List<int[]> stock = new List<int[]>();

        VisualElement stageEl, easel, controls, topBar; FitBox ctrlFit, sideFit;
        bool? landscape;
        VisualElement root, canvasEl, swatch, pips, stockRow, loupe, loupeImg, refBig;
        readonly Label[] tubeCount = new Label[5];
        readonly VisualElement[] tubes = new VisualElement[5];
        readonly Button[] sizeBtn = new Button[4];
        Button undoBtn; Label toast; float toastT, clearPendingT;
        int pointerId = -1; Vector2 lastP; float lastT;

        void Start()
        {
            Application.targetFrameRate = 60;
            Canvas = new PaintCanvas();
            if (!FindFirstObjectByType<AudioListener>()) (Camera.main ? Camera.main.gameObject : gameObject).AddComponent<AudioListener>();
            sfx = gameObject.AddComponent<Sfx>();
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>("D113Theme");
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1080, 1920);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ps.match = .5f;
            var go = new GameObject("UI"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; go.SetActive(true);
            root = doc.rootVisualElement;
            Build();
            for (int i = 0; i < 3; i++) bests[i] = PlayerPrefs.GetInt("d113_best_" + Stages.All[i].id, 0);
            SetStage(0);
            ShowSelect();
        }

        void OnDestroy() { Canvas?.Dispose(); }

        void Update()
        {
            Canvas.Tick(Time.deltaTime);
            if (toastT > 0 && (toastT -= Time.deltaTime) <= 0) toast.style.opacity = 0;
            if (clearPendingT > 0) clearPendingT -= Time.deltaTime;
            undoBtn.SetEnabled(Canvas.UndoCount > 0);
        }

        // ---------- UI ----------
        static VisualElement Box(VisualElement parent, string name = null)
        { var v = new VisualElement { name = name }; parent?.Add(v); return v; }
        static void Round(VisualElement v, float r)
        { v.style.borderTopLeftRadius = v.style.borderTopRightRadius = v.style.borderBottomLeftRadius = v.style.borderBottomRightRadius = r; }
        static void Border(VisualElement v, float w, Color c)
        {
            v.style.borderTopWidth = v.style.borderBottomWidth = v.style.borderLeftWidth = v.style.borderRightWidth = w;
            v.style.borderTopColor = v.style.borderBottomColor = v.style.borderLeftColor = v.style.borderRightColor = c;
        }
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        Button Btn(VisualElement parent, string text, System.Action a, float w = 150)
        {
            var b = new Button(a) { text = text };
            b.style.width = w; b.style.height = 92; b.style.fontSize = 34; b.style.marginLeft = b.style.marginRight = 6;
            b.style.backgroundColor = Hex("#5a4636"); b.style.color = Hex("#f6ead6"); Round(b, 20); Border(b, 3, Hex("#8b6a4c"));
            parent.Add(b); return b;
        }

        void Build()
        {
            var font = Resources.Load<Font>("Fonts/MPLUS1p-Regular");
            if (font) root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            root.style.backgroundColor = new Color(.18f, .14f, .12f, .38f); // 段階3：後ろの3Dアトリエを透かす
            root.style.paddingTop = 24; root.style.paddingBottom = 24; root.style.paddingLeft = 16; root.style.paddingRight = 16;

            // 上：ステージ名とお手本
            var top = Box(null); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center; top.style.height = 150; top.style.flexShrink = 0;
            var tl = Box(top); tl.style.flexGrow = 1;
            stName = new Label(); stName.style.fontSize = 52; stName.style.color = Hex("#f6ead6"); stName.style.unityFontStyleAndWeight = FontStyle.Bold; tl.Add(stName);
            stSub = new Label(); stSub.style.fontSize = 30; stSub.style.color = Hex("#c9b294"); tl.Add(stSub);
            var stageBtn = Btn(top, "ステージ", () => { sfx.Tap(); ShowSelect(); }, 150); stageBtn.style.height = 80; stageBtn.style.fontSize = 28;
            lockBtn = Btn(top, "↔横", ToggleLock, 110); lockBtn.style.height = 80; lockBtn.style.fontSize = 30;
            var hintBtn = Btn(top, "ヒント", () => { sfx.Tap(); ShowHint(); }, 130); hintBtn.style.height = 80; hintBtn.style.fontSize = 28; hintBtn.style.marginRight = 14;
            refEl = Box(top); refEl.style.width = refEl.style.height = 136; Round(refEl, 8); Border(refEl, 5, Hex("#b8873d"));
            refEl.RegisterCallback<PointerDownEvent>(e => refBig.style.display = DisplayStyle.Flex);

            // キャンバス：木枠に布を張った本物のキャンバス（側面の厚みと影つき）
            stageEl = Box(null); stageEl.style.flexGrow = 1; stageEl.style.flexShrink = 1; stageEl.style.alignItems = Align.Center; stageEl.style.justifyContent = Justify.Center;
            easel = Box(stageEl);
            var shadow = Box(easel); shadow.style.position = Position.Absolute; shadow.style.left = 18; shadow.style.top = 22; shadow.style.right = -26; shadow.style.bottom = -30;
            shadow.style.backgroundColor = new Color(0, 0, 0, .38f); Round(shadow, 10);
            var sideR = Box(easel); sideR.style.position = Position.Absolute; sideR.style.top = 6; sideR.style.bottom = -8; sideR.style.right = -14; sideR.style.width = 16;
            sideR.style.backgroundColor = Hex("#cfc6b3"); sideR.style.borderTopRightRadius = sideR.style.borderBottomRightRadius = 5;
            var sideB = Box(easel); sideB.style.position = Position.Absolute; sideB.style.left = 6; sideB.style.right = -12; sideB.style.bottom = -14; sideB.style.height = 16;
            sideB.style.backgroundColor = Hex("#b9b09d"); sideB.style.borderBottomLeftRadius = sideB.style.borderBottomRightRadius = 5;
            for (int i = 0; i < 4; i++) // 側面の留め鋲
            {
                var st = Box(sideR); st.style.position = Position.Absolute; st.style.left = 4; st.style.top = Length.Percent(12 + i * 25); st.style.width = 7; st.style.height = 20; st.style.backgroundColor = Hex("#8d8a84"); Round(st, 2);
            }
            canvasEl = Box(easel); canvasEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Canvas.Display));
            Round(canvasEl, 4); Border(canvasEl, 2, Hex("#e8e0cf"));
            canvasEl.RegisterCallback<PointerDownEvent>(OnDown);
            canvasEl.RegisterCallback<PointerMoveEvent>(OnMove);
            canvasEl.RegisterCallback<PointerUpEvent>(OnUp);
            canvasEl.RegisterCallback<PointerCaptureOutEvent>(e => EndStroke());

            // ルーペ（タッチ中だけ）
            loupe = Box(canvasEl); loupe.style.position = Position.Absolute; loupe.style.width = loupe.style.height = 260;
            loupe.style.overflow = Overflow.Hidden; Round(loupe, 130); Border(loupe, 6, Hex("#f6ead6")); loupe.style.display = DisplayStyle.None; loupe.pickingMode = PickingMode.Ignore;
            loupeImg = Box(loupe); loupeImg.style.position = Position.Absolute; loupeImg.pickingMode = PickingMode.Ignore;
            loupeImg.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Canvas.Display));

            controls = Box(null); controls.style.flexShrink = 0; controls.style.alignItems = Align.Center;
            topBar = top;
            // パレット：混ぜた色＋滴の数
            var pal = Box(controls); pal.style.flexDirection = FlexDirection.Row; pal.style.alignItems = Align.Center; pal.style.height = 250;
            pal.style.backgroundColor = Hex("#c79a64"); Round(pal, 40); pal.style.paddingLeft = 24; pal.style.paddingRight = 24; Border(pal, 4, Hex("#8f6638"));
            var mixCol = Box(pal); mixCol.style.alignItems = Align.Center; mixCol.style.width = 230;
            swatch = Box(mixCol); swatch.style.width = swatch.style.height = 140; Round(swatch, 70); Border(swatch, 5, Hex("#7d5530"));
            pips = Box(mixCol); pips.style.flexDirection = FlexDirection.Row; pips.style.flexWrap = Wrap.Wrap; pips.style.width = 200; pips.style.marginTop = 12; pips.style.justifyContent = Justify.Center;
            for (int i = 0; i < MixCore.MaxDrops; i++) { var p = Box(pips); p.style.width = p.style.height = 22; Round(p, 11); p.style.marginLeft = p.style.marginRight = 3; p.style.marginBottom = 4; }
            var tubeRow = Box(pal); tubeRow.style.flexDirection = FlexDirection.Row; tubeRow.style.flexGrow = 1; tubeRow.style.justifyContent = Justify.SpaceAround;
            for (int i = 0; i < 5; i++) BuildTube(tubeRow, i);

            // ストック6つ
            stockRow = Box(controls); stockRow.style.flexDirection = FlexDirection.Row; stockRow.style.height = 100; stockRow.style.alignItems = Align.Center; stockRow.style.justifyContent = Justify.Center; stockRow.style.marginTop = 14;

            // 道具：筆サイズ・もどす・あらう・まっさら・見せる
            var tools = Box(controls); tools.style.flexDirection = FlexDirection.Row; tools.style.justifyContent = Justify.Center; tools.style.marginTop = 8;
            for (int i = 0; i < 4; i++)
            {
                int k = i; var b = Btn(tools, "", () => { sizeIdx = k; UpdateSizes(); sfx.Tap(); }, 82);
                var dot = Box(b); float d = 12 + i * 11; dot.style.width = dot.style.height = d; Round(dot, d / 2); dot.style.backgroundColor = Hex("#f6ead6"); dot.style.alignSelf = Align.Center;
                sizeBtn[i] = b;
            }
            var tools2 = tools;
            var gap = Box(tools2); gap.style.width = 16;
            undoBtn = Btn(tools2, "もどす", () => { if (Canvas.Undo()) sfx.Tap(); }, 150);
            Btn(tools2, "あらう", () => { System.Array.Clear(Counts, 0, 5); UpdateMix(); sfx.Wash(); }, 150);
            Btn(tools2, "まっさら", OnClear, 170);
            var done = Btn(tools2, "見せる", OnSubmit, 160); done.style.backgroundColor = Hex("#b5452f");
            UpdateSizes();

            root.RegisterCallback<GeometryChangedEvent>(e => Arrange());
            Arrange();

            // お手本の拡大
            refBig = Box(root); refBig.style.position = Position.Absolute; refBig.style.left = refBig.style.right = refBig.style.top = refBig.style.bottom = 0;
            refBig.style.backgroundColor = new Color(0, 0, 0, .75f); refBig.style.alignItems = Align.Center; refBig.style.justifyContent = Justify.Center; refBig.style.display = DisplayStyle.None;
            refBigImg = Box(refBig); refBigImg.style.width = refBigImg.style.height = 900; Border(refBigImg, 12, Hex("#b8873d"));
            refBig.RegisterCallback<PointerDownEvent>(e => refBig.style.display = DisplayStyle.None);

            toast = new Label(); toast.style.position = Position.Absolute; toast.style.alignSelf = Align.Center; toast.style.top = 260;
            toast.style.fontSize = 40; toast.style.color = Color.white; toast.style.backgroundColor = new Color(0, 0, 0, .7f); Round(toast, 30);
            toast.style.paddingLeft = toast.style.paddingRight = 40; toast.style.paddingTop = toast.style.paddingBottom = 18; toast.style.opacity = 0; toast.pickingMode = PickingMode.Ignore;
            root.Add(toast);
        }

        /// 縦持ち：上から 名前→キャンバス→道具。横長：左にキャンバス、右に名前と道具。キャンバスは入る限り大きく
        void Arrange()
        {
            var r = root.layout; if (float.IsNaN(r.width) || r.width <= 0) return;
            bool land = r.width > r.height * .85f;
            if (landscape != land)
            {
                landscape = land;
                topBar.RemoveFromHierarchy(); stageEl.RemoveFromHierarchy(); controls.RemoveFromHierarchy(); ctrlFit?.RemoveFromHierarchy(); sideFit?.RemoveFromHierarchy();
                if (land)
                {
                    root.style.flexDirection = FlexDirection.Row;
                    sideFit = new FitBox(false); sideFit.style.width = Length.Percent(46); sideFit.style.marginLeft = 16;
                    topBar.style.width = 960; sideFit.content.Add(topBar); sideFit.content.Add(controls);
                    root.Insert(0, stageEl); root.Insert(1, sideFit);
                }
                else
                {
                    root.style.flexDirection = FlexDirection.Column;
                    topBar.style.width = StyleKeyword.Auto;
                    ctrlFit = new FitBox(true); ctrlFit.content.Add(controls);
                    root.Insert(0, topBar); root.Insert(1, stageEl); root.Insert(2, ctrlFit);
                }
            }
            stageEl.schedule.Execute(() =>
            {
                var sl = stageEl.layout; if (float.IsNaN(sl.width)) return;
                float s = Mathf.Max(160, Mathf.Min(sl.width - 40, sl.height - 40));
                easel.style.width = easel.style.height = s; canvasEl.style.width = canvasEl.style.height = s;
            });
        }

        void BuildTube(VisualElement row, int i)
        {
            var col = Box(row); col.style.alignItems = Align.Center;
            var pig = MixCore.PIG[i]; var c = new Color((float)pig[0], (float)pig[1], (float)pig[2]);
            var tube = Box(col); tube.style.width = 96; tube.style.height = 130; tube.style.alignItems = Align.Center;
            var cap = Box(tube); cap.style.width = 44; cap.style.height = 26; cap.style.backgroundColor = Hex("#3a3a3a"); Round(cap, 6);
            var body = Box(tube); body.style.width = 96; body.style.flexGrow = 1; body.style.backgroundColor = c; Round(body, 18); Border(body, 4, Hex("#f2efe6"));
            body.style.alignItems = Align.Center; body.style.justifyContent = Justify.Center;
            var n = new Label("0"); n.style.fontSize = 40; n.style.unityFontStyleAndWeight = FontStyle.Bold; n.style.color = i == 3 ? Hex("#5a4636") : Color.white; body.Add(n);
            tubeCount[i] = n; tubes[i] = tube;
            tube.RegisterCallback<PointerDownEvent>(e => AddDrop(i));
            var minus = new Button(() => { if (Counts[i] > 0) { Counts[i]--; UpdateMix(); } }) { text = "−" };
            minus.style.width = 76; minus.style.height = 56; minus.style.fontSize = 38; minus.style.marginTop = 8; Round(minus, 16);
            minus.style.backgroundColor = Hex("#5a4636"); minus.style.color = Hex("#f6ead6");
            col.Add(minus);
        }

        public void AddDrop(int i)
        {
            if (MixCore.Total(Counts) >= MixCore.MaxDrops) { Toast("12滴まで"); return; }
            Counts[i]++; UpdateMix(); sfx.Squish(); sfx.Invoke(nameof(Sfx.Plop), .12f);
            // チューブを絞る動き
            tubes[i].style.scale = new Scale(new Vector3(1.12f, .86f, 1));
            tubes[i].schedule.Execute(() => tubes[i].style.scale = new Scale(Vector3.one)).ExecuteLater(110);
        }

        void UpdateMix()
        {
            int tot = MixCore.Total(Counts);
            for (int i = 0; i < 5; i++) tubeCount[i].text = Counts[i].ToString();
            if (MixCore.MixRGB(Counts, out var r, out var g, out var b))
            {
                swatch.style.backgroundColor = new Color((float)r / 255, (float)g / 255, (float)b / 255);
                swatch.style.scale = new Scale(new Vector3(1.08f, .94f, 1));
                swatch.schedule.Execute(() => swatch.style.scale = new Scale(Vector3.one)).ExecuteLater(140);
            }
            else swatch.style.backgroundColor = Hex("#e9dcc6");
            int k = 0;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < Counts[i]; j++)
                { var p = MixCore.PIG[i]; pips[k++].style.backgroundColor = new Color((float)p[0], (float)p[1], (float)p[2]); }
            for (; k < MixCore.MaxDrops; k++) pips[k].style.backgroundColor = new Color(0, 0, 0, .18f);
        }

        void UpdateSizes()
        {
            for (int i = 0; i < 4; i++) sizeBtn[i].style.backgroundColor = Hex(i == sizeIdx ? "#b8873d" : "#5a4636");
        }

        void AddStock()
        {
            var key = string.Join(",", Counts);
            stock.RemoveAll(s => string.Join(",", s) == key);
            stock.Insert(0, (int[])Counts.Clone());
            if (stock.Count > StockMax) stock.RemoveAt(StockMax);
            stockRow.Clear();
            foreach (var s in stock)
            {
                var rec = s; MixCore.MixRGB(rec, out var r, out var g, out var b);
                var dot = Box(stockRow); dot.style.width = dot.style.height = 80; Round(dot, 40); dot.style.marginLeft = dot.style.marginRight = 12;
                dot.style.backgroundColor = new Color((float)r / 255, (float)g / 255, (float)b / 255); Border(dot, 4, Hex("#c79a64"));
                dot.RegisterCallback<PointerDownEvent>(e => { System.Array.Copy(rec, Counts, 5); UpdateMix(); sfx.Tap(); });
            }
        }

        void Toast(string m) { toast.text = m; toast.style.opacity = 1; toastT = 1.7f; toast.BringToFront(); }

        void OnClear()
        {
            if (Canvas.PaintedPx > ClearConfirm && clearPendingT <= 0) { clearPendingT = 2.5f; Toast("もう一度で まっさら"); return; }
            clearPendingT = 0; Canvas.Reset();
        }

        void OnSubmit()
        {
            if (Canvas.PaintedPx < SubmitMin) { Toast("もう少し描いてから見せてね"); sfx.Locked(); return; }
            Submit();
        }

        // ---------- 採点：いまキャンバスにある絵そのものを読み取って採点する ----------
        readonly byte[] syncRGB = new byte[256 * 256 * 3];
        readonly float[] syncH = new float[256 * 256];
        public GradeResult Submit()
        {
            EndStroke();
            Canvas.Sync(syncRGB, syncH);
            var g = Grader.Grade(syncRGB, syncH, Canvas.Painted, Canvas.GetStats(), Stage);
            LastGrade = g;
            int idx = Stage.id - 1; bool wasOpen = idx + 1 < 3 && Unlocked(idx + 1);
            if (g.total > bests[idx]) { bests[idx] = g.total; PlayerPrefs.SetInt("d113_best_" + Stage.id, g.total); PlayerPrefs.Save(); }
            bool fresh = idx + 1 < 3 && !wasOpen && Unlocked(idx + 1);
            if (Application.CanStreamedLevelBeLoaded("Gallery") && !JudgeShow.Running && !JudgeShow.SkipShow)
                StartCoroutine(JudgeShow.Run(this, g, Stage, Canvas.Display, root, sfx, () => ShowResult(g, fresh)));
            else ShowResult(g, fresh);
            return g;
        }

        bool Unlocked(int i) => i == 0 || bests[i - 1] >= ClearScore;

        public void SetStage(int i)
        {
            Stage = Stages.All[i];
            Grader.GetRef(Stage);
            stName.text = Stage.name; stSub.text = Stage.sub;
            var tex = Resources.Load<Texture2D>("Refs/stage" + Stage.id);
            refEl.style.backgroundImage = new StyleBackground(tex); refBigImg.style.backgroundImage = new StyleBackground(tex);
            sizes = Stage.grad ? SizesW : SizesN; sizeIdx = Stage.grad ? 3 : 2; UpdateSizes();
            hlock = false; lockBtn.style.display = Stage.grad ? DisplayStyle.Flex : DisplayStyle.None; UpdateLock();
            Canvas.ResetHard(); System.Array.Clear(Counts, 0, 5); stock.Clear(); stockRow.Clear(); UpdateMix();
        }

        void ToggleLock() { hlock = !hlock; sfx.Tap(); UpdateLock(); Toast(hlock ? "まっすぐ横に引けるよ" : "横ロックOFF"); }
        void UpdateLock() => lockBtn.style.backgroundColor = Hex(hlock ? "#b8873d" : "#5a4636");

        VisualElement Overlay()
        {
            var o = Box(root); o.style.position = Position.Absolute; o.style.left = o.style.right = o.style.top = o.style.bottom = 0;
            o.style.backgroundColor = new Color(.09f, .06f, .05f, .94f);
            var fit = new FitBox(false); fit.style.position = Position.Absolute; fit.style.left = fit.style.right = 24; fit.style.top = fit.style.bottom = 24; o.Add(fit);
            fit.content.RegisterCallback<DetachFromPanelEvent>(_ => o.RemoveFromHierarchy());
            return fit.content;
        }
        static Label Txt(VisualElement p, string t, float size, string col, bool bold = false)
        {
            var l = new Label(t); l.style.fontSize = size; l.style.color = Hex(col); l.style.whiteSpace = WhiteSpace.Normal;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold; p.Add(l); return l;
        }
        static Color Rgb(double[] c) => new Color((float)c[0] / 255, (float)c[1] / 255, (float)c[2] / 255);

        // ---------- ステージ選択 ----------
        public void ShowSelect()
        {
            selPanel?.RemoveFromHierarchy();
            selPanel = Overlay();
            Txt(selPanel, "ステージ", 56, "#f1d48a", true).style.marginBottom = 30;
            var grid = Box(selPanel); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.width = 1000; grid.style.justifyContent = Justify.Center;
            for (int i = 0; i < 4; i++)
            {
                int k = i; bool real = i < 3, open = real && Unlocked(i);
                var card = Box(grid); card.style.width = 460; card.style.marginLeft = card.style.marginRight = 16; card.style.marginBottom = 30; card.style.alignItems = Align.Center;
                card.style.backgroundColor = Hex("#3b2e25"); Round(card, 24); card.style.paddingTop = 20; card.style.paddingBottom = 20; Border(card, 4, Hex(open ? "#b8873d" : "#4a3b30"));
                var th = Box(card); th.style.width = th.style.height = 380; Round(th, 8); th.style.backgroundColor = Hex("#1e1712");
                if (real) th.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("Refs/stage" + Stages.All[i].id));
                if (!open) th.style.unityBackgroundImageTintColor = new Color(.25f, .25f, .25f);
                Txt(card, real ? Stages.All[i].name : "つづく…", 38, open ? "#f6ead6" : "#9d8f7c", true).style.marginTop = 12;
                Txt(card, !real ? "じゅんび中" : !open ? "ステージ" + i + "で" + ClearScore + "点以上" : bests[i] > 0 ? "ベスト " + bests[i] + "点" : Stages.All[i].sub, 28, "#c9b294");
                card.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (!open) { sfx.Locked(); Toast(real ? "ステージ" + k + "で" + ClearScore + "点以上とると開くよ" : "じゅんび中"); return; }
                    sfx.Tap(); if (Stage != Stages.All[k] || Canvas.PaintedPx > 0) SetStage(k);
                    selPanel.RemoveFromHierarchy(); selPanel = null;
                });
            }
        }

        // ---------- 混ぜ方ヒント ----------
        void ShowHint()
        {
            hintPanel?.RemoveFromHierarchy();
            hintPanel = Overlay();
            var card = Box(hintPanel); card.style.width = 960; card.style.backgroundColor = Hex("#f3e9d6"); Round(card, 30);
            card.style.paddingLeft = card.style.paddingRight = 44; card.style.paddingTop = card.style.paddingBottom = 36;
            var R = Grader.GetRef(Stage);
            var rows = new List<(string, double[])>();
            for (int k = 0; k < Stage.labels.Length; k++)
            {
                if (Stage.blankOK && k == 0) continue;
                rows.Add((Stage.labels[k], Stage.hintColor.TryGetValue(k, out var hc) ? new double[] { hc[0], hc[1], hc[2] } : R.meanRGB[k]));
            }
            foreach (var x in Stage.hintExtra) rows.Add((x.name, new double[] { x.rgb[0], x.rgb[1], x.rgb[2] }));
            foreach (var (label, col) in rows)
            {
                var row = Box(card); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 18;
                var sw = Box(row); sw.style.width = sw.style.height = 76; Round(sw, 38); sw.style.backgroundColor = Rgb(col); Border(sw, 3, Hex("#7d5530"));
                var lb = Txt(row, label, 36, "#3a2a1e", true); lb.style.width = 230; lb.style.marginLeft = 20;
                var rec = Grader.BestRecipe(col);
                var dots = Box(row); dots.style.flexDirection = FlexDirection.Row; dots.style.flexWrap = Wrap.Wrap; dots.style.flexGrow = 1;
                for (int p = 0; p < 4; p++) for (int n = 0; n < rec[p]; n++)
                {
                    var d = Box(dots); d.style.width = d.style.height = 40; Round(d, 20); d.style.marginRight = 6; d.style.marginBottom = 4;
                    d.style.backgroundColor = Rgb(new[] { MixCore.PIG[p][0] * 255, MixCore.PIG[p][1] * 255, MixCore.PIG[p][2] * 255 }); Border(d, 2, Hex("#7d5530"));
                }
            }
            Txt(card, Stage.hintNote, 32, "#5a4636").style.marginTop = 10;
            var ok = Btn(card, "わかった", () => { sfx.Tap(); hintPanel.RemoveFromHierarchy(); }, 280); ok.style.alignSelf = Align.Center; ok.style.marginTop = 26;
        }

        // ---------- 結果（先生3人の採点）----------
        void ShowResult(GradeResult g, bool fresh)
        {
            resultPanel?.RemoveFromHierarchy();
            resultPanel = Overlay(); sfx.Chime();
            var card = Box(resultPanel); card.style.width = 1000; card.style.flexShrink = 0; card.style.backgroundColor = Hex("#f3e9d6"); Round(card, 30);
            card.style.paddingLeft = card.style.paddingRight = 40; card.style.paddingTop = card.style.paddingBottom = 30;
            var head = Box(card); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            var art = Box(head); art.style.width = art.style.height = 300; Border(art, 14, Hex("#a77a45"));
            art.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Canvas.Display));
            var hc = Box(head); hc.style.flexGrow = 1; hc.style.alignItems = Align.Center;
            int stars = Grader.JsRound(g.total / 20.0);
            Txt(hc, new string('★', stars) + new string('☆', 5 - stars), 70, "#c8922e");
            var tr = Box(hc); tr.style.flexDirection = FlexDirection.Row; tr.style.alignItems = Align.FlexEnd;
            Txt(tr, "総合", 34, "#5a4636").style.marginBottom = 22;
            var tot = Txt(tr, "0", 130, "#b5452f", true); tot.style.marginLeft = 10;
            Txt(tr, "点", 34, "#5a4636").style.marginBottom = 22;
            Txt(hc, Judges.Verdict(g.total), 34, "#3a2a1e", true);
            CountUp(tot, g.total, 1500);
            int[] sc = { g.color, g.shape, g.feel };
            string[] cm = { Judges.Color(g, Stage), Judges.Shape(g, Stage), Judges.Feel(g, Stage) };
            for (int j = 0; j < 3; j++)
            {
                var row = Box(card); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 22; row.style.alignItems = Align.Center;
                var nb = Box(row); nb.style.width = 200; nb.style.flexShrink = 0; nb.style.alignItems = Align.Center;
                Txt(nb, Judges.Name[j], 30, "#3a2a1e", true); Txt(nb, Judges.Role[j], 26, "#8a6a4a");
                var s = Txt(nb, "0", 64, "#b5452f", true);
                CountUp(s, sc[j], 300 + j * 350);
                var c = Txt(row, cm[j], 32, "#3a2a1e"); c.style.flexShrink = 1; c.style.flexGrow = 1; c.style.marginLeft = 16;
                c.style.backgroundColor = Hex("#fffaf0"); Round(c, 20); c.style.paddingLeft = c.style.paddingRight = 22; c.style.paddingTop = c.style.paddingBottom = 14;
            }
            int idx = Stage.id - 1; string note = null;
            if (fresh) note = "ステージ" + (idx + 2) + "「" + Stages.All[idx + 1].name + "」が開いた！";
            else if (g.total < ClearScore && idx + 1 < 3 && !Unlocked(idx + 1)) note = "あと " + (ClearScore - g.total) + "点 で次のステージ！";
            if (note != null) { var n = Txt(card, note, 36, "#b5452f", true); n.style.alignSelf = Align.Center; n.style.marginTop = 22; if (fresh) sfx.Invoke(nameof(Sfx.Chime), 1.6f); }
            var btns = Box(card); btns.style.flexDirection = FlexDirection.Row; btns.style.justifyContent = Justify.Center; btns.style.marginTop = 26;
            Btn(btns, "見なおす", () => { sfx.Tap(); resultPanel.RemoveFromHierarchy(); }, 220);
            Btn(btns, "ステージ", () => { sfx.Tap(); resultPanel.RemoveFromHierarchy(); ShowSelect(); }, 220);
            Btn(btns, "もう一枚", () => { sfx.Tap(); resultPanel.RemoveFromHierarchy(); Canvas.ResetHard(); }, 220).style.backgroundColor = Hex("#b5452f");
        }

        static void CountUp(Label l, int target, long delayMs)
        {
            float t0 = -1; l.text = "0";
            l.schedule.Execute(() =>
            {
                if (t0 < 0) t0 = Time.unscaledTime;
                float k = Mathf.Clamp01((Time.unscaledTime - t0) / .9f);
                l.text = Mathf.RoundToInt(target * k * (2 - k)).ToString();
            }).StartingIn(delayMs).Every(16).Until(() => t0 >= 0 && Time.unscaledTime - t0 > .95f);
        }

        // ---------- 描く ----------
        Vector2 ToCanvas(Vector2 world)
        {
            var l = canvasEl.WorldToLocal(world); var s = canvasEl.layout.width;
            return new Vector2(l.x / s * 256f, (1f - l.y / s) * 256f);
        }
        static float PressureMul(IPointerEvent e)
        {
            bool real = e.pointerType == UPointer.pen || (e.pressure > 0 && e.pressure < 1 && Mathf.Abs(e.pressure - .5f) > .001f);
            return real ? Mathf.Lerp(.55f, 1.25f, e.pressure) : 1f;
        }

        void OnDown(PointerDownEvent e)
        {
            if (pointerId >= 0) return; // 2本目の指は無視
            var p = ToCanvas(e.position);
            if (!BeginStroke(p, PressureMul(e))) { Toast("色を作ってね"); return; }
            pointerId = e.pointerId; canvasEl.CapturePointer(pointerId);
            if (e.pointerType == UPointer.touch) { loupe.style.display = DisplayStyle.Flex; MoveLoupe(e.position); }
        }
        void OnMove(PointerMoveEvent e)
        {
            if (e.pointerId != pointerId) return;
            StrokeTo(ToCanvas(e.position), PressureMul(e));
            if (loupe.style.display == DisplayStyle.Flex) MoveLoupe(e.position);
        }
        void OnUp(PointerUpEvent e) { if (e.pointerId == pointerId) EndStroke(); }

        public bool BeginStroke(Vector2 p256, float pressure = 1)
        {
            lockY = p256.y;
            if (!Canvas.BeginStroke(Counts, sizes[sizeIdx], p256, pressure)) return false;
            AddStock(); lastP = p256; lastT = Time.unscaledTime; return true;
        }
        public void StrokeTo(Vector2 p256, float pressure = 1)
        {
            if (hlock) p256.y = lockY;
            float dt = Mathf.Max(1e-3f, Time.unscaledTime - lastT);
            float speed01 = Mathf.Clamp01((p256 - lastP).magnitude / dt / 900f);
            float wm = pressure * Mathf.Lerp(1.08f, .82f, speed01);
            Canvas.StrokeTo(p256, wm, speed01); lastP = p256; lastT = Time.unscaledTime;
            sfx.Brush(Mathf.Max(.05f, speed01), Canvas.Load);
        }
        public void EndStroke()
        {
            if (pointerId >= 0 && canvasEl.HasPointerCapture(pointerId)) canvasEl.ReleasePointer(pointerId);
            pointerId = -1; Canvas.EndStroke(); loupe.style.display = DisplayStyle.None;
        }

        void MoveLoupe(Vector2 world)
        {
            var l = canvasEl.WorldToLocal(world); float s = canvasEl.layout.width, z = 3f, L = 260;
            bool topLeft = l.x < s * .5f && l.y < s * .5f;
            loupe.style.left = topLeft ? s - L - 10 : 10; loupe.style.top = 10;
            loupeImg.style.width = loupeImg.style.height = s * z;
            loupeImg.style.left = L / 2 - l.x * z; loupeImg.style.top = L / 2 - l.y * z;
        }
    }
}
