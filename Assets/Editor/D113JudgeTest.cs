// メニュー Day113/審査テスト：再生して、テスト用の絵で「見せる」を押し、要所のスクショを Shots/judge_*.png に撮る
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace Day113.EditorTools
{
    static class D113JudgeTest
    {
        [MenuItem("Day113/審査テスト")]
        static void Run() { EditorSceneManager.SaveOpenScenes(); EditorSceneManager.OpenScene("Assets/Scenes/Atelier.unity"); EditorPrefs.SetBool("d113_judge_test", true); EditorApplication.isPlaying = true; }

        [MenuItem("Day113/審査テスト（高得点：大富豪と怪盗）")]
        static void RunHigh() { EditorPrefs.SetInt("d113_judge_test_total", 90); Run(); }
    }
}
