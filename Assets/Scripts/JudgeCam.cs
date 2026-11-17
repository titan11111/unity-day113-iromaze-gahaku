using System; using UnityEngine;
namespace Day113
{
    /// 審査のカメラ（Web版と同じ動き）：目標の位置と注視点へ、指数でなめらかに寄る。follow を入れると毎フレーム追いかける。
    public class JudgeCam : MonoBehaviour
    {
        public Vector3 pos, tgt, dPos, dTgt; public float k = 1.6f;
        public Func<(Vector3 p, Vector3 t, float k)> follow;
        Camera cam; float baseFov;
        public static bool Portrait => Screen.height >= Screen.width;
        void Awake() { cam = GetComponent<Camera>(); baseFov = cam.fieldOfView; pos = dPos = transform.position; tgt = dTgt = pos + transform.forward * 3f; }
        public void Shot(Vector3 p, Vector3 t, float kk, bool snap = false)
        {
            follow = null; dPos = p; dTgt = t; k = kk;
            if (snap) { pos = p; tgt = t; Apply(); }
        }
        void LateUpdate()
        {
            if (follow != null) { var f = follow(); dPos = f.p; dTgt = f.t; k = f.k; }
            float a = 1f - Mathf.Exp(-Time.deltaTime * k);
            pos += (dPos - pos) * a; tgt += (dTgt - tgt) * a; Apply();
        }
        void Apply() { transform.SetPositionAndRotation(pos, Quaternion.LookRotation(tgt - pos)); cam.fieldOfView = Portrait ? 60f : 44f; }
        void OnDestroy() { if (cam) cam.fieldOfView = baseFov; }
    }
}
