using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UntitledDungeonGame
{
    public class GameManager : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (Loader.IsHost)
            {
                Debug.Log($"Starting game as host");
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.Log($"Starting game as client");
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}
