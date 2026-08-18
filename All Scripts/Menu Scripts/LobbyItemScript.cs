using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;


public class LobbyItemScript : MonoBehaviour
{
    public TMP_Text lobbyNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    public void Setup(Lobby lobby, LobbyManager manager)
    {
        lobbyNameText.text = lobby.Name;
        string lobbyId = lobby.Id;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            manager.JoinLobby(lobbyId);
        });
    }
}
