using PlayFab.ExperimentationModels;
using System.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class PauseManager : NetworkBehaviour
{
    public bool isPaused = false;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private PlayerInput playerInput;
    public GameObject pauseUI;
    public GameObject crosshair;
    void Update()
    {   // Pause the game if pause button is pressed. If game is paused already, resume it
        if (playerInputHandler.PauseInput)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {   // Resume the game using a coroutine to ensure focus
        StartCoroutine(ResumeRoutine());
    }
    IEnumerator ResumeRoutine()
    {   // Resume the game using a coroutine
        playerInput.SwitchCurrentActionMap("Player");
        pauseUI.SetActive(false);
        crosshair.SetActive(true);
        isPaused = false;

        yield return null; // wait a frame to ensure focus

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PauseGame()
    {   // Pause the game
        playerInput.SwitchCurrentActionMap("UI");
        pauseUI.SetActive(true);
        crosshair.SetActive(false);
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UnityEngine.EventSystems.EventSystem.current
            .SetSelectedGameObject(pauseUI.transform.GetChild(0).gameObject);
    }

    public void LoadMenu()
    {
        if (IsServer)
        {
            LeaderboardManager.Instance.GetScore(OwnerClientId);
        }
        else
        {
            LeaderboardManager.Instance.RequestScoreServerRpc(OwnerClientId);
        }
        gameObject.GetComponent<PlayerUI>().UnsubscribeToEvents();
        SessionManager.Instance.ReturnToMainMenu(); 
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked");
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
