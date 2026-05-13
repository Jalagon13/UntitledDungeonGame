using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UntitledDungeonGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_InputField _joinInput;
        [SerializeField] private Relay _relay;

        private void Awake()
        {
            if (_hostButton != null)
            {
                _hostButton.onClick.AddListener(() =>
                {
                    _relay.CreateRelay();
                });
            }

            if (_joinButton != null)
            {
                _joinButton.onClick.AddListener(() =>
                {
                    _relay.JoinRelay(_joinInput.text);
                });
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(() =>
                {
                    Application.Quit();
                });
            }

            Time.timeScale = 1f;
        }
    }
}
