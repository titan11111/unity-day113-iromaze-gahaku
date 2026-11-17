using UnityEngine;
namespace Day113
{
    /// 画面の左下にビルドIDを薄く出す（公開版の取り違え防止）
    public class BuildStamp : MonoBehaviour
    {
        string id; GUIStyle st;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var t = Resources.Load<TextAsset>("buildid"); if (!t) return;
            var go = new GameObject("BuildStamp"); DontDestroyOnLoad(go); go.AddComponent<BuildStamp>().id = t.text.Trim();
        }
        void OnGUI()
        {
            if (st == null) { st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(10, Screen.height / 90) }; st.normal.textColor = new Color(1, 1, 1, .35f); }
            GUI.Label(new Rect(6, Screen.height - st.fontSize * 1.8f, 400, st.fontSize * 1.6f), id, st);
        }
    }
}
