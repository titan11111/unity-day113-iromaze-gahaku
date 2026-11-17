// 段階4：ギャラリーシーンを組み立てる（一度だけ自動実行。メニュー Day113/ギャラリーを組み直す でも実行可）
using System.IO; using System.Linq;
using UnityEditor; using UnityEditor.SceneManagement;
using UnityEngine; using UnityEngine.Rendering;

namespace Day113.EditorTools
{
    static class D113GallerySetup
    {
        const string Marker = "Temp/d113_gallery_setup_done";
        const string ScenePath = "Assets/Scenes/Gallery.unity";
        const string Shot = "../Shots/stage4_gallery.png";

        [InitializeOnLoadMethod]
        static void Auto()
        {
            if (File.Exists(Marker)) return;
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) Run(); };
        }

        [MenuItem("Day113/ギャラリーを組み直す")]
        static void Run()
        {
            try
            {
                AssetDatabase.Refresh();
                Fix("Assets/Models/D113_Gallery.fbx", false);
                Fix("Assets/Models/D113_Teachers.fbx", true);
                RemapTeacherMaterials();
                Fix("Assets/Models/D113_Crowd.fbx", true);
                RemapCrowdMaterials();
                var prev = EditorSceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(prev)) EditorSceneManager.SaveOpenScenes();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/D113_Gallery.fbx"));
                room.name = "GalleryRoom";

                var tPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/D113_Teachers.fbx");
                var teachers = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab);
                teachers.name = "Teachers"; teachers.transform.position = new Vector3(0, 0, -1.4f);
                var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/D113_Teachers.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
                AnimationClip Clip(string rig, string act) => clips.FirstOrDefault(c => c.name == rig + "|" + act) ?? clips.FirstOrDefault(c => c.name.EndsWith(act) && c.name.Contains(rig));
                var jr = teachers.AddComponent<Day113.JudgeRig>();
                string[] rigN = { "Teacher_Elder", "Teacher_Critic", "Teacher_Loupe" }, key = { "Elder", "Critic", "Loupe" }, insp = { "Elder_Inspect", "Critic_Inspect", "Loupe_Inspect" };
                for (int i = 0; i < 3; i++)
                {
                    jr.rigs[i] = teachers.transform.Find(rigN[i]);
                    jr.walk[i] = Clip(rigN[i], key[i] + "_Walk"); jr.surprise[i] = Clip(rigN[i], key[i] + "_Surprise"); jr.stand[i] = Clip(rigN[i], key[i] + "_Stand"); jr.inspect[i] = Clip(rigN[i], insp[i]);
                    jr.stand[i]?.SampleAnimation(teachers, 0f);
                }
                jr.painting = room.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "PlayerPainting");
                jr.mags = teachers.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains("_mag_")).ToArray();
                foreach (var m in jr.mags) m.enabled = false;

                // 展示会の登場人物（ひな形は非表示で置いておき、実行時に複製する）
                var cPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/D113_Crowd.fbx");
                if (cPrefab != null)
                {
                    var crowd = (GameObject)PrefabUtility.InstantiatePrefab(cPrefab); crowd.name = "CrowdTemplates";
                    PrefabUtility.UnpackPrefabInstance(crowd, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    // FBXの取り込みで体（SkinnedMesh）が一番上に出るので、各人の骨組みの中へ戻す（1人ずつ複製できるように）
                    foreach (var n in new[] { "Visitor_M", "Visitor_F", "Rich", "Thief", "Staff" })
                    {
                        var rig = crowd.transform.Find(n); if (rig == null) continue;
                        foreach (var smr in crowd.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            if (smr.transform.parent == crowd.transform && smr.name.StartsWith(n + "_")) smr.transform.SetParent(rig, true);
                    }
                    crowd.SetActive(false);
                    var cclips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/D113_Crowd.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
                    AnimationClip CC(string n, string a) => cclips.FirstOrDefault(c => c.name == n + "|" + n + "_" + a);
                    var kitGo = new GameObject("CrowdKit"); var kit = kitGo.AddComponent<Day113.CrowdKit>();
                    kit.kinds = new[] { "Visitor_M", "Visitor_F", "Rich", "Thief", "Staff" }.Select(n => new Day113.CrowdKit.Kind {
                        name = n, template = crowd.transform.Find(n)?.gameObject,
                        walk = CC(n, "Walk"), stand = CC(n, "Stand"), peer = CC(n, "Peer"), clap = CC(n, "Clap"), surprise = CC(n, "Surprise"), point = CC(n, "Point"), bow = CC(n, "Bow") }).ToArray();
                    Debug.Log("[D113] crowd kinds: " + string.Join(",", kit.kinds.Select(k => k.name + ":" + (k.template != null) + "/" + (k.walk != null))));
                }

                var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 1.55f, 3.6f); cam.tag = "Untagged"; cam.transform.rotation = Quaternion.Euler(4, 180, 0);
                cam.fieldOfView = 64; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.1f, .06f, .05f);
                jr.cam = cam;

                var spot = new GameObject("PaintSpot").AddComponent<Light>(); spot.type = LightType.Spot; spot.color = new Color(1f, .86f, .66f);
                spot.intensity = 4.5f; spot.range = 5; spot.spotAngle = 48; spot.shadows = LightShadows.Soft;
                spot.transform.position = new Vector3(0, 3.25f, -2.6f); spot.transform.LookAt(new Vector3(0, 1.75f, -3.45f));
                foreach (var z in new[] { -2f, 0.5f, 2.5f })
                {
                    var p = new GameObject("CeilingWarm").AddComponent<Light>(); p.type = LightType.Point; p.color = new Color(1f, .78f, .52f);
                    p.intensity = 0.9f; p.range = 4.5f; p.transform.position = new Vector3(0, 3.2f, z);
                }
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.38f, .26f, .2f);

                EditorSceneManager.SaveScene(scene, ScenePath);
                var list = EditorBuildSettings.scenes.ToList();
                if (!list.Any(s => s.path == ScenePath)) { list.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = list.ToArray(); }

                var rt = new RenderTexture(1080, 1920, 24); cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); tex.Apply();
                RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt);
                File.WriteAllBytes(Shot, tex.EncodeToPNG());

                cam.enabled = false; EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
                File.WriteAllText(Marker, System.DateTime.Now.ToString("s"));
                Debug.Log($"[D113] Gallery scene built: walk={jr.walk[0]?.name},{jr.walk[1]?.name},{jr.walk[2]?.name} painting={(jr.painting!=null)} clips={clips.Count} shot={Path.GetFullPath(Shot)}");
                EditorSceneManager.OpenScene("Assets/Scenes/Atelier.unity");
            }
            catch (System.Exception e) { Debug.LogError("[D113] Gallery setup failed: " + e); File.WriteAllText(Marker + "_error", e.ToString()); }
        }

        static void RemapTeacherMaterials()
        {
            const string fbx = "Assets/Models/D113_Teachers.fbx";
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter; if (mi == null) return;
            System.IO.Directory.CreateDirectory("Assets/Materials/Teachers");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var who in new[] { "Elder", "Critic", "Loupe" })
            foreach (var kind in new[] { "Body", "Face" })
            {
                var texPath = kind == "Face" ? $"Assets/Textures/Teachers/face_{who.ToLower()}_hc.png" : $"Assets/Textures/Teachers/{kind.ToLower()}_{who.ToLower()}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                var matPath = $"Assets/Materials/Teachers/D113P_{kind}_{who}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, matPath); }
                mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(mat);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), $"D113P_{kind}_{who}"), mat);
            }
            AssetDatabase.SaveAssets(); mi.SaveAndReimport();
        }

        static void RemapCrowdMaterials()
        {
            const string fbx = "Assets/Models/D113_Crowd.fbx";
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter; if (mi == null) return;
            System.IO.Directory.CreateDirectory("Assets/Materials/Crowd");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var key in new[] { "visitor_m", "visitor_f", "rich", "thief", "staff" })
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/Crowd/body_{key}.png");
                var matPath = $"Assets/Materials/Crowd/D113C_Body_{key}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, matPath); }
                mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.15f); EditorUtility.SetDirty(mat);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), $"D113C_Body_{key}"), mat);
            }
            AssetDatabase.SaveAssets(); mi.SaveAndReimport();
        }

        static void Fix(string path, bool anim)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter; if (mi == null) { Debug.LogError("[D113] missing " + path); return; }
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.importCameras = false; mi.importLights = false;
            mi.animationType = anim ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None; mi.importAnimation = anim;
            mi.SaveAndReimport();
        }
    }
}
