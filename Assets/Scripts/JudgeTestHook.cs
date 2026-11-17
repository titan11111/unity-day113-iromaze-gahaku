#if UNITY_EDITOR
using System.Collections; using System.Reflection; using UnityEngine;
namespace Day113
{
    /// エディタ専用：メニュー「Day113/審査テスト」で再生したときだけ、自動で絵を描いて審査を流し、スクショを撮る。
    public class JudgeTestHook : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!UnityEditor.EditorPrefs.GetBool("d113_judge_test", false)) return;
            UnityEditor.EditorPrefs.SetBool("d113_judge_test", false);
            JudgeShow.TestTotal = UnityEditor.EditorPrefs.GetInt("d113_judge_test_total", -1); UnityEditor.EditorPrefs.DeleteKey("d113_judge_test_total");
            Application.runInBackground = true;
            new GameObject("JudgeTestHook").AddComponent<JudgeTestHook>();
        }
        IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f);
            var at = FindFirstObjectByType<Atelier>();
            var sel = typeof(Atelier).GetField("selPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            ((UnityEngine.UIElements.VisualElement)sel.GetValue(at))?.RemoveFromHierarchy(); sel.SetValue(at, null);
            var cv = at.Canvas; var counts = at.Counts;
            void Set(int r, int y, int b, int w, int k) { counts[0] = r; counts[1] = y; counts[2] = b; counts[3] = w; counts[4] = k; }
            void Path(float size, System.Func<float, Vector2> f, int steps) { cv.BeginStroke(counts, size, f(0), 1f); for (int i = 1; i <= steps; i++) cv.StrokeTo(f(i / (float)steps), 1f, .2f); cv.EndStroke(); }
            Set(0, 1, 1, 6, 0); for (float y = 262; y >= 104; y -= 16) { float yy = y; Path(28, t => new Vector2(-14 + t * 284, yy), 40); }
            Set(1, 1, 1, 1, 0); for (float y = 104; y >= -8; y -= 16) { float yy = y; Path(28, t => new Vector2(-14 + t * 284, yy), 40); }
            Set(6, 0, 0, 1, 0); for (int k = 0; k < 10; k++) { float a = k; Path(22, t => new Vector2(100 + Mathf.Cos(t * 6.28f) * (30 - a * 2.5f), 120 + Mathf.Sin(t * 6.28f) * (30 - a * 2.5f)), 30); }
            Set(0, 6, 0, 1, 0); for (int k = 0; k < 8; k++) { float a = k; Path(20, t => new Vector2(170 + Mathf.Cos(t * 6.28f) * (34 - a * 3f), 105 + Mathf.Sin(t * 6.28f) * (18 - a * 1.6f)), 30); }
            yield return null;
            JudgeShow.DebugShot = n => ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("../Shots/" + n + ".png"));
            at.Submit();
        }
    }
}
#endif
