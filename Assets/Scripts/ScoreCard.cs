using UnityEngine;
namespace Day113
{
    /// 先生の点数札：右腕を真上へ「ボン」と振り上げ、札は傾いて出てから揺れて止まる（Web版と同じ）。
    public class ScoreCard
    {
        GameObject root; Transform stick, board, rig, upper, fore, hand;
        public bool Ok => upper && fore && hand;

        public static ScoreCard Make(Transform rig, int score)
        {
            var c = new ScoreCard { rig = rig };
            c.upper = Find(rig, "upperarm"); c.fore = Find(rig, "forearm"); c.hand = Find(rig, "hand");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material Mat(Color col) { var m = new Material(lit); m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", .2f); return m; }
            c.root = new GameObject("ScoreCard"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(c.root, rig.gameObject.scene);
            GameObject Prim(PrimitiveType t, Transform parent, Vector3 lp, Vector3 ls, Material m)
            {
                var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
                g.transform.localPosition = lp; g.transform.localScale = ls; g.GetComponent<Renderer>().sharedMaterial = m; return g;
            }
            c.stick = Prim(PrimitiveType.Cylinder, c.root.transform, Vector3.zero, new Vector3(.02f, .16f, .02f), Mat(new Color(.36f, .22f, .12f))).transform;
            c.board = new GameObject("Board").transform; c.board.SetParent(c.root.transform, false);
            Prim(PrimitiveType.Cube, c.board, Vector3.zero, new Vector3(.4f, .27f, .012f), Mat(new Color(.75f, .22f, .17f)));
            Prim(PrimitiveType.Cube, c.board, Vector3.zero, new Vector3(.372f, .242f, .016f), Mat(new Color(.98f, .97f, .94f)));
            var tg = new GameObject("Num"); tg.transform.SetParent(c.board, false); tg.transform.localPosition = new Vector3(0, -.005f, -.0095f);
            var tm = tg.AddComponent<TextMesh>(); tm.text = score.ToString(); tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.fontSize = 120; tm.characterSize = .017f; tm.fontStyle = FontStyle.Bold;
            tm.color = score >= 70 ? new Color(.75f, .22f, .17f) : score >= 40 ? new Color(.17f, .14f, .13f) : new Color(.29f, .44f, .65f);
            tg.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            c.root.SetActive(false);
            return c;
        }

        static Transform Find(Transform r, string key)
        {
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains(key) && (n.EndsWith(".r") || n.EndsWith("_r") || n.EndsWith(" r") || n.EndsWith("right"))) return t;
            }
            return null;
        }

        /// u: 0=腕を下ろした姿勢 → 1=真上（1を超えると行き過ぎ）。tilt: 札の傾き（度）
        public void Apply(float u, float tiltDeg, Vector3 camPos)
        {
            if (!Ok) { root.SetActive(false); return; }
            Vector3 side = upper.position - rig.position; side.y = 0; side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
            Vector3 toCam = camPos - upper.position; toCam.y = 0; toCam = toCam.sqrMagnitude > 1e-6f ? toCam.normalized : Vector3.forward;
            Vector3 down = (fore.position - upper.position).normalized;
            Vector3 mid = (side * .75f + toCam * .65f).normalized;
            Vector3 up = (Vector3.up + side * .12f + toCam * .08f).normalized;
            Vector3 dir = u < .5f ? Vector3.Slerp(down, mid, u * 2f) : Vector3.SlerpUnclamped(mid, up, u * 2f - 1f);
            Aim(upper, fore, dir); Aim(fore, hand, dir);
            root.SetActive(u > .12f);
            Vector3 b = hand.position + dir * .03f;
            stick.SetPositionAndRotation(b + dir * .16f, Quaternion.FromToRotation(Vector3.up, dir));
            board.SetPositionAndRotation(b + dir * .4f, Quaternion.LookRotation(-toCam) * Quaternion.Euler(0, 0, tiltDeg));
        }

        static void Aim(Transform a, Transform b, Vector3 dir)
        {
            var cur = b.position - a.position; if (cur.sqrMagnitude < 1e-8f) return;
            a.rotation = Quaternion.FromToRotation(cur.normalized, dir) * a.rotation;
        }

        public void Destroy() { if (root) Object.Destroy(root); }

        /// 振り上げの動き：0.25秒で行き過ぎてから戻る
        public static float RaiseCurve(float t)
        {
            float x = Mathf.Clamp01(t / .25f), c1 = 2.2f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
        /// 札の傾き：25°で出て、揺れながら0°へ
        public static float TiltCurve(float t) => 25f * Mathf.Exp(-5f * t) * Mathf.Cos(13f * t);
    }
}
