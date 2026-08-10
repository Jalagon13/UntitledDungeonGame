using FMOD.Studio;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeaGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_InputField _joinInput;
        [SerializeField] private Relay _relay;

        private EventInstance _titleMenuMusicEventInstance;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (_hostButton != null)
            {
                _hostButton.onClick.AddListener(() =>
                {
                    _relay.CreateRelay();
                });
            }

            if (_joinButton != null)
            {
                // _joinButton.onClick.AddListener(() =>
                // {
                //     _relay.JoinRelay(_joinInput.text);
                // });
            }

            _quitButton.onClick.AddListener(() =>
            {
                Application.Quit();
            });

            Time.timeScale = 1f;
        }

        private void Start()
        {
            // AudioManager.Instance.InitializeAmbience(FMODEvents.Instance.WindAmb);

            _titleMenuMusicEventInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.TitleMusic);
            _titleMenuMusicEventInstance.start();

            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            // AudioManager.Instance.StopCurrentAmbience();
            _titleMenuMusicEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
    
}
