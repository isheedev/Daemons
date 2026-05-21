using UnityEditor;
using UnityEngine;

// Utility: enable Read/Write on model assets (FBX/OBJ) so runtime mesh access works
public static class EnableMeshReadWrite
{
    [MenuItem("Tools/Enable Read/Write For Models (Project)")]
    public static void EnableReadWriteForAllModels()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");
        int count = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log($"Enabled Read/Write: {path}");
                count++;
            }
        }

        Debug.Log($"EnableMeshReadWrite: processed {guids.Length} model(s), updated {count}.");
    }

    [MenuItem("Tools/Enable Read/Write For Models (Selected Folder)")]
    public static void EnableReadWriteForSelectedFolder()
    {
        var obj = Selection.activeObject;
        if (obj == null)
        {
            Debug.LogWarning("Select a folder or asset in the Project window first.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Could not determine asset path for selection.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { path });
        int count = 0;
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var importer = AssetImporter.GetAtPath(p) as ModelImporter;
            if (importer == null) continue;

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log($"Enabled Read/Write: {p}");
                count++;
            }
        }

        Debug.Log($"EnableMeshReadWrite: processed {guids.Length} model(s) in '{path}', updated {count}.");
    }
}
