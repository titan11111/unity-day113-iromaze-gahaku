using System; using System.Collections;
using UnityEngine; using UnityEngine.SceneManagement; using Object = UnityEngine.Object; using UnityEngine.UIElements;
namespace Day113
{
    /// 審査の演出：展示室へ移動 → 先生が1人ずつ絵の前へ歩く → 覗き込む → 振り向いて講評と点数 → 戻る → 総合。タップで早送り。
    public static class JudgeShow
    {
        public static bool Running { get; private set; }
        public static Action<string> DebugShot;
        public static Exhibit.Result ExhibitResult;
        public static bool SkipShow; // テスト専用：審査の演出を飛ばして結果画面だけ出す
        public static int TestTotal = -1; // テスト専用：総合点を上書き（怪盗・大富豪の確認） // テスト時にスクショを撮る

        public static IEnumerator Run(MonoBehaviour host, GradeResult g, StageDef st, Texture art, VisualElement uiRoot, Sfx sfx, Action done)
        {
            Running = true;
            if (TestTotal >= 0) { g.total = TestTotal; g.color = Mathf.Min(100, TestTotal + 3); g.shape = TestTotal; g.feel = Mathf.Min(100, TestTotal + 5); TestTotal = -1; }
            var atelierCam = Camera.main;
            var rt = new RenderTexture(art.width, art.height, 0); Graphics.Blit(art, rt);
            var load = SceneManager.LoadSceneAsync("Gallery", LoadSceneMode.Additive);
            while (!load.isDone) yield return null;
            var gal = SceneManager.GetSceneByName("Gallery");
            JudgeRig jr = null;
            foreach (var go in gal.GetRootGameObjects()) { jr = go.GetComponentInChildren<JudgeRig>(); if (jr) break; }
            if (jr == null) { Debug.LogError("[D113] JudgeRig not found"); Finish(atelierCam, uiRoot, rt); done?.Invoke(); yield break; }

            // アトリエの部屋と照明は審査中だけ隠す（同じ位置に重なるため）
            var hidden = new System.Collections.Generic.List<GameObject>();
            foreach (var go in host.gameObject.scene.GetRootGameObjects())
                if (go.activeSelf && go != host.gameObject && (go.GetComponentInChildren<Renderer>() || go.GetComponent<Light>())) { go.SetActive(false); hidden.Add(go); }
            var amb = RenderSettings.ambientLight; RenderSettings.ambientLight = new Color(.38f, .26f, .2f);
            if (atelierCam) atelierCam.enabled = false;
            var al = atelierCam ? atelierCam.GetComponent<AudioListener>() : null; if (al) al.enabled = false;
            jr.cam.enabled = true;
            var jal = jr.cam.GetComponent<AudioListener>(); if (!jal) jal = jr.cam.gameObject.AddComponent<AudioListener>(); jal.enabled = true; // 審査中も音が聞こえるように
            uiRoot.style.display = DisplayStyle.None;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var pm = new Material(lit); pm.SetTexture("_BaseMap", rt); pm.SetFloat("_Smoothness", .25f);
            // 額の中に、向きの正しい板を重ねて絵を貼る（元の箱のUVは向きが合わないため）
            var pb = jr.painting.bounds;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "PlayerArt";
            Object.Destroy(quad.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(quad, gal);
            quad.transform.position = new Vector3(pb.center.x, pb.center.y, pb.max.z + .006f);
            quad.transform.rotation = Quaternion.LookRotation(Vector3.back);
            float side = Mathf.Min(pb.size.x, pb.size.y);
            quad.transform.localScale = new Vector3(side, side, 1);
            quad.GetComponent<Renderer>().sharedMaterial = pm;

            var layer = new VisualElement(); layer.style.position = Position.Absolute; layer.style.left = layer.style.right = layer.style.top = layer.style.bottom = 0;
            layer.pickingMode = PickingMode.Position; uiRoot.parent.Add(layer);
            var jfont = Resources.Load<Font>("Fonts/MPLUS1p-Regular"); // Web版は端末の日本語フォントが使えない → 同梱フォントを指定
            if (jfont) layer.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(jfont));
            bool tapped = false; layer.RegisterCallback<PointerDownEvent>(_ => tapped = true);

            // 人物の「休み」の姿勢を覚える（ビルドでも姿勢が戻るように。PoseLib）
            PoseLib.CaptureRest(jr.gameObject, true);
            for (int k = 0; k < 3; k++) { cur[k] = jr.stand[k]; curT[k] = 0; }
            var camT = jr.cam.transform; Vector3 camHome = camT.position; Quaternion camRot = camT.rotation;
            var homes = new Vector3[3]; var homeRot = new Quaternion[3];
            // 先生は壁ぎわに並んで待つ（絵の前をあける）
            var baseRot = new Quaternion[3]; for (int i = 0; i < 3; i++) baseRot[i] = jr.rigs[i].rotation; // 正面(+Z)を向いた時の元の傾き
            Vector3[] wait = { new Vector3(-1.55f, 0, -1.0f), new Vector3(1.55f, 0, -1.0f), new Vector3(1.55f, 0, .15f) };
            for (int i = 0; i < 3; i++)
            {
                var p = wait[i]; p.y = jr.rigs[i].position.y; jr.rigs[i].position = p;
                var face = new Vector3(0, p.y, .6f) - p; face.y = 0; jr.rigs[i].rotation = Quaternion.LookRotation(face.normalized) * baseRot[i];
                homes[i] = jr.rigs[i].position; homeRot[i] = jr.rigs[i].rotation; Samp(jr, jr.stand[i], 0);
            }
            // カメラ：Web版と同じ動き（JudgeCam）。Unityは左手系なので、画面の右＝ワールドの -X
            var P = new Vector3(pb.center.x, pb.center.y, pb.max.z); // 絵の正面の中心
            var jc = jr.cam.gameObject.AddComponent<JudgeCam>();
            jc.Shot(P + new Vector3(-2.4f, .2f, 6.2f), P + new Vector3(0, -.1f, .5f), 1.2f, true);
            var title = JudgeShowUI.Banner(layer, "審査がはじまります", 64);
            yield return Wait(.3f, () => tapped);
            jc.Shot(P + new Vector3(0, -.05f, 3.4f), P, 1.0f);
            yield return Wait(1.6f, () => tapped); DebugShot?.Invoke("judge_title"); yield return Wait(.5f, () => tapped); title.RemoveFromHierarchy(); tapped = false;

            int[] scores = { g.color, g.shape, g.feel };
            string[] comments = { Judges.Color(g, st), Judges.Shape(g, st), Judges.Feel(g, st) };
            Vector3 spot = new Vector3(0, homes[1].y, -2.55f);
            for (int i = 0; i < 3; i++)
            {
                var r = jr.rigs[i]; tapped = false;
                // 絵の前へ歩く
                // 柵（ロープ）の端を回り込んで絵の前へ（HTML版と同じ道順）
                float sideX = homes[i].x < 0 ? -1.25f : 1.25f;
                var c1 = new Vector3(sideX, homes[i].y, -1.85f); var c2 = new Vector3(sideX, homes[i].y, -2.55f);
                var mySpot = new Vector3(i == 1 ? 0f : (i == 0 ? -.35f : .35f), homes[i].y, -2.6f);
                float sg = (homes[i].x < 0 || i == 1) ? -1f : 1f, fy = homes[i].y; // カメラは先生が来た側（2人目は待っている先生をよけて反対側）
                int ii = i;
                // 歩く先生を右うしろから追う
                jc.follow = () => { var p = jr.rigs[ii].position; return (new Vector3(p.x + sg * (JudgeCam.Portrait ? 1.1f : 1.6f), fy + 1.75f, p.z + 2.6f), new Vector3(P.x + (p.x - P.x) * .6f, fy + 1.6f, p.z - .6f), 2f); };
                yield return Walk(jr, i, homes[i], c1, .9f, () => tapped, baseRot[i]); DebugShot?.Invoke($"judge_{i}_walk");
                yield return Walk(jr, i, c1, c2, .7f, () => tapped, baseRot[i]);
                yield return Walk(jr, i, c2, mySpot, .6f, () => tapped, baseRot[i]);
                yield return Turn(r, Quaternion.LookRotation(Vector3.back) * baseRot[i], .35f);
                Samp(jr, jr.inspect[i], 0); SetMag(jr, i, true);
                // 虫眼鏡：肩ごしのアップ（頭は画面の端、絵が大きく迫る）
                var rp = new Vector3(r.position.x, fy, r.position.z);
                jc.Shot(rp + new Vector3(sg * .95f, 1.64f, .17f), rp + new Vector3(-sg * .05f, 1.58f, -.38f), 1.6f);
                tapped = false;
                var bubble = JudgeShowUI.Bubble(layer, Judges.Name[i] + "（" + Judges.Role[i] + "）", comments[i], out var scoreL, out var bodyL);
                scoreL.style.visibility = Visibility.Hidden;
                yield return Typewriter(bodyL, "…じ〜っ……", () => tapped, () => sfx?.Voice(ii));
                yield return Wait(1.2f, () => tapped);
                DebugShot?.Invoke($"judge_{i}_inspect");
                yield return Wait(1.2f, () => tapped);
                // 振り向いて講評：正面のバストアップ
                Samp(jr, jr.stand[i], 0); SetMag(jr, i, false);
                jc.Shot(rp + new Vector3(sg * .22f, 1.62f, 1.4f), rp + new Vector3(0, 1.6f, 0), 1.8f);
                yield return Turn(r, baseRot[i], .35f);
                yield return Wait(.5f, () => false);
                tapped = false; sfx?.Tap();
                yield return Typewriter(bodyL, comments[i], () => tapped, () => sfx?.Voice(ii));
                yield return Wait(.5f, () => tapped);
                DebugShot?.Invoke($"judge_{i}_comment");
                // 札ボン：引きながら、右腕で点数札を振り上げる＋右下の点数がポン
                jc.Shot(rp + new Vector3(sg * .3f + .2f, 1.78f, 2.3f), rp + new Vector3(.2f, 1.82f, 0), 1.8f); // 札（先生の右手＝ワールド+X）が切れないよう少し寄せる
                var card = ScoreCard.Make(r, scores[i]);
                if (!card.Ok) Debug.LogWarning("[D113] 札：右腕の骨が見つからない " + r.name);
                scoreL.style.visibility = Visibility.Visible;
                host.StartCoroutine(Stamp(scoreL)); host.StartCoroutine(CountUp(scoreL, scores[i], () => false));
                sfx?.Bon();
                tapped = false; float ct = 0; bool shot = false;
                while (ct < 2.2f && !(tapped && ct > .6f))
                {
                    ct += Time.deltaTime; Samp(jr, jr.stand[i], 0);
                    card.Apply(ScoreCard.RaiseCurve(ct), ScoreCard.TiltCurve(ct), camT.position);
                    if (!shot && ct > .9f) { shot = true; DebugShot?.Invoke($"judge_{i}_card"); }
                    yield return null;
                }
                for (float dt2 = 0; dt2 < .25f; dt2 += Time.deltaTime) { Samp(jr, jr.stand[i], 0); card.Apply(Mathf.Max(0f, 1f - dt2 / .25f), 0, camT.position); yield return null; }
                card.Destroy(); Samp(jr, jr.stand[i], 0); // 休みの姿勢へ戻してから立ち姿 → 腕は下りる
                bubble.RemoveFromHierarchy();
                // 元の場所へ
                yield return Turn(r, Quaternion.LookRotation(Flat(homes[i] - spot)) * baseRot[i], .3f);
                jc.follow = () => { var p = jr.rigs[ii].position; return (new Vector3(p.x + sg * (JudgeCam.Portrait ? 1.1f : 1.6f), fy + 1.75f, p.z + 2.6f), new Vector3(P.x + (p.x - P.x) * .6f, fy + 1.6f, p.z - .6f), 2f); };
                yield return Walk(jr, i, mySpot, c2, .6f, () => tapped, baseRot[i]);
                yield return Walk(jr, i, c2, c1, .7f, () => tapped, baseRot[i]);
                yield return Walk(jr, i, c1, homes[i], .9f, () => tapped, baseRot[i]);
                yield return Turn(r, homeRot[i], .3f);
                Samp(jr, jr.stand[i], 0);
                jc.Shot(P + new Vector3(0, -.05f, 3.4f), P, 1.4f);
            }
            Object.Destroy(jc);
            tapped = false;
            camT.SetPositionAndRotation(new Vector3(0, 1.7f, 1.6f), Quaternion.LookRotation(new Vector3(0, 1.2f, -1.2f) - new Vector3(0, 1.7f, 1.6f)));
            var fin = JudgeShowUI.Banner(layer, $"総合 {g.total}点\n{Judges.Verdict(g.total)}", 70);
            sfx?.Chime(); DebugShot?.Invoke("judge_total");
            yield return Wait(2.6f, () => tapped);
            fin.RemoveFromHierarchy();
            CrowdKit kit = null; foreach (var go in gal.GetRootGameObjects()) { kit = go.GetComponentInChildren<CrowdKit>(); if (kit) break; }
            if (kit != null)
            {
                var lights = new System.Collections.Generic.List<Light>(); foreach (var go in gal.GetRootGameObjects()) lights.AddRange(go.GetComponentsInChildren<Light>());
                ExhibitResult = new Exhibit.Result(); tapped = false;
                yield return Exhibit.Run(jr, kit, g, layer, jr.cam, lights.ToArray(), () => tapped, ExhibitResult, DebugShot);
            }
            layer.RemoveFromHierarchy();
            camT.SetPositionAndRotation(camHome, camRot);
            yield return SceneManager.UnloadSceneAsync(gal);
            foreach (var go in hidden) if (go) go.SetActive(true);
            RenderSettings.ambientLight = amb;
            Finish(atelierCam, uiRoot, rt);
            if (jal) jal.enabled = false;
            if (al) al.enabled = true;
            done?.Invoke();
        }

