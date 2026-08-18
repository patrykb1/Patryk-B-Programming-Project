using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class ReferenceReloader : MonoBehaviour
{
    [Header("Menu Manager")]
    public PlayfabLogin playfabLogin;
    public GameObject lobbyPanel;
    public GameObject accountPanel;
    public CanvasGroup settingsPanel;
    public PopupUI popup;
    public GameObject keyboardBinds;
    public GameObject controllerBinds;
    public InputActionAsset inputActions;

    [Header("Playfab Login")]
    public TMP_InputField emailInput;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;
    public Button signUpOrLoginButton;
    public TMP_Text usernameText;
    public TMP_Text emailText;
    public GameObject selection;
    public GameObject accountSettings;
    public static PlayfabLogin Instance;

    [Header("Lobby Manager")]
    public Transform lobbyListContainer;
    public Transform playerListContainer;
    public TMP_Text lobbyIdText;
    public GameObject lobbyEditorPanel;
    public PopupUI popupUI;
    public GameObject lobbyNameEditor;
    public Button readyButton;
    public TMP_Text deviceId;

    [Header("Volume UI")]
    public Slider masterSlider;
    public TMP_Text masterLabel;
    public Slider musicSlider;
    public TMP_Text musicLabel;
    public Slider sfxSlider;
    public TMP_Text sfxLabel;

    public void ReloadMenuManager()
    {
        MenuManager.Instance.playfabLogin = playfabLogin;
        MenuManager.Instance.lobbyPanel = lobbyPanel;
        MenuManager.Instance.accountPanel = accountPanel;
        MenuManager.Instance.settingsPanel = settingsPanel;
        MenuManager.Instance.popup = popup;
        MenuManager.Instance.keyboardBinds = keyboardBinds;
        MenuManager.Instance.controllerBinds = controllerBinds;

    }

    public void ReloadPlayfabLogin()
    {
        PlayfabLogin.Instance.emailInput = emailInput;
        PlayfabLogin.Instance.usernameInput = usernameInput;
        PlayfabLogin.Instance.passwordInput = passwordInput;
        PlayfabLogin.Instance.messageText = messageText;
        PlayfabLogin.Instance.signUpOrLoginButton = signUpOrLoginButton;
        PlayfabLogin.Instance.usernameText = usernameText;
        PlayfabLogin.Instance.emailText = emailText;
        PlayfabLogin.Instance.selection = selection;
        PlayfabLogin.Instance.accountSettings = accountSettings;
    }
    public void ReloadLobbyManager()
    {
        LobbyManager.Instance.lobbyListContainer = lobbyListContainer;
        LobbyManager.Instance.playerListContainer = playerListContainer;
        LobbyManager.Instance.lobbyIdText = lobbyIdText;
        LobbyManager.Instance.lobbyEditorPanel = lobbyEditorPanel;
        LobbyManager.Instance.popupUI = popupUI;
        LobbyManager.Instance.lobbyNameEditor = lobbyNameEditor;
        LobbyManager.Instance.readyButton = readyButton;
        LobbyManager.Instance.deviceId = deviceId;
    }

    public void ReloadControlsUI()
    {
        SettingsManager.Instance.gameObject.GetComponent<ControlsUI>().buttonImages = new System.Collections.Generic.List<GameObject>(GameObject.FindGameObjectsWithTag("BindButton"));
    }

    public void ReloadVolumeUI()
    {
        VolumeUI volumeUI = SettingsManager.Instance.gameObject.GetComponent<VolumeUI>();
        volumeUI.masterSlider = masterSlider;
        volumeUI.masterLabel = masterLabel;
        volumeUI.musicSlider = musicSlider;
        volumeUI.musicLabel = musicLabel;
        volumeUI.sfxSlider = sfxSlider;
        volumeUI.sfxLabel = sfxLabel;
    }
}
