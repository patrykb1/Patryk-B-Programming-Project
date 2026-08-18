using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class PopupUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text messageText;
    public Button okButton;
    public Image popupBlocker;
    private GameObject previousSelected;
    private void Awake()
    {   // Disable the popup when the menu is loaded
        panel.SetActive(false);
        okButton.onClick.AddListener(() => ClosePopup());
    }

    public void ShowMessage(string message)
    {   // Display the popup with the provided message
        previousSelected = EventSystem.current.currentSelectedGameObject; // Store the currently selected UI element
        messageText.text = message;
        Debug.Log($"Showing popup message: {message}");
        if (panel != null)
        {
            panel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(okButton.gameObject);
            popupBlocker.raycastTarget = true; // Enable blocker to prevent interactions with underlying UI
        }
    }

    private void ClosePopup()
    {   // Hide the popup and restore previous selection
        panel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(previousSelected); // Restore previous selection
        popupBlocker.raycastTarget = false; // Disable blocker to allow interactions with underlying UI
    }
}
