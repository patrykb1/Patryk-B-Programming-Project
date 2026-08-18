using UnityEngine;
using Unity.Netcode;

public class FPSPlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject bodyMesh;   // full body
    [SerializeField] private GameObject fpsGun;
    [SerializeField] private GameObject nameTag;

    public NetworkVariable<NetworkObjectReference> WorldGun =
    new NetworkVariable<NetworkObjectReference>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkObject resolvedWorldGun;

    public override void OnNetworkSpawn()
    {   // Assign value of networked WorldGun to local resolvedWorldGun
        WorldGun.OnValueChanged += OnWorldGunAssigned;
        if (WorldGun.Value.TryGet(out var gun))
        {
            resolvedWorldGun = gun;
        }

        ApplyViewSetup();
    }

    public override void OnDestroy()
    {
        WorldGun.OnValueChanged -= OnWorldGunAssigned;
    }

    //Called when server assigns the world gun
    private void OnWorldGunAssigned
    (
        NetworkObjectReference oldRef,
        NetworkObjectReference newRef
    )
    {
        if (!newRef.TryGet(out var gun)) return;
        resolvedWorldGun = gun;
        ApplyWorldGunViewRules();
    }

    private void ApplyViewSetup()
    {
        if (IsOwner)
        {   // Owner should see own gun, and not see own nametag or body mesh
            fpsGun.SetActive(true);
            SetLayerRecursively(bodyMesh, LayerMask.NameToLayer("InvisibleForLocalPlayer"));
            SetLayerRecursively(nameTag, LayerMask.NameToLayer("InvisibleForLocalPlayer"));
            foreach (var rend in bodyMesh.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        else
        {   // Other players shouldn't see local gun as they see the world gun.
            fpsGun.SetActive(false);
            SetLayerRecursively(bodyMesh, LayerMask.NameToLayer("VisibleForOtherPlayer"));
            SetLayerRecursively(nameTag, LayerMask.NameToLayer("VisibleForOtherPlayer"));

            foreach (var rend in bodyMesh.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }

    private void ApplyWorldGunViewRules()
    {   // Apply visibility rules to the WORLD gun
        if (resolvedWorldGun == null) return;

        int layer = IsOwner
            ? LayerMask.NameToLayer("InvisibleForLocalPlayer")
            : LayerMask.NameToLayer("VisibleForOtherPlayer");

        SetLayerRecursively(resolvedWorldGun.gameObject, layer);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {   // Recursion algorithm to set layer for all child objects
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}