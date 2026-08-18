using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance; // Singleton
    private string username;
    private bool serverDisconnected = false;
    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Avoid duplicates
        }
    }
    // Update the username
    public void UpdateUsername(string newUsername) => username = newUsername;

    public string GetUsername() => username;

    // Set the server disconnected status
    public void SetServerDisconnected(bool value) => serverDisconnected = value;

    public bool GetServerDisconnected() => serverDisconnected;

}