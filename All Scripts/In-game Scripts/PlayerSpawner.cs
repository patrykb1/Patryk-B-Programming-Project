using Unity.Netcode;
using UnityEngine;

public class NewLobbyManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab; // assign your player prefab in Inspector

    public void CleanupOldPlayers()
    {
        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {   // Loop over connected clients
            if (clientPair.Value.PlayerObject != null && clientPair.Value.PlayerObject.IsSpawned)
            {
                // Player object already exists (likely from previous scene)
                clientPair.Value.PlayerObject.Despawn();   // Properly despawn the NetworkObject
                Destroy(clientPair.Value.PlayerObject.gameObject); // Remove any leftover GameObject
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null)
        {   // Only run if there is a network manager
            if (NetworkManager.Singleton.IsServer)
            {   // Server should spawn players and start game
                CleanupOldPlayers(); // Clean up any leftover player objects from previous scenes
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) SpawnPlayer(clientId);
                GameManager.Instance.StartGame();
            }   // All players subscribe to OnClientConnected
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {   // Spawn player if client is in connected clients and client isn't already spawned
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned) return;
        }
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null) return;
        Vector3 spawnPosition = new Vector3(Random.Range(0f, 10f), 20f, Random.Range(0f, 10f));
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {   // Destroy player instance if player is instantiated incorrectly
            Destroy(playerInstance);
            return;
        }
        netObj.SpawnAsPlayerObject(clientId, false);
    }
}