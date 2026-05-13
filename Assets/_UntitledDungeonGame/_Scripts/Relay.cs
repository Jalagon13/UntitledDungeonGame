using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


namespace UntitledDungeonGame
{
    public class Relay : MonoBehaviour
    {
        private bool _createdRelay;
        private bool _joinedRelay;
        private bool _isOffline;

        private async void Start()
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // Detect offline mode
            _isOffline = Application.internetReachability == NetworkReachability.NotReachable;

            if (_isOffline)
            {
                Debug.LogWarning("No internet connection. Starting in offline mode...");

                transport.SetConnectionData("0.0.0.0", 7777);

                Loader.IsHost = true;
                Loader.Load(Loader.Scene.GameScene);
                return;
            }

            // If Online, use unity services
            try
            {
                await UnityServices.InitializeAsync();

                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
                };

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Unity Services failed to initialize: {e.Message}");
            }
        }

        public async void CreateRelay()
        {
            if (_createdRelay || _isOffline) return;

            try
            {
                _createdRelay = true;

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                GUIUtility.systemCopyBuffer = joinCode;
                Debug.Log(allocation.Region);
                Debug.Log("Join Code: " + joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

                Loader.IsHost = true;
                Loader.Load(Loader.Scene.GameScene);
            }
            catch (RelayServiceException e)
            {
                Debug.LogError(e);
            }
        }

        public async void JoinRelay(string joinCode)
        {
            if (_joinedRelay || _isOffline) return;

            try
            {
                _joinedRelay = true;

                Debug.Log($"Joining Relay with {joinCode}");
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

                Loader.IsHost = false;
                Loader.Load(Loader.Scene.GameScene);
            }
            catch (RelayServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }
}