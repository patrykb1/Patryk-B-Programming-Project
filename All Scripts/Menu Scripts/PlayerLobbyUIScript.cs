using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyUIScript : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Button kickPlayerButton;
    public GameObject readyTick;

    public void Setup(Lobby lobby, Player player, LobbyManager manager)
    {   // Setup UI when object is instantiated
        string lobbyId = lobby.Id;
        string playerId = player.Id;
        // Update the player name text, set a default value if not found to prevent errors
        playerNameText.text = player.Data.TryGetValue("Username", out PlayerDataObject playerNameData) 
        ? playerNameData.Value : "Unknown Player";
        string localPlayerId = AuthenticationService.Instance.PlayerId;
        bool isHost = lobby.HostId == localPlayerId;
        bool isSelf = playerId == localPlayerId;
        UpdateReadyTick(player);
        // Only host can kick, and host cannot kick themselves
        kickPlayerButton.gameObject.SetActive(isHost && !isSelf);

        kickPlayerButton.onClick.RemoveAllListeners();
        kickPlayerButton.onClick.AddListener(() =>
            manager.KickPlayer(lobbyId, playerId)
        );
    }

    public void UpdateReadyTick(Player player)
    { 
        if (player == null || readyTick == null)
        {
            Debug.LogWarning("Player or Ready tick is null in UpdateReadyTick");
            return;
        }
        if (player.Data != null && player.Data.TryGetValue("Ready", out PlayerDataObject readyData))
        {   // Attempt to parse the "Ready" state
            bool.TryParse(readyData.Value, out bool result);
            readyTick.SetActive(result);
        }
        else
        {   // If "Ready" state is not found, set the tick to inactive
            readyTick.SetActive(false);
        }
    }
}