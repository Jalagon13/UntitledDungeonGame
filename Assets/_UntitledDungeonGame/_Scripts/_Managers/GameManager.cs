using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UntitledDungeonGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Update()
        {
            MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
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
