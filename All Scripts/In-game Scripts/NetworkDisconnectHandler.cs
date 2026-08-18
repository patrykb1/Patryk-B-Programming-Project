using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkDisconnectrHandler : MonoBehaviour
{
    void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Disconnected from host. Returning to main menu...");
            PlayerData.Instance.SetServerDisconnected(true);
            // Load your main menu scene
            SceneManager.LoadScene("Main Menu");
        }
    }
}