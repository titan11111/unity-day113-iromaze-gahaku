using System; using System.Collections; using System.Collections.Generic;
using UnityEngine; using UnityEngine.UIElements;
namespace Day113
{
    /// 展示会：来場者が扉から入り、行列→鑑賞枠5つ→反応→退場。総合70以上で大富豪、80以上で怪盗（大富豪のあと）。
    public static class Exhibit
    {
        public class Result { public int visitors, heart, think, sweat; public string rich, thief; }

        static readonly Vector3 Door = new Vector3(-2.25f, 0, -2.45f);
        static readonly Vector3 DoorIn = new Vector3(-1.85f, 0, -1.95f);
        static Vector3 Slot(int k) => new Vector3(-.85f + k * .42f, 0, -1.95f);
        static Vector3 QueuePos(int n) => new Vector3(-.95f, 0, -1.25f + n * .55f);
        static readonly Vector3 Art = new Vector3(0, 0, -3.4f);

        public static IEnumerator Run(JudgeRig jr, CrowdKit kit, GradeResult g, VisualElement layer, Camera cam, Light[] lights, Func<bool> tapped, Result res, Action<string> shot)
        {
            var parent = new GameObject("Exhibit").transform; parent.SetParent(jr.transform.parent, false);
            float y = jr.rigs[1].position.y;
            Vector3 Y(Vector3 v) { v.y = y; return v; }
            var all = new List<Actor>();
            var rnd = new System.Random(g.total * 31 + 7);

            // カメラ：入口側から全体
            var ct = cam.transform; ct.position = new Vector3(1.1f, 2.5f, 2.6f); ct.rotation = Quaternion.LookRotation(new Vector3(-.4f, 1.0f, -2.2f) - ct.position);
            var hud = new Label(); hud.style.position = Position.Absolute; hud.style.top = 30; hud.style.left = 30; hud.style.right = 30;
            hud.style.fontSize = 40; hud.style.color = new Color(1f, .95f, .82f); hud.style.backgroundColor = new Color(0, 0, 0, .5f);
            hud.style.paddingLeft = hud.style.paddingRight = 24; hud.style.paddingTop = hud.style.paddingBottom = 12; hud.style.unityTextAlign = TextAnchor.MiddleCenter;
            layer.Add(hud);
            void Hud() => hud.text = $"展示会　来場 {res.visitors}人　♥ {res.heart}　？ {res.think}　汗 {res.sweat}";
            Hud();
            var open = JudgeShowUI.Banner(layer, "展示会スタート！", 70); yield return Wait(1.3f); open.RemoveFromHierarchy();

            // スタッフ
            Actor staff = null; var sk = kit.Get("Staff");
            if (sk != null) { staff = new Actor(sk, Y(new Vector3(1.05f, 0, -2.6f)), parent, Color.white); staff.Face(Vector3.forward); all.Add(staff); }

            int N = Mathf.RoundToInt(g.total * .3f) + 4; float interval = 1f / (.35f + g.total / 100f * 1.25f);
            var kinds = new[] { kit.Get("Visitor_M"), kit.Get("Visitor_F") };
            Color[] tints = { Color.white, new Color(.85f, .92f, 1f), new Color(1f, .9f, .85f), new Color(.92f, 1f, .9f), new Color(1f, 1f, .85f) };
            int[] judgeScore = { g.color, g.shape, g.feel };

            var queue = new List<Actor>(); var slots = new Actor[5]; var state = new Dictionary<Actor, (int st, float t, int slot)>();
            var lastSign = new Label("最後尾"); lastSign.style.position = Position.Absolute; lastSign.style.fontSize = 30; lastSign.style.color = Color.white;
            lastSign.style.backgroundColor = new Color(.55f, .12f, .1f, .9f); lastSign.style.paddingLeft = lastSign.style.paddingRight = 10; layer.Add(lastSign);
            var bubbles = new List<(Label l, Actor a, float until)>();

            int spawned = 0; float spawnT = 0, t = 0; bool richDone = g.total < 70, thiefDone = g.total < 80, richStarted = false, thiefStarted = false;
            IEnumerator richCo = null, thiefCo = null; bool shotMid = false;
            while (true)
            {
                float dt = Time.deltaTime; t += dt; spawnT -= dt;
                if (spawned < N && spawnT <= 0 && queue.Count < 7)
                {
                    var k = kinds[spawned % 2]; if (k == null) k = kinds[0];
                    var a = new Actor(k, Y(Door), parent, tints[rnd.Next(tints.Length)]); all.Add(a); queue.Add(a); state[a] = (0, 0, -1);
                    spawned++; res.visitors = spawned; spawnT = interval; Hud();
                }
                // 行列：先頭から順に詰める。空いた鑑賞枠へ
                for (int i = 0; i < queue.Count; i++)
                {
                    var a = queue[i]; var s = state[a];
                    if (i == 0)
                    {
                        int free = Array.IndexOf(slots, null);
                        if (free >= 0 && Vector3.Distance(a.Pos, Y(QueuePos(0))) < .2f) { slots[free] = a; queue.RemoveAt(i); state[a] = (1, 0, free); i--; continue; }
                    }
                    var target = Y(QueuePos(i));
                    if (Vector3.Distance(a.Pos, Y(Door)) < .4f && s.st == 0) a.StepTo(Y(DoorIn), 1.1f, dt);
                    else if (!a.StepTo(target, 1.0f, dt)) { } else a.Face(Vector3.back);
                }
                // 鑑賞→反応→退場
                for (int k = 0; k < 5; k++)
                {
                    var a = slots[k]; if (a == null) continue; var s = state[a];
                    if (s.st == 1) { if (a.StepTo(Y(Slot(k)), 1.0f, dt)) { a.Face(Vector3.back); a.Pose(a.kind.peer, 0); state[a] = (2, 0, k); } }
                    else if (s.st == 2)
                    {
                        s.t += dt; state[a] = s;
                        if (s.t > 2.2f)
                        {
                            int view = rnd.Next(3); bool heart = rnd.NextDouble() < judgeScore[view] / 100.0; string mark; Color col;
                            if (heart) { res.heart++; mark = "♥"; col = new Color(.95f, .3f, .45f); a.Pose(a.kind.clap, 0); }
                            else if (rnd.Next(2) == 0) { res.think++; mark = "？"; col = new Color(.45f, .5f, .6f); a.Pose(a.kind.stand, 0); }
                            else { res.sweat++; mark = "汗"; col = new Color(.3f, .55f, .85f); a.Pose(a.kind.stand, 0); }
                            var l = new Label(mark); l.style.position = Position.Absolute; l.style.fontSize = 46; l.style.color = Color.white; l.style.backgroundColor = col;
                            l.style.paddingLeft = l.style.paddingRight = 12; l.style.borderTopLeftRadius = l.style.borderTopRightRadius = l.style.borderBottomLeftRadius = l.style.borderBottomRightRadius = 20;
                            layer.Add(l); bubbles.Add((l, a, t + 1.4f)); Hud();
                            state[a] = (3, 0, k);
                        }
                    }
                    else if (s.st == 3) { s.t += dt; state[a] = s; if (s.t > 1.2f) { slots[k] = null; state[a] = (4, 0, -1); } }
                }
                // 退場
                foreach (var a in new List<Actor>(state.Keys))
                {
                    var s = state[a];
                    if (s.st == 4) { if (a.StepTo(Y(DoorIn + new Vector3(.25f, 0, .1f)), 1.1f, dt)) state[a] = (5, 0, -1); }
                    else if (s.st == 5) { if (a.StepTo(Y(Door), 1.1f, dt)) { state.Remove(a); all.Remove(a); a.Destroy(); } }
                }
                // 吹き出しと最後尾札
                for (int i = bubbles.Count - 1; i >= 0; i--)
                {
                    var b = bubbles[i]; if (t > b.until || b.a.root == null) { b.l.RemoveFromHierarchy(); bubbles.RemoveAt(i); continue; }
                    Place(b.l, b.a.Pos + Vector3.up * 2.05f, cam);
                }
                if (queue.Count > 1) { lastSign.style.display = DisplayStyle.Flex; Place(lastSign, queue[queue.Count - 1].Pos + Vector3.up * 2.0f, cam); } else lastSign.style.display = DisplayStyle.None;

                if (!shotMid && t > 7f) { shotMid = true; shot?.Invoke("exhibit_crowd"); }
                // 大富豪（開幕9秒後）→ 怪盗
                if (!richDone && !richStarted && t > 9f) { richStarted = true; richCo = Rich(kit, g, parent, staff, layer, cam, y, shot); }
                if (richCo != null && !richCo.MoveNext()) { richCo = null; richDone = true; }
                if (richDone && !thiefDone && !thiefStarted && t > 12f) { thiefStarted = true; thiefCo = Thief(kit, jr, parent, staff, all, layer, cam, lights, y, shot); }
                if (thiefCo != null && !thiefCo.MoveNext()) { thiefCo = null; thiefDone = true; }

                if (spawned >= N && state.Count == 0 && richDone && thiefDone && richCo == null && thiefCo == null) break;
                if (tapped() && spawned >= N && richDone && thiefDone) break;
                yield return null;
            }
            res.rich = g.total >= 70 ? "大富豪が " + Price(g.total) + " でお買い上げ" : null;
            res.thief = g.total >= 80 ? "怪盗があらわれた！（絵は無事）" : null;
            foreach (var b in bubbles) b.l.RemoveFromHierarchy();
            lastSign.RemoveFromHierarchy();
            string crowd = res.visitors >= 30 ? "大盛況！" : res.visitors >= 20 ? "にぎわった！" : res.visitors >= 12 ? "まずまずの入り" : "静かな展示会";
            var fin = JudgeShowUI.Banner(layer, $"閉幕　来場 {res.visitors}人　♥ {res.heart}\n{crowd}", 60);
            shot?.Invoke("exhibit_close");
            float w = 0; while (w < 2.4f && !tapped()) { w += Time.deltaTime; yield return null; }
            fin.RemoveFromHierarchy(); hud.RemoveFromHierarchy();
            foreach (var a in all) a.Destroy();
            UnityEngine.Object.Destroy(parent.gameObject);
        }

        public static string Price(int tt) => tt >= 95 ? "5000万円" : tt >= 88 ? "1000万円" : tt >= 80 ? "300万円" : "50万円";

        static IEnumerator Rich(CrowdKit kit, GradeResult g, Transform parent, Actor staff, VisualElement layer, Camera cam, float y, Action<string> shot)
        {
            var k = kit.Get("Rich"); if (k == null) yield break;
            var a = new Actor(k, new Vector3(Door.x, y, Door.z), parent, Color.white);
            var stop = new Vector3(.75f, y, -1.75f);
            while (!a.StepTo(new Vector3(DoorIn.x, y, DoorIn.z + .4f), .9f, Time.deltaTime)) yield return null;
            while (!a.StepTo(stop, .9f, Time.deltaTime)) yield return null;
            a.Face(Vector3.back); a.Pose(k.point, 0);
            var b1 = Say(layer, "大富豪", "これは…買えるのかね？"); shot?.Invoke("exhibit_rich");
            for (float t = 0; t < 2.2f; t += Time.deltaTime) yield return null;
            b1.RemoveFromHierarchy();
            if (staff != null) { staff.Face(a.Pos - staff.Pos); staff.Pose(staff.kind.bow, 0); }
            var b2 = Say(layer, "スタッフ", $"…{Price(g.total)}でいかがでしょう！");
            for (float t = 0; t < 2.0f; t += Time.deltaTime) yield return null;
            b2.RemoveFromHierarchy();
            a.Pose(k.clap, 0); var b3 = JudgeShowUI.Banner(layer, $"お買い上げ！ {Price(g.total)}", 64);
            for (float t = 0; t < 1.6f; t += Time.deltaTime) yield return null;
            b3.RemoveFromHierarchy(); if (staff != null) { staff.Pose(staff.kind.stand, 0); staff.Face(Vector3.forward); }
            while (!a.StepTo(new Vector3(DoorIn.x + .3f, y, DoorIn.z + .4f), 1.0f, Time.deltaTime)) yield return null;
            while (!a.StepTo(new Vector3(Door.x, y, Door.z), 1.0f, Time.deltaTime)) yield return null;
            a.Destroy();
        }

