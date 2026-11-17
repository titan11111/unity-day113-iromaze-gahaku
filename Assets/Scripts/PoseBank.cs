using System.Collections.Generic; using UnityEngine;
namespace Day113
{
    /// 人物の動き（クリップ）をEditorで焼いた姿勢データ。WebGLなどのビルドでは AnimationClip.SampleAnimation が汎用クリップに効かないため、これで再生する。
    /// 作り方：メニュー Day113/姿勢データを焼く（WebGLビルドの前にも自動で焼く）
    public class PoseBank : ScriptableObject
    {
        [System.Serializable] public class Track { public string path; public int kind; public float[] v; } // kind 0=位置 1=回転(四元数) 2=拡大 3=回転(オイラー)
        [System.Serializable] public class Clip { public string name; public float length; public float fps; public int frames; public List<Track> tracks = new List<Track>(); }
        public List<Clip> clips = new List<Clip>();
    }
}
