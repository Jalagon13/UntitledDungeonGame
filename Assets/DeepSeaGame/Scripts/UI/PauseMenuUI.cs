using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeepSeaGame
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static Action OnPauseMenuOpened;
        public static Action OnPauseMenuClosed;

        [SerializeField] private GameObject _pauseMenuUI;
        [SerializeField] private float _shortUnpauseDelay = 0.2f;

        private bool _pauseMenuOpen;
        public bool IsPauseMenuOpen => _pauseMenuOpen;

        private void Start()
        {
            Hide(false);

            GameInput.Instance.OnTogglePauseMenu += GameInput_OnTogglePauseMenu;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnTogglePauseMenu -= GameInput_OnTogglePauseMenu;
        }

        private void GameInput_OnTogglePauseMenu(object sender, InputAction.CallbackContext e)
        {
            if (CraftingMenuUI.Instance.CraftingMenuUIOpen)
            {
                InventoryManager.Instance.ToggleInventory();
                return;
            }

            if (!_pauseMenuOpen)
            {
                Show();
            }
            else
            {
                Hide(false);
            }
        }

        public void ResumeButtonPressed()
        {
            Hide(true);
        }

        public void QuitToMainMenuButtonPressed()
        {
            Time.timeScale = 1f;
            // Scene loading stuff here
            Loader.Load(Loader.Scene.MainMenuScene);
        }

        public void SurveyButton()
        {
            Application.OpenURL("https://docs.google.com/forms/d/e/1FAIpQLSci_BikUHDYd5gK0aGFpBO9uqyJIpj7qYRKAGguH6i_U9S_eQ/viewform?usp=dialog");
        }

        private void Show()
        {
            OnPauseMenuOpened?.Invoke();

            _pauseMenuUI.SetActive(true);
            _pauseMenuOpen = true;

            Time.timeScale = 0f;
        }

        private void Hide(bool delay)
        {
            if (delay)
            {
                StartCoroutine(Delay());
            }
            else
            {
                OnPauseMenuClosed?.Invoke();

                Time.timeScale = 1f;

                _pauseMenuUI.SetActive(false);
                _pauseMenuOpen = false;
            }
        }

        private IEnumerator Delay()
        {
            Time.timeScale = 1f;

            yield return new WaitForSecondsRealtime(_shortUnpauseDelay);
            OnPauseMenuClosed?.Invoke();

            _pauseMenuUI.SetActive(false);
            _pauseMenuOpen = false;
        }
    }
}
