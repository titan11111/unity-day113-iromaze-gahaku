// メニュー Day113/画面チェック（スマホ縦・横）：スマホの縦(1080x2340)と横(2340x1080)で、ステージ選択・お絵かき・結果の3画面を撮る
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace Day113.EditorTools
{
    [InitializeOnLoad]
    static class D113FitCheck
    {
        static readonly (uint w, uint h, string tag)[] Sizes = { (1080, 2340, "portrait"), (2340, 1080, "landscape") };
        static D113FitCheck() { EditorApplication.playModeStateChanged += OnState; }

        [MenuItem("Day113/画面チェック（スマホ縦・横）")]
        static void Run() { EditorPrefs.SetInt("d113_fit_step", 0); Start(0); }

        static void Start(int i)
        {
            var sz = Sizes[i];
            PlayModeWindow.SetCustomRenderingResolution(sz.w, sz.h, "D113 " + sz.tag);
            EditorSceneManager.SaveOpenScenes(); EditorSceneManager.OpenScene("Assets/Scenes/Atelier.unity");
            EditorPrefs.SetString("d113_fit_tag", sz.tag);
            EditorApplication.isPlaying = true;
        }

        static void OnState(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredEditMode || !EditorPrefs.HasKey("d113_fit_step")) return;
            if (EditorPrefs.HasKey("d113_fit_tag")) return; // まだ撮影中に止まった
            int next = EditorPrefs.GetInt("d113_fit_step") + 1;
            if (next < Sizes.Length) { EditorPrefs.SetInt("d113_fit_step", next); Start(next); }
            else EditorPrefs.DeleteKey("d113_fit_step");
        }
    }
}
