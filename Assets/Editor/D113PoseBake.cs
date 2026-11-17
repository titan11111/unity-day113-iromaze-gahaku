// メニュー Day113/姿勢データを焼く：先生と来場者のFBXの全クリップを30コマ/秒で焼いて Resources/D113Poses.asset に保存する
using System.Collections.Generic; using UnityEditor; using UnityEngine;
namespace Day113.EditorTools
{
    public static class D113PoseBake
    {
        static readonly string[] Fbx = { "Assets/Models/D113_Teachers.fbx", "Assets/Models/D113_Crowd.fbx" };
        const string Out = "Assets/Resources/D113Poses.asset";

        [MenuItem("Day113/姿勢データを焼く")]
        public static void Bake()
        {
            var bank = ScriptableObject.CreateInstance<PoseBank>(); var seen = new HashSet<string>(); int nTracks = 0;
            foreach (var path in Fbx)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(o is AnimationClip clip) || clip.name.StartsWith("__preview__")) continue;
                    if (!seen.Add(clip.name)) { Debug.LogWarning("[D113] 同じ名前のクリップ " + clip.name + "（後の方は無視）"); continue; }
                    float fps = 30f; int frames = Mathf.Max(1, Mathf.CeilToInt(clip.length * fps) + 1);
                    var bc = new PoseBank.Clip { name = clip.name, length = clip.length, fps = fps, frames = frames };
                    var groups = new Dictionary<(string, int), Dictionary<string, AnimationCurve>>();
                    foreach (var b in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (b.type != typeof(Transform)) continue;
                        string pn = b.propertyName; int dot = pn.LastIndexOf('.'); if (dot < 0) continue;
                        string head = pn.Substring(0, dot), comp = pn.Substring(dot + 1);
                        int kind = head == "m_LocalPosition" ? 0 : head == "m_LocalRotation" ? 1 : head == "m_LocalScale" ? 2 : head.StartsWith("localEulerAngles") ? 3 : -1;
                        if (kind < 0) continue;
                        var key = (b.path, kind); if (!groups.TryGetValue(key, out var g)) groups[key] = g = new Dictionary<string, AnimationCurve>();
                        g[comp] = AnimationUtility.GetEditorCurve(clip, b);
                    }
                    foreach (var kv in groups)
                    {
                        int kind = kv.Key.Item2; string[] comps = kind == 1 ? new[] { "x", "y", "z", "w" } : new[] { "x", "y", "z" };
                        float[] def = kind == 1 ? new[] { 0f, 0f, 0f, 1f } : kind == 2 ? new[] { 1f, 1f, 1f } : new[] { 0f, 0f, 0f };
                        var v = new float[frames * comps.Length];
                        for (int f = 0; f < frames; f++)
                        {
                            float t = Mathf.Min(clip.length, f / fps);
                            for (int k = 0; k < comps.Length; k++) v[f * comps.Length + k] = kv.Value.TryGetValue(comps[k], out var cu) ? cu.Evaluate(t) : def[k];
                        }
                        bc.tracks.Add(new PoseBank.Track { path = kv.Key.Item1, kind = kind, v = v }); nTracks++;
                    }
                    bank.clips.Add(bc);
                }
            AssetDatabase.DeleteAsset(Out); AssetDatabase.CreateAsset(bank, Out); AssetDatabase.SaveAssets();
            Debug.Log($"[D113] 姿勢データを焼いた：クリップ{bank.clips.Count}本・トラック{nTracks}本 → {Out}");
            System.IO.File.WriteAllText("Temp/d113_posebake.txt", $"clips={bank.clips.Count}\ntracks={nTracks}\n");
        }
    }
}
