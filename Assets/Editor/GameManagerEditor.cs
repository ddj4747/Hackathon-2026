using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GameManager))]
public sealed class GameManagerEditor : Editor
{
    private SceneAsset selectedLevel;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Beaten Levels", EditorStyles.boldLabel);
        var manager = (GameManager)target;
        List<string> beatenScenes = FindBeatenScenes(manager);
        if (beatenScenes.Count == 0)
            EditorGUILayout.HelpBox("No beaten levels saved.", MessageType.Info);

        foreach (string beatenPath in beatenScenes)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<SceneAsset>(beatenPath), typeof(SceneAsset), false);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                manager.SetLevelBeaten(beatenPath, false);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        selectedLevel = (SceneAsset)EditorGUILayout.ObjectField("Add Level", selectedLevel, typeof(SceneAsset), false);
        string path = selectedLevel != null ? AssetDatabase.GetAssetPath(selectedLevel) : string.Empty;
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path) || path == "Assets/Scenes/Menu.unity" || manager.IsLevelBeaten(path)))
        {
            if (GUILayout.Button("Add Beaten Level"))
            {
                manager.SetLevelBeaten(path, true);
                selectedLevel = null;
            }
        }
    }

    private static List<string> FindBeatenScenes(GameManager manager)
    {
        var beatenScenes = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (manager.IsLevelBeaten(path))
                beatenScenes.Add(path);
        }
        beatenScenes.Sort(System.StringComparer.OrdinalIgnoreCase);
        return beatenScenes;
    }
}
