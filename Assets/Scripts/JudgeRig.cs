using UnityEngine;
namespace Day113
{
    /// Galleryシーンの先生3人。クリップはEditorのセットアップで入る（0=老大家 1=評論家 2=虫眼鏡）。
    public class JudgeRig : MonoBehaviour
    {
        public Transform[] rigs = new Transform[3];
        public AnimationClip[] walk = new AnimationClip[3], stand = new AnimationClip[3], inspect = new AnimationClip[3], surprise = new AnimationClip[3];
        public Renderer painting;
        public Renderer[] mags; // 虫眼鏡（覗くときだけ表示）
        public Camera cam;
    }
}
