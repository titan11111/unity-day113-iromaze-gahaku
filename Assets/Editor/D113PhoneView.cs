using UnityEditor;
static class D113PhoneView {
  [MenuItem("Day113/スマホ縦で遊ぶ")] static void P() { PlayModeWindow.SetCustomRenderingResolution(1080, 2340, "D113 portrait"); EditorApplication.isPlaying = true; }
  [MenuItem("Day113/スマホ横で遊ぶ")] static void L() { PlayModeWindow.SetCustomRenderingResolution(2340, 1080, "D113 landscape"); EditorApplication.isPlaying = true; }
}
