//DOCUMENTED CODE
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class DeathScreenManager : NetworkBehaviour
{
    public GameObject deathScreenUI;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInput playerInput;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        { //subscribe to the player's death event to show the death screen when the player dies
            playerHealth.OnDeath += ShowDeathScreen;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {   // Unsubscribe from the player's death event to prevent memory leaks when the object is despawned
            playerHealth.OnDeath -= ShowDeathScreen;
        }
    }
    private void ShowDeathScreen()
    {   // Enable the death screen UI and make it interactive when the player dies
        deathScreenUI.GetComponent<Image>().raycastTarget = true; // Enable interaction
        deathScreenUI.GetComponent<CanvasGroup>().blocksRaycasts = true; // Ensure it blocks raycasts
        deathScreenUI.GetComponent<CanvasGroup>().alpha = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideDeathScreen()
    {   // implemented, but not use
        deathScreenUI.GetComponent<Image>().raycastTarget = false;
        deathScreenUI.GetComponent<CanvasGroup>().blocksRaycasts = false ; // Ensure it blocks raycasts
        deathScreenUI.GetComponent<CanvasGroup>().alpha = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {   // Return to the main menu by shutting down the network and loading the main menu scene
        SessionManager.Instance.ReturnToMainMenu();
    }

}
