#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneMenuItem : Editor
{
    [MenuItem("Scenes/MainScene")]
    public static void OpenMainScene()
    {
        OpenScene("Assets/Scenes/MainScene");
    }

    [MenuItem("Scenes/LevelsScene")]
    public static void OpenLevelsScene()
    {
        OpenScene("Assets/Scenes/LevelsScene");
    }

    private static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path + ".unity");
        }
    }
}
#endif