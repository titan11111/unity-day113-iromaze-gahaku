using System.Collections.Generic; using UnityEngine;
namespace Day113
{
    /// クリップの姿勢を当てる。焼いた姿勢データ（Resources/D113Poses）があればそれを使い、無いクリップだけ SampleAnimation に任せる。
    /// 当てる前に「最初の姿勢（休み）」へ戻すので、前の動き（札の腕・虫眼鏡）が残らない。
    public static class PoseLib
    {
        static Dictionary<string, PoseBank.Clip> map;
        static readonly Dictionary<int, Dictionary<string, Transform>> paths = new Dictionary<int, Dictionary<string, Transform>>();
        struct Rest { public Transform t; public Vector3 p, s; public Quaternion r; }
        static readonly Dictionary<int, Rest[]> rest = new Dictionary<int, Rest[]>();

        static void Load()
        {
            if (map != null) return;
            map = new Dictionary<string, PoseBank.Clip>();
            var bank = Resources.Load<PoseBank>("D113Poses");
            if (bank) foreach (var c in bank.clips) map[c.name] = c;
            else Debug.LogWarning("[D113] 姿勢データ D113Poses が無い（Day113/姿勢データを焼く）");
        }

        /// 子孫の今の姿勢を「休み」として覚える（最初に1回）
        public static void CaptureRest(GameObject root, bool force = false)
        {
            int id = root.GetInstanceID(); if (!force && rest.ContainsKey(id)) return;
            var ts = root.GetComponentsInChildren<Transform>(true); var list = new List<Rest>();
            foreach (var t in ts) if (t != root.transform) list.Add(new Rest { t = t, p = t.localPosition, r = t.localRotation, s = t.localScale });
            rest[id] = list.ToArray(); paths.Remove(id);
        }

        public static void Sample(AnimationClip c, GameObject root, float t, bool resetRest = true)
        {
            if (!c || !root) return;
            if (resetRest && rest.TryGetValue(root.GetInstanceID(), out var rs)) foreach (var x in rs) if (x.t) { x.t.localPosition = x.p; x.t.localRotation = x.r; x.t.localScale = x.s; }
            Load();
            if (!map.TryGetValue(c.name, out var b)) { c.SampleAnimation(root, t); return; }
            int f = Mathf.Clamp(Mathf.RoundToInt(t * b.fps), 0, b.frames - 1);
            var pm = PathMap(root);
            foreach (var tr in b.tracks)
            {
                if (!pm.TryGetValue(tr.path, out var x) || !x) continue;
                switch (tr.kind)
                {
                    case 0: x.localPosition = V3(tr.v, f); break;
                    case 2: x.localScale = V3(tr.v, f); break;
                    case 3: x.localEulerAngles = V3(tr.v, f); break;
                    default: { int i = f * 4; var q = new Quaternion(tr.v[i], tr.v[i + 1], tr.v[i + 2], tr.v[i + 3]); x.localRotation = q.normalized; break; }
                }
            }
        }

        static Vector3 V3(float[] v, int f) { int i = f * 3; return new Vector3(v[i], v[i + 1], v[i + 2]); }

        static Dictionary<string, Transform> PathMap(GameObject root)
        {
            int id = root.GetInstanceID();
            if (paths.TryGetValue(id, out var m)) return m;
            m = new Dictionary<string, Transform> { [""] = root.transform };
            void Walk(Transform t, string pre) { foreach (Transform ch in t) { var p = pre.Length == 0 ? ch.name : pre + "/" + ch.name; if (!m.ContainsKey(p)) m[p] = ch; Walk(ch, p); } }
            Walk(root.transform, ""); paths[id] = m; return m;
        }
    }
}
