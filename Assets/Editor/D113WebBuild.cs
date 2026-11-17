// メニュー Day113/WebGLビルド：GitHub Pages で動く形（圧縮なし）で Days/day113/Builds/Web に書き出す。結果は Temp/d113_build_result.txt
using System; using System.IO; using UnityEditor; using UnityEditor.Build.Reporting; using UnityEngine;
namespace Day113.EditorTools
{
    public static class D113WebBuild
    {
        [MenuItem("Day113/WebGLビルド")]
        public static void Build()
        {
            D113PoseBake.Bake(); // ビルドでも人物が動くよう、姿勢データを焼き直す
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
            if (s.result == BuildResult.Succeeded) PatchIndex(Path.Combine(outDir, "index.html"));
            File.WriteAllText("Temp/d113_build_result.txt", $"{s.result}\nid={id}\nerrors={s.totalErrors}\nsize={s.totalSize}\ntime={s.totalTime}\n");
            Debug.Log("[D113] WebGL build " + s.result + " " + id);
        }

        /// スマホは全画面・ピンチ/長押しメニュー無効・解像度の上限2倍。PCもウィンドウいっぱい
        static void PatchIndex(string path)
        {
            var h = File.ReadAllText(path);
            h = System.Text.RegularExpressions.Regex.Replace(h, "<title>.*?</title>", "<title>113 いろまぜ画伯</title>\n    <meta name=\"apple-mobile-web-app-capable\" content=\"yes\">\n    <style>html,body{margin:0;height:100%;overflow:hidden;background:#1a1210;-webkit-touch-callout:none;-webkit-user-select:none;user-select:none;touch-action:none}#unity-container.d113{position:fixed;inset:0;width:100%;height:100%;transform:none;left:0;top:0}#unity-canvas.d113{width:100%!important;height:100%!important}#unity-footer{display:none}</style>");
            h = h.Replace("// config.devicePixelRatio = 1;", "config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 2);");
            h = h.Replace("canvas.style.width = \"960px\";\n        canvas.style.height = \"600px\";", "document.querySelector(\"#unity-container\").className = \"d113\";\n        canvas.className = \"d113\";");
            File.WriteAllText(path, h);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), ".nojekyll"), "");
        }
    }
}