        static JudgeRig cur;
        public static void PoseSurprise(JudgeRig jr, int i) { var c = jr.surprise != null && jr.surprise.Length > i ? jr.surprise[i] : null; Samp(jr, c ?? jr.stand[i], 0); }
        public static void PoseStand(JudgeRig jr, int i) { Samp(jr, jr.stand[i], 0); }

        static void Finish(Camera atelierCam, VisualElement uiRoot, RenderTexture rt)
        {
            if (atelierCam) atelierCam.enabled = true;
            uiRoot.style.display = DisplayStyle.Flex;
            Running = false;
        }

        /// ポーズを当てる。3人それぞれ「今の動き」を覚えておき、毎回 休み→3人分 の順に当てる（1人だけ動かしても他の2人が崩れない）。
        /// クリップは「先生3人の親」基準のパスなので、3人のルートの位置と向きは保つ。
        static readonly AnimationClip[] cur = new AnimationClip[3]; static readonly float[] curT = new float[3];
        static void Samp(JudgeRig jr, AnimationClip c, float t)
        {
            if (!c) return;
            int who = Owner(jr, c); if (who >= 0) { cur[who] = c; curT[who] = t; }
            var p = new Vector3[3]; var q = new Quaternion[3]; var sc = new Vector3[3];
            for (int k = 0; k < 3; k++) { var tr = jr.rigs[k]; p[k] = tr.position; q[k] = tr.rotation; sc[k] = tr.localScale; }
            if (who < 0) PoseLib.Sample(c, jr.gameObject, t);
            else { bool first = true; for (int k = 0; k < 3; k++) if (cur[k]) { PoseLib.Sample(cur[k], jr.gameObject, curT[k], first); first = false; } }
            for (int k = 0; k < 3; k++) { var tr = jr.rigs[k]; tr.SetPositionAndRotation(p[k], q[k]); tr.localScale = sc[k]; }
        }
        static int Owner(JudgeRig jr, AnimationClip c)
        {
            for (int k = 0; k < 3; k++)
                if (Has(jr.walk, k, c) || Has(jr.stand, k, c) || Has(jr.inspect, k, c) || Has(jr.surprise, k, c)) return k;
            return -1;
        }
        static bool Has(AnimationClip[] a, int k, AnimationClip c) => a != null && a.Length > k && a[k] == c;