        static IEnumerator Thief(CrowdKit kit, JudgeRig jr, Transform parent, Actor staff, List<Actor> crowd, VisualElement layer, Camera cam, Light[] lights, float y, Action<string> shot)
        {
            var k = kit.Get("Thief"); if (k == null) yield break;
            var keep = new float[lights.Length]; for (int i = 0; i < lights.Length; i++) { keep[i] = lights[i].intensity; lights[i].intensity *= .08f; }
            var amb = RenderSettings.ambientLight; RenderSettings.ambientLight = amb * .25f;
            var dark = JudgeShowUI.Banner(layer, "停電！？", 80); dark.style.color = new Color(1f, .4f, .35f);
            for (float t = 0; t < 1.2f; t += Time.deltaTime) yield return null;
            dark.RemoveFromHierarchy();
            var a = new Actor(k, new Vector3(0, y, -2.45f), parent, Color.white); a.Face(Vector3.forward); a.Pose(k.bow, 0);
            var spot = new GameObject("ThiefSpot").AddComponent<Light>(); spot.type = LightType.Spot; spot.intensity = 6; spot.range = 6; spot.spotAngle = 30; spot.color = new Color(.85f, .9f, 1f);
            spot.transform.position = new Vector3(0, 3.3f, -1.2f); spot.transform.LookAt(a.Pos + Vector3.up);
            var b = Say(layer, "怪盗", "今宵、この名画…しかと見届けた！"); shot?.Invoke("exhibit_thief");
            foreach (var c in crowd) c.Pose(c.kind.surprise, 0);
            for (int i = 0; i < 3; i++) JudgeShow.PoseSurprise(jr, i);
            for (float t = 0; t < 2.4f; t += Time.deltaTime) yield return null;
            b.RemoveFromHierarchy();
            while (!a.StepTo(new Vector3(DoorIn.x, y, DoorIn.z), 2.6f, Time.deltaTime)) yield return null;
            while (!a.StepTo(new Vector3(Door.x, y, Door.z), 2.6f, Time.deltaTime)) yield return null;
            a.Destroy(); UnityEngine.Object.Destroy(spot.gameObject);
            for (int i = 0; i < lights.Length; i++) lights[i].intensity = keep[i]; RenderSettings.ambientLight = amb;
            foreach (var c in crowd) c.Pose(c.kind.stand, 0);
            for (int i = 0; i < 3; i++) JudgeShow.PoseStand(jr, i);
            var ok = JudgeShowUI.Banner(layer, "怪盗は消えた…絵は無事！", 60);
            for (float t = 0; t < 1.6f; t += Time.deltaTime) yield return null;
            ok.RemoveFromHierarchy();
        }

        static VisualElement Say(VisualElement layer, string who, string text)
        {
            var c = JudgeShowUI.Bubble(layer, who, text, out var score, out var body); score.text = ""; body.text = text; return c;
        }

        static void Place(VisualElement l, Vector3 world, Camera cam)
        {
            if (l.panel == null) return;
            var sp = cam.WorldToScreenPoint(world); if (sp.z < 0) { l.style.display = DisplayStyle.None; return; }
            var p = RuntimePanelUtils.ScreenToPanel(l.panel, new Vector2(sp.x, Screen.height - sp.y));
            l.style.display = DisplayStyle.Flex; l.style.left = p.x - 30; l.style.top = p.y - 30;
        }

        static IEnumerator Wait(float s) { for (float t = 0; t < s; t += Time.deltaTime) yield return null; }
    }
}
