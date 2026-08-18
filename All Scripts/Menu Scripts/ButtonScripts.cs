using UnityEngine;

public class ButtonScripts : MonoBehaviour
{   
    GraphicsUI graphicsUI;
    VolumeUI volumeUI;

    private void Awake() 
    {   //Helper method to reference the correct UI scripts
        graphicsUI = SettingsManager.Instance.gameObject.GetComponent<GraphicsUI>();
        volumeUI = SettingsManager.Instance.gameObject.GetComponent<VolumeUI>();
    }

    //Menu Buttons
    public void OnPlayPressed() => MenuManager.Instance.PlayPressed();
    public void OnExitPressed() => MenuManager.Instance.QuitGame();

    //Lobby Buttons
    public void OnCreateLobbyPressed() => LobbyManager.Instance.CreateLobby();
    public void OnEditLobbyName(string newName) => LobbyManager.Instance.EditLobbyName(newName);
    public void OnListLobbyPressed() => LobbyManager.Instance.ListLobbies();
    public void OnStartGamePressed() => LobbyManager.Instance.OnStartPressed();
    public void OnLeaveLobbyPressed() => LobbyManager.Instance.LeaveLobby();
    public void OnListPlayersPressed() => LobbyManager.Instance.ListPlayers();

    //Settings Buttons
    public void OnSaveChangesPressed() => SettingsManager.Instance.SaveChanges();
    public void OnFullscreenToggle(bool toggle) => graphicsUI.OnFullscreenToggle(toggle);
    public void OnMasterChanged(float value) => volumeUI.OnMasterChanged(value);
    public void OnMusicChanged(float value) => volumeUI.OnMusicChanged(value);
    public void OnSFXChanged(float value) => volumeUI.OnSFXChanged(value);
}
