using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class LocalLeaderboardManager : NetworkBehaviour
{
    public Transform contentTransform; // Parent of all entries
    public GameObject entryPrefab;     // Prefab for a player row
    public static LocalLeaderboardManager Instance;
    public PlayerInputHandler playerInputHandler;

    public override void OnNetworkSpawn()
    {   // Register local player to server-owned
        if (IsOwner)
        {
            Instance = this;
            string username = PlayerData.Instance.GetUsername();
            StartCoroutine(WaitOneSecondAndRegister(username));
        }
        else
        {
            enabled = false; // Disable for non-owners
        }
    }

    private IEnumerator WaitOneSecondAndRegister(string username)
    {
        yield return new WaitForSeconds(1f);

        if (IsServer)
        {
            LeaderboardManager.Instance.RegisterPlayer(OwnerClientId, username);
        }
        else
        {
            SendUsernameServerRpc(OwnerClientId, username);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendUsernameServerRpc(ulong clientId, string username)
    {   //RPC sent to server by client, asking to register the client
        Debug.Log($"Server received username {username} from {clientId}");
        LeaderboardManager.Instance.RegisterPlayer(clientId, username);
    }

    public void UpdateLeaderboardUI(NetworkedPlayerStats[] networkedPlayers)
    {   //Updates leaderboard UI
        if (contentTransform == null || entryPrefab == null) return;
        //Convert networked player stats to player stats
        List<PlayerStats> sortedPlayers = networkedPlayers
            .Select(p => p.ToPlayerStats())
            .ToList();
        // Destroy old entries
        foreach (Transform child in contentTransform)
        {
            if (child.name == "Header") continue; // Keep the header row
            Destroy(child.gameObject);
        }
        // Instantiate new entries
        foreach (var player in sortedPlayers)
        {
            GameObject entry = Instantiate(entryPrefab, contentTransform);
            entry.GetComponent<LeaderboardEntry>().SetData(player);
        }
    }

    private void Update()
    {
        bool leaderboardOn = playerInputHandler.LeaderboardInput;

        var canvasGroup = contentTransform.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) return;

        canvasGroup.alpha = leaderboardOn ? 1f : 0f;
    }
}
