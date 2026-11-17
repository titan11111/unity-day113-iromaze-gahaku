using System.IO; using System.Linq; using UnityEditor; using UnityEngine;
namespace Day113.EditorTools
{
    static class D113ClipCheck
    {
        [MenuItem("Day113/クリップ確認")]
        static void Run()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in AssetDatabase.LoadAllAssetsAtPath("Assets/Models/D113_Teachers.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
            {
                var b = AnimationUtility.GetCurveBindings(c);
                sb.AppendLine($"{c.name} len={c.length} n={b.Length} :: " + string.Join(" | ", b.Select(x => x.path + "." + x.propertyName).Where(s => !s.Contains("/")).Take(8)) + " || sample: " + string.Join(" | ", b.Select(x => x.path).Distinct().Take(4)));
            }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/D113_Teachers.fbx");
            foreach (Transform t in go.transform) sb.AppendLine($"child {t.name} rot={t.localEulerAngles} children={t.childCount} first={(t.childCount>0?t.GetChild(0).name:"")}");
            File.WriteAllText("../Shots/clipcheck.txt", sb.ToString());
        }
    }
}