        static void SetMag(JudgeRig jr, int i, bool on)
        {
            if (jr.mags == null) return; string key = new[] { "Elder", "Critic", "Loupe" }[i];
            foreach (var m in jr.mags) if (m && m.name.StartsWith(key)) m.enabled = on;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

        static IEnumerator Walk(JudgeRig jr, int i, Vector3 from, Vector3 to, float dur, Func<bool> skip, Quaternion baseRot)
        {
            var r = jr.rigs[i]; var dir = to - from; dir.y = 0;
            if (dir.sqrMagnitude > 1e-4f) r.rotation = Quaternion.LookRotation(dir.normalized) * baseRot;
            var clip = jr.walk[i]; float t = 0;
            while (t < dur && !skip())
            {
                t += Time.deltaTime; float k = Mathf.SmoothStep(0, 1, t / dur);
                r.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Abs(Mathf.Sin(t * 7.5f)) * .015f;
                if (clip) Samp(jr, clip, (t * 1.25f) % clip.length);
                yield return null;
            }
            r.position = to;
        }

        static IEnumerator Turn(Transform r, Quaternion to, float dur)
        {
            var from = r.rotation; float t = 0;
            while (t < dur) { t += Time.deltaTime; r.rotation = Quaternion.Slerp(from, to, t / dur); yield return null; }
            r.rotation = to;
        }

        static IEnumerator MoveCam(Transform c, Vector3 pos, Vector3 look, float dur)
        {
            Vector3 p0 = c.position; Quaternion q0 = c.rotation, q1 = Quaternion.LookRotation(look - pos); float t = 0;
            while (t < dur) { t += Time.deltaTime; float k = Mathf.SmoothStep(0, 1, t / dur); c.position = Vector3.Lerp(p0, pos, k); c.rotation = Quaternion.Slerp(q0, q1, k); yield return null; }
            c.SetPositionAndRotation(pos, q1);
        }

        /// 右下の点数がスタンプのように「ポン」と出る（0→1.25→1倍）
        static IEnumerator Stamp(VisualElement e)
        {
            float t = 0;
            while (t < .45f) { t += Time.deltaTime; float x = t / .45f; float s = x < .55f ? Mathf.Lerp(0f, 1.25f, x / .55f) : Mathf.Lerp(1.25f, 1f, (x - .55f) / .45f); e.style.scale = new Scale(new Vector3(s, s, 1)); yield return null; }
            e.style.scale = new Scale(Vector3.one);
        }

        static IEnumerator Wait(float s, Func<bool> skip) { float t = 0; while (t < s && !skip()) { t += Time.deltaTime; yield return null; } }

        /// 文字送り。voice があれば2文字ごとに「ポ」と鳴らす（記号・空白では鳴らさない）
        static IEnumerator Typewriter(Label l, string s, Func<bool> skip, Action voice = null)
        {
            int said = 0;
            for (int i = 1; i <= s.Length && !skip(); i++)
            {
                l.text = s.Substring(0, i); char ch = s[i - 1];
                if (voice != null && !char.IsWhiteSpace(ch) && "…、。！？!?〜ー「」・".IndexOf(ch) < 0 && (said++ % 2 == 0)) voice();
                yield return new WaitForSeconds(.045f);
            }
            l.text = s;
        }

        static IEnumerator CountUp(Label l, int to, Func<bool> skip)
        {
            float t = 0;
            while (t < .8f && !skip()) { t += Time.deltaTime; l.text = Mathf.RoundToInt(to * Mathf.Min(1f, t / .8f)) + "点"; yield return null; }
            l.text = to + "点";
        }

    }

