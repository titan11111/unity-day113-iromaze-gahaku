// メニュー Day113/WebGLビルド：GitHub Pages で動く形（圧縮なし）で Days/day113/Builds/Web に書き出す。結果は Temp/d113_build_result.txt
using System; using System.IO; using UnityEditor; using UnityEditor.Build.Reporting; using UnityEngine;
namespace Day113.EditorTools
{
    public static class D113WebBuild
    {
        [MenuItem("Day113/WebGLビルド")]
        static void Build()
        {
            string id = "d113-" + DateTime.Now.ToString("yyyyMMdd-HHmm");
            File.WriteAllText("Assets/Resources/buildid.txt", id); AssetDatabase.ImportAsset("Assets/Resources/buildid.txt");
            PlayerSettings.productName = "113 いろまぜ画伯";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Pagesは.unityweb用のヘッダーを付けられない
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;
            var scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
            string outDir = Path.GetFullPath("../Builds/Web");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var rep = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = outDir, target = BuildTarget.WebGL, options = BuildOptions.None });
            var s = rep.summary;
            File.WriteAllText("Temp/d113_build_result.txt", $"{s.result}\nid={id}\nerrors={s.totalErrors}\nsize={s.totalSize}\ntime={s.totalTime}\n");
            Debug.Log("[D113] WebGL build " + s.result + " " + id);
        }
    }
}
