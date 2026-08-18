using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void ReturnToMainMenu() => StartCoroutine(ShutdownAndLoad());

    private IEnumerator ShutdownAndLoad()
    {
        NetworkManager.Singleton.Shutdown();

        // Wait until shutdown actually completes
        yield return new WaitUntil(() =>
            !NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer
        );
        SceneManager.LoadScene("Main Menu");
    }
}