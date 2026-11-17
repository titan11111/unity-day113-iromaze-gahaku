using System.IO; using System.Linq; using UnityEditor; using UnityEngine;
static class D113ModelList { [MenuItem("Day113/モデル確認")] static void Run() {
  var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/D113_Teachers.fbx");
  File.WriteAllText("../Shots/models.txt", string.Join("\n", go.GetComponentsInChildren<Renderer>(true).Select(r => r.name + " " + r.GetType().Name + " " + r.bounds.size + " mats=" + string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null"))))); } }
