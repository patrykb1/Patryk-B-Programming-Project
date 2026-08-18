using UnityEngine;
using Unity.Netcode;
public class CanvasScript : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        gameObject.SetActive(IsOwner);
    }
}
