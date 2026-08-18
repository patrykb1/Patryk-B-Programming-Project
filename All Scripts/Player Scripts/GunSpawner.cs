using Unity.Netcode;
using UnityEngine;

public class GunSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject gunPrefab;
    [SerializeField] private Transform gunHolder;
    private NetworkObject gunNetworkObject;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) SpawnGunServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void SpawnGunServerRpc(ulong ownerClientId, ServerRpcParams rpcParams = default)
    {
        // Only spawn if this client doesn't already have a gun
        if (gunNetworkObject != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(gunNetworkObject.NetworkObjectId)) return;
        GameObject gunGO = Instantiate(gunPrefab);  
        var netObj = gunGO.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(ownerClientId);   // Spawn with this client's ownership
        gunNetworkObject = netObj;
        var playerSetup = GetComponent<FPSPlayerSetup>();
        playerSetup.WorldGun.Value = netObj;    // update the player's world gun reference
        SetupGunClientRpc(netObj.NetworkObjectId, ownerClientId); // Tell all clients to setup this gun
    }

    [ClientRpc]
    private void SetupGunClientRpc(ulong gunNetId, ulong ownerClientId, ClientRpcParams clientRpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(gunNetId, out var netObj)) return;
        gunNetworkObject = netObj;
        // Only the owning client needs input
        var gunScript = gunNetworkObject.GetComponent<GunScript>();
        gunScript.SetHolder(gunHolder.Find("Gun Offset"));  // Set the gun holder
        if (NetworkManager.Singleton.LocalClientId == ownerClientId) gunScript.InitializeInput(transform.GetComponent<PlayerInputHandler>());

        gunNetworkObject.GetComponent<AmmoManager>().SetPlayer(transform); // Ammo Setup

        transform.GetComponentInChildren<RealWorldRigScript>()?.OnWeaponSpawn(gunNetworkObject.gameObject); // Setup rig for arm movement
        transform.GetComponent<PlayerUI>()?.SetGun(gunNetworkObject.gameObject);    // Setup player UI
    }
}