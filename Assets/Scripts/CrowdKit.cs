using System.Collections.Generic; using UnityEngine;
namespace Day113
{
    /// 展示会の登場人物（来場者2種・大富豪・怪盗・スタッフ）の元データ。Editorのセットアップで入る。
    public class CrowdKit : MonoBehaviour
    {
        [System.Serializable] public class Kind { public string name; public GameObject template; public AnimationClip walk, stand, peer, clap, surprise, point, bow; }
        public Kind[] kinds;
        public Kind Get(string n) { foreach (var k in kinds) if (k.name == n) return k; return null; }
    }

    /// 1人の登場人物。包み（wrapper）の下にリグを置き、クリップは包みに当てる。
    public class Actor
    {
        public GameObject root; public Transform rig; public CrowdKit.Kind kind; Quaternion baseLocal; float phase;
        public Actor(CrowdKit.Kind k, Vector3 pos, Transform parent, Color tint)
        {
            kind = k; root = new GameObject(k.name + "_actor"); root.transform.SetParent(parent, false); root.transform.position = pos;
            var r = Object.Instantiate(k.template, root.transform); r.name = k.template.name; r.SetActive(true);
            rig = r.transform; baseLocal = k.template.transform.localRotation; rig.localPosition = Vector3.zero; rig.localRotation = baseLocal;
            foreach (var smr in r.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true; // 複製すると表示範囲が元の位置のまま残るため
            if (tint != Color.white)
            {
                var mpb = new MaterialPropertyBlock();
                foreach (var rr in r.GetComponentsInChildren<Renderer>()) { rr.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", tint); rr.SetPropertyBlock(mpb); }
            }
            Pose(k.stand, 0);
        }
        public void Pose(AnimationClip c, float t)
        {
            if (!c) return; c.SampleAnimation(root, t); rig.localPosition = Vector3.zero; rig.localRotation = baseLocal;
        }
        public void Face(Vector3 dir) { dir.y = 0; if (dir.sqrMagnitude > 1e-5f) root.transform.rotation = Quaternion.LookRotation(dir.normalized); }
        /// 1フレーム分だけ目的地へ歩く。着いたら true
        public bool StepTo(Vector3 target, float speed, float dt)
        {
            var p = root.transform.position; target.y = p.y; var d = target - p; float dist = d.magnitude;
            if (dist < .03f) { root.transform.position = target; Pose(kind.stand, 0); return true; }
            Face(d); float step = Mathf.Min(dist, speed * dt); root.transform.position = p + d / dist * step;
            phase += dt * speed * 1.6f; if (kind.walk) Pose(kind.walk, phase % kind.walk.length); return false;
        }
        public Vector3 Pos => root.transform.position;
        public void Destroy() { if (root) Object.Destroy(root); }
    }
}
