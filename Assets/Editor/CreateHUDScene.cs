using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateHUDScene
{
    [MenuItem("Tools/Create HUD Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        var hudGO = new GameObject("HUD");
        hudGO.AddComponent<HUD>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/HUD.unity");

        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string hudPath = "Assets/Scenes/HUD.unity";
        bool exists = false;
        foreach (var s in buildScenes)
        {
            if (s.path == hudPath) { exists = true; break; }
        }
        if (!exists)
        {
            buildScenes.Add(new EditorBuildSettingsScene(hudPath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        Debug.Log("HUD scene created at Assets/Scenes/HUD.unity and added to Build Settings");
    }
}
