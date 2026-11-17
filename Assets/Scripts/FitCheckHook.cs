#if UNITY_EDITOR
using System.Collections; using System.Reflection; using UnityEngine;
namespace Day113
{
    /// エディタ専用：画面チェックで再生したとき、3つの画面を撮って再生を止める
    public class FitCheckHook : MonoBehaviour
    {
        string tag;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var t = UnityEditor.EditorPrefs.GetString("d113_fit_tag", ""); if (t == "") return;
            Application.runInBackground = true;
            new GameObject("FitCheckHook").AddComponent<FitCheckHook>().tag = t;
        }
        void Shot(string n) => ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath($"../Shots/fit_{tag}_{n}.png"));
        IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f); Shot("1select"); yield return new WaitForSeconds(.6f);
            var at = FindFirstObjectByType<Atelier>();
            var sel = typeof(Atelier).GetField("selPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            ((UnityEngine.UIElements.VisualElement)sel.GetValue(at))?.RemoveFromHierarchy(); sel.SetValue(at, null);
            var cv = at.Canvas; var counts = at.Counts;
            void Set(int r, int y, int b, int w, int k) { counts[0] = r; counts[1] = y; counts[2] = b; counts[3] = w; counts[4] = k; }
            void Path(float size, System.Func<float, Vector2> f, int steps) { cv.BeginStroke(counts, size, f(0), 1f); for (int i = 1; i <= steps; i++) cv.StrokeTo(f(i / (float)steps), 1f, .2f); cv.EndStroke(); }
            Set(0, 1, 1, 6, 0); for (float y = 262; y >= 104; y -= 16) { float yy = y; Path(28, t => new Vector2(-14 + t * 284, yy), 40); }
            Set(1, 1, 1, 1, 0); for (float y = 104; y >= -8; y -= 16) { float yy = y; Path(28, t => new Vector2(-14 + t * 284, yy), 40); }
            Set(6, 0, 0, 1, 0); for (int k = 0; k < 10; k++) { float a = k; Path(22, t => new Vector2(100 + Mathf.Cos(t * 6.28f) * (30 - a * 2.5f), 120 + Mathf.Sin(t * 6.28f) * (30 - a * 2.5f)), 30); }
            yield return new WaitForSeconds(.8f); Shot("2atelier"); yield return new WaitForSeconds(.6f);
            JudgeShow.SkipShow = true; at.Submit();
            yield return new WaitForSeconds(2.5f); Shot("3result"); yield return new WaitForSeconds(.8f);
            JudgeShow.SkipShow = false;
            UnityEditor.EditorPrefs.DeleteKey("d113_fit_tag");
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
