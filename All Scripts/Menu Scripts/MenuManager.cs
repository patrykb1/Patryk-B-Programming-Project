using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    public PlayfabLogin playfabLogin;

    public GameObject lobbyPanel;
    public GameObject accountPanel;
    public CanvasGroup settingsPanel;
    public PopupUI popup;
    public GameObject keyboardBinds;
    public GameObject controllerBinds;
    public InputActionAsset inputActions;

    // Subscribe to scene events
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    
    private void Awake()
    {   // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {   // Handle scene loading
        if (scene.name == "Main Menu")
        {   //set ui input actions asset to "UI" action map
            inputActions.FindActionMap("UI").Enable();
            inputActions.FindActionMap("Player").Disable();
            FindFirstObjectByType<ReferenceReloader>().ReloadMenuManager();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            popup = FindFirstObjectByType<PopupUI>();
            if (PlayerData.Instance.GetServerDisconnected())
            {   // Show server disconnected message
                StartCoroutine(ShowServerDisconnectedMessageNextFrame());
            }
        }
    }

    private IEnumerator ShowServerDisconnectedMessageNextFrame()
    {
        yield return null; // Wait one frame to ensure the scene is fully loaded
        popup.ShowMessage("Server disconnected! Please log in again.");
        PlayerData.Instance.SetServerDisconnected(false);
    }

    public void QuitGame() => Application.Quit();

    public void PlayPressed()
    {   // Handle play button pressed
        if (string.IsNullOrEmpty(PlayfabLogin.Instance.username))
        {
            popup.ShowMessage("Please log in to play!");
        }
        else
        {
            lobbyPanel.SetActive(true);
            accountPanel.SetActive(false);
            settingsPanel.alpha = 0;
            settingsPanel.interactable = false;
        }
    }

    public void OnKeybindsLoaded()
    {   // Handle UI when keybinds are loaded
        keyboardBinds.SetActive(true);
        controllerBinds.SetActive(false);
    }
}
