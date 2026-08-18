using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections; // Needed for FixedString128Bytes
public class NametagRotator : NetworkBehaviour
{
    private Transform playerCamera;
    public TMP_Text nametag;
    public Transform nametagTransform;

    // NetworkVariable with FixedString128Bytes instead of string
    public NetworkVariable<FixedString128Bytes> username =
       new(
           writePerm: NetworkVariableWritePermission.Owner, // Only owner can write
           readPerm: NetworkVariableReadPermission.Everyone   // Everyone can read
       );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Assign local username to NetworkVariable
            username.Value = new FixedString128Bytes(PlayerData.Instance.GetUsername());
            Debug.Log("Assigned username to NetworkVariable: " + username.Value);
            return;
        }
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        // Set initial nametag text
        nametag.text = username.Value.IsEmpty ? "Player" : username.Value.ToString();

        // Subscribe to changes so the text updates if the value changes
        username.OnValueChanged += OnUsernameChanged;
    }

    private void OnUsernameChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        nametag.text = newValue.ToString();
    }

    public override void OnNetworkDespawn()
    {
        username.OnValueChanged -= OnUsernameChanged;
    }

    private void Update()
    {
        // If we don't have a camera yet, try to assign it
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null) return; // Still missing
        }
        Vector3 direction = playerCamera.position - transform.position;
        nametagTransform.rotation = Quaternion.LookRotation(direction);
    }
}