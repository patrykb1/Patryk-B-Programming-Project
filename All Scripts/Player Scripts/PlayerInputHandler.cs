//DOCUMENTED CODE
using UnityEngine;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour
{
    [SerializeField] UnityEngine.InputSystem.PlayerInput playerInput;
    public Vector2 MoveInput { get; private set; }
    public bool JumpInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool SprintInput { get; private set; }
    public bool ShootInput { get; private set; }
    public bool ReloadInput { get; private set; }
    public bool AimInput { get; private set; }
    public bool CrouchInput { get; private set; }

    public bool PauseInput { get; private set; }

    public bool LeaderboardInput { get; private set; }
    public NetworkVariable<bool> isAiming = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    [SerializeField] private PlayerHealth playerHealth;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        { // Enable the PlayerInput component and set the initial action map to "Player" for the local player
            playerInput.enabled = true;
            playerInput.SwitchCurrentActionMap("Player");
            playerHealth.OnDeath += () => playerInput.SwitchCurrentActionMap("UI");
        }
        else
        { // Disable the PlayerInput component for non-owners to prevent them from processing input
            playerInput.enabled = false;
        }
    }
    void Update()
    {
        if (!IsOwner) return; // Only the owner should process input
        var playerActionMap = playerInput.actions.FindActionMap("Player", true);
        if (playerInput.currentActionMap.name == "UI")
        {   // Only check for pause input when in UI to prevent conflicts with player controls
            playerActionMap = playerInput.actions.FindActionMap("UI", true);
            PauseInput = playerActionMap.FindAction("Pause", true).WasPressedThisFrame();
            return;
        } // Read input values and store them as public properties for other scripts to access
        MoveInput = playerActionMap.FindAction("Move", true).ReadValue<Vector2>();
        JumpInput = playerActionMap.FindAction("Jump", true).WasPressedThisFrame();
        LookInput = playerActionMap.FindAction("Look", true).ReadValue<Vector2>();
        SprintInput = playerActionMap.FindAction("Sprint", true).IsPressed();
        ShootInput = playerActionMap.FindAction("Shoot", true).WasPressedThisFrame();
        ReloadInput = playerActionMap.FindAction("Reload", true).WasPressedThisFrame();
        AimInput = playerActionMap.FindAction("Aim", true).IsPressed();
        CrouchInput = playerActionMap.FindAction("Crouch", true).IsPressed();
        PauseInput = playerActionMap.FindAction("Pause", true).WasPressedThisFrame();
        LeaderboardInput = playerActionMap.FindAction("Leaderboard", true).IsPressed();
        isAiming.Value = AimInput;
    }
}
