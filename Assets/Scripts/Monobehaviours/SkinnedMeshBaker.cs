using UnityEditor;
using UnityEngine;

/// <summary>
/// Used to bake a skinned mesh renderer into a gameobject
/// </summary>
public class SkinnedMeshBaker {
    [MenuItem("Tools/Bake Selected Skinned Mesh")]
    static void BakeSelected() {
        var smr = Selection.activeGameObject?.GetComponent<SkinnedMeshRenderer>();
        if (smr == null) {
            Debug.LogError("Select a GameObject with a SkinnedMeshRenderer first.");
            return;
        }

        Mesh bakedMesh = new Mesh();
        smr.BakeMesh(bakedMesh);

        string path = "Assets/BakedMesh.asset";
        AssetDatabase.CreateAsset(bakedMesh, path);
        AssetDatabase.SaveAssets();

        GameObject go = new GameObject(smr.name + "_Baked");
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = bakedMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = smr.sharedMaterials;

        Debug.Log("Baked mesh saved to " + path);
    }
}
