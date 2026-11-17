// アトリエのシーンを開いてMain Cameraで撮る（メニュー Day113/アトリエを撮る）
using System.IO; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace Day113.EditorTools
{
    static class D113Shots
    {
        [MenuItem("Day113/アトリエを撮る")]
        static void Atelier()
        {
            var prev = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/Atelier.unity");
            var cam = Camera.main;
            var rt = new RenderTexture(1080, 1920, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt);
            File.WriteAllBytes("../Shots/stage3_room_cam.png", tex.EncodeToPNG());
            Debug.Log("[D113] Atelier shot saved");
            if (!string.IsNullOrEmpty(prev) && prev != "Assets/Scenes/Atelier.unity") EditorSceneManager.OpenScene(prev);
        }
    }
}