    public static class JudgeShowUI
    {
        public static VisualElement Banner(VisualElement layer, string text, int size)
        {
            var b = new Label(text); b.style.position = Position.Absolute; b.style.left = b.style.right = 0; b.style.top = Length.Percent(38);
            b.style.unityTextAlign = TextAnchor.MiddleCenter; b.style.fontSize = size; b.style.color = new Color(1f, .95f, .82f);
            b.style.backgroundColor = new Color(0, 0, 0, .55f); b.style.paddingTop = b.style.paddingBottom = 30; b.style.whiteSpace = WhiteSpace.Normal;
            b.style.unityFontStyleAndWeight = FontStyle.Bold; layer.Add(b); return b;
        }

        public static VisualElement Bubble(VisualElement layer, string who, string text, out Label score, out Label body)
        {
            var c = new VisualElement(); c.style.position = Position.Absolute; c.style.left = 40; c.style.right = 40; c.style.bottom = 120;
            c.style.backgroundColor = new Color(.96f, .92f, .84f, .96f);
            c.style.borderTopLeftRadius = c.style.borderTopRightRadius = c.style.borderBottomLeftRadius = c.style.borderBottomRightRadius = 30;
            c.style.paddingLeft = c.style.paddingRight = 36; c.style.paddingTop = c.style.paddingBottom = 26;
            var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.justifyContent = Justify.SpaceBetween; head.style.alignItems = Align.Center; c.Add(head);
            var n = new Label(who); n.style.fontSize = 40; n.style.color = new Color(.45f, .27f, .14f); n.style.unityFontStyleAndWeight = FontStyle.Bold; head.Add(n);
            score = new Label("0点"); score.style.fontSize = 72; score.style.color = new Color(.71f, .27f, .18f); score.style.unityFontStyleAndWeight = FontStyle.Bold; head.Add(score);
            body = new Label(""); body.style.fontSize = 40; body.style.color = new Color(.2f, .14f, .1f); body.style.whiteSpace = WhiteSpace.Normal; body.style.marginTop = 10; c.Add(body);
            layer.Add(c); return c;
        }
    }
}
