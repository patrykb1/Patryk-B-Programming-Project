using Unity.Netcode;
using UnityEngine;

public class DummyListenerScript : NetworkBehaviour
{
    private AudioListener dummy;

    private void Awake()
    {
        dummy = gameObject.GetComponent<AudioListener>();
    }
    void Start()
    {
        dummy.enabled = true;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnDestroy()
    {   // Always unsubscribe to avoid memory leaks
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        base.OnDestroy();
    }
    private void OnClientConnected(ulong clientId)
    {
        // Only the local player matters
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Disable dummy listener when local player exists
            dummy.enabled = false;

            // Ensure that the player's audio listener is enabled, so that exactly 1 is enabled at all times
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                var playerListener = localPlayer.GetComponentInChildren<AudioListener>();
                if (playerListener != null)
                    playerListener.enabled = true;
            }
        }
    }

}
