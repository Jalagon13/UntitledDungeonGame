using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace UntitledDungeonGame
{
    [InitializeOnLoad]
    public static class PlayFromStartScene
    {
        static PlayFromStartScene()
        {
            EditorApplication.playModeStateChanged += LoadStartScene;
        }

        private static void LoadStartScene(PlayModeStateChange state)
        {
            // Check if entering Play Mode
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Define your desired starting scene
                string startSceneName = "MainMenuScene";

                // Check if the active scene is not the desired start scene
                if (SceneManager.GetActiveScene().name != startSceneName)
                {
                    // Load the starting scene
                    Scene startScene = SceneManager.GetSceneByName(startSceneName);
                    if (startScene != null)
                    {
                        SceneManager.LoadScene(startSceneName);
                    }
                    else
                    {
                        Debug.LogError($"Scene '{startSceneName}' not found! Make sure it's in the build settings.");
                    }
                }
            }
        }
    }
}