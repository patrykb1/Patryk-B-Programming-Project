using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
// Define a struct to hold player stats
public struct PlayerStats
{
    public string username;
    public int score;
    public int kills;
    public int ping;

    public PlayerStats(string username, int score, int kills, int ping)
    {
        this.username = username;
        this.score = score;
        this.kills = kills;
        this.ping = ping;
    }
}

public struct NetworkedPlayerStats: INetworkSerializable
{
    public FixedString128Bytes username; // string replacement: normal strings can't be networked
    public int score;
    public int kills;
    public int ping;
    public NetworkedPlayerStats(PlayerStats p) //Constructor
    {
        username = new FixedString128Bytes(p.username);
        score = p.score;
        kills = p.kills;
        ping = p.ping;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref username);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref kills);
        serializer.SerializeValue(ref ping);
    }
    public PlayerStats ToPlayerStats() => new PlayerStats(username.ToString(), score, kills, ping);
}

public class LeaderboardManager : NetworkBehaviour
{   //ulong is the client ID, PlayerStats holds the player's username, score, kills, and ping
    private readonly Dictionary<ulong, PlayerStats> playerStats = new(); 
    public static LeaderboardManager Instance;
    public override void OnNetworkSpawn()
    {   // Ensure only one LeaderboardManager exists, and clear the dictionary
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        playerStats.Clear();
    }
    public void RegisterPlayer(ulong clientId, string username)
    {   // If the player doesn't already exist, add them
        if (!playerStats.ContainsKey(clientId)) playerStats[clientId] = new PlayerStats(username, 0, 0, 0);
        UpdateLeaderboardServerCall();
    }
    private List<PlayerStats> GetSortedLeaderboard() => playerStats.Values
        .OrderByDescending(p => p.score).ThenBy(p => p.username).ToList();  // Returns a sorted leaderboard
    void UpdateLeaderboardServerCall()
    {   // The server calls this method to update the leaderboard both locally and on all clients
        var sorted = GetSortedLeaderboard();
        var netStats = sorted.Select(p => new NetworkedPlayerStats(p)).ToArray();
        LocalLeaderboardManager.Instance.UpdateLeaderboardUI(netStats);
        UpdateLeaderboardClientRpc(netStats);
    }

    [ClientRpc]
    private void UpdateLeaderboardClientRpc(NetworkedPlayerStats[] updatedStats)
    {   // Server (host) cannot execute client RPCs that it calls itself
        LocalLeaderboardManager.Instance.UpdateLeaderboardUI(updatedStats);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IncrementKillsServerRpc(ulong clientId) => IncrementKills(clientId);
    public void IncrementKills(ulong clientId)
    {
        if (playerStats.TryGetValue(clientId, out var stats))
        {
            stats.kills++;
            playerStats[clientId] = stats; 
            UpdateLeaderboardServerCall();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IncrementScoreServerRpc(ulong clientId, int amount) => IncrementScore(clientId, amount);
    public void IncrementScore(ulong clientId, int amount){
        if (playerStats.TryGetValue(clientId, out var stats))
        {
            stats.score += amount;
            playerStats[clientId] = stats; // Update the dictionary
            UpdateLeaderboardServerCall();
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestScoreServerRpc(ulong clientId){
        if (playerStats.TryGetValue(clientId, out var stats)) 
            SaveHighScoreClientRpc(stats.score,clientId);
    }
    public void GetScore(ulong clientId){
        if (playerStats.TryGetValue(clientId, out var stats))
            PlayfabLogin.Instance.SaveHighScore(stats.score);
    }
    [ClientRpc]
    private void SaveHighScoreClientRpc(int score, ulong targetClientId){
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        PlayfabLogin.Instance.SaveHighScore(score);
    }
}
