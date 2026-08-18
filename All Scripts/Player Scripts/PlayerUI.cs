using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;
using Unity.Netcode;
public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private UIDocument gameUI;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Texture2D crouchIcon;
    [SerializeField] private Texture2D walkIcon;
    [SerializeField] private Texture2D sprintIcon;
    [SerializeField] private float fadeDuration = 1.5f;

    private Image iconHolder;
    [SerializeField] private PlayerController playerController;
    private AmmoManager ammoManager;
    private Label healthLabel;
    private Label ammoLabel;
    private Label roundLabel;
    private Label timerLabel;
    private Label eventLabel;
    private Label objectiveHealthLabel; 
    private bool subscribedToEvents = false;

    public void SetAmmoManager(AmmoManager manager)
    {   // Set the ammo manager here, only then can the rest of the setup be performed
        ammoManager = manager;
        SubscribeToEvents();
        LabelSetup();
    }
    private void LabelSetup()
    {   // Setup the labels for the UI
        if (!IsOwner)
        {   // If this player is not the owner, disable the HUD
            gameUI.gameObject.SetActive(false);
            return;
        }
        if (gameUI == null) return;

        healthLabel = gameUI.rootVisualElement.Q<Label>("HealthLabel");
        ammoLabel = gameUI.rootVisualElement.Q<Label>("AmmoLabel");
        roundLabel = gameUI.rootVisualElement.Q<Label>("RoundLabel");
        timerLabel = gameUI.rootVisualElement.Q<Label>("TimerLabel");
        iconHolder = gameUI.rootVisualElement.Q<Image>("MovementStateIcon");
        eventLabel = gameUI.rootVisualElement.Q<Label>("EventLabel");
        objectiveHealthLabel = gameUI.rootVisualElement.Q<Label>("ObjectiveHealthLabel");

        if (iconHolder != null)
        {   // Setup the icon holder
            iconHolder.scaleMode = ScaleMode.ScaleToFit;
            Texture2D defaultIcon = walkIcon ?? sprintIcon ?? crouchIcon;
            if (defaultIcon != null) UpdateMovementIcon(defaultIcon);
        }
        HandleAmmoChanged(ammoManager != null ? ammoManager.currentAmmo : 0);
        HandleHealthChanged(playerHealth != null ? playerHealth.health.Value : 0f);
    }

    private void SubscribeToEvents()
    {
        if (!IsOwner || subscribedToEvents) return;
        GameManager.OnRoundChanged += HandleRoundChanged;
        GameManager.OnRoundCountdown += HandleCountdown;
        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerController.OnStateChanged += HandleStateChange;
        WorldEventManager.OnWorldEventChanged += HandleWorldEventChanged;
        ObjectiveScript.OnHealthChanged += HandleObjectiveHealthChanged;
        ammoManager.OnAmmoChanged += HandleAmmoChanged;
        ammoManager.OnReload += HandleReload;
        playerHealth.OnDeath += HandlePlayerDeath;
        subscribedToEvents = true;
    }
    private void HandlePlayerDeath()
    {
        UnsubscribeToEvents();
        gameUI.gameObject.SetActive(false);
    }
    public void UnsubscribeToEvents()
    {
        if (!IsOwner || !subscribedToEvents)  return;
        GameManager.OnRoundChanged -= HandleRoundChanged;
        GameManager.OnRoundCountdown -= HandleCountdown;
        playerHealth.OnHealthChanged -= HandleHealthChanged;
        playerController.OnStateChanged -= HandleStateChange;
        WorldEventManager.OnWorldEventChanged -= HandleWorldEventChanged;
        ObjectiveScript.OnHealthChanged -= HandleObjectiveHealthChanged;
        ammoManager.OnAmmoChanged -= HandleAmmoChanged;
        ammoManager.OnReload -= HandleReload;
        playerHealth.OnDeath -= HandlePlayerDeath;
        subscribedToEvents = false;
    }
    // Unsubscribe from all events when disconnected
    public override void OnNetworkDespawn() => UnsubscribeToEvents();

    void HandleRoundChanged(int round)
    {   // Update the round label
        if (roundLabel != null) roundLabel.text = $"Round {round}";
    }

    void HandleHealthChanged(float newHealth)
    {   // Update the health label
        if (healthLabel == null) return;
        int health = Mathf.RoundToInt(newHealth);
        healthLabel.text = $"{health}/{playerHealth.maxHealth}";
    }

    void HandleAmmoChanged(int currentAmmo)
    {   // Update the ammo label
        if (ammoLabel == null) return;
        ammoLabel.text = $"{currentAmmo} / {ammoManager.maxAmmo}";
        ammoLabel.style.color = currentAmmo <= 5 ? Color.red : Color.white;
    }

    void HandleReload()
    {   // Update the ammo label to show reloading status
        if (ammoLabel == null) return;
        ammoLabel.text = $"Reloading...";
        ammoLabel.style.color = Color.yellow;
    }

    public void SetGun(GameObject gunGO)
    {   // Subscribe to ammo events
        if (ammoManager != null)
        {
            // Unsubscribe from previous gun events if needed
            ammoManager.OnAmmoChanged -= HandleAmmoChanged;
            ammoManager.OnReload -= HandleReload;
        }

        ammoManager = gunGO.GetComponent<AmmoManager>();

        if (ammoManager != null)
        {
            ammoManager.OnAmmoChanged += HandleAmmoChanged;
            ammoManager.OnReload += HandleReload;
            HandleAmmoChanged(ammoManager.currentAmmo);
        }
    }

    string FormatTime(float time)
    {   // Format the time into minutes and seconds
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    void HandleCountdown(float timeRemaining)
    {   // Start the countdown coroutine
        StartCoroutine(CountdownCoroutine(timeRemaining));
    }

    private IEnumerator CountdownCoroutine(float timeRemaining)
    {   // Update the timer label
        if (timerLabel == null) yield break;

        while (timeRemaining > 0f)
        {   // Update every frame to avoid missing seconds and maximum accuracy
            timerLabel.text = $"{FormatTime(timeRemaining)}";
            timeRemaining -= Time.deltaTime;
            yield return null; // Wait one frame
        }
    }

    private void UpdateMovementIcon(Texture2D tex)
    {   // Update movement icon
        if (iconHolder == null || tex == null) return;
        // Ensure the icon is updated correctly and fits the container
        iconHolder.image = tex;
        iconHolder.style.backgroundImage = new StyleBackground(tex);
        iconHolder.scaleMode = ScaleMode.ScaleToFit;
    }

    void HandleStateChange(PlayerController.MovementState newState)
    {
        if (iconHolder == null) return;
        switch (newState)
        {
            case PlayerController.MovementState.walking:
                UpdateMovementIcon(walkIcon);
                break;
            case PlayerController.MovementState.crouchIdle:
            case PlayerController.MovementState.crouchWalking:
                UpdateMovementIcon(crouchIcon);
                break;
            case PlayerController.MovementState.sprinting:
                UpdateMovementIcon(sprintIcon);
                break;
            default:    // No icons available for other states
                break;
        }
    }

    void HandleWorldEventChanged(WorldEventManager.WorldEventType newEvent)
    {
        if (newEvent == WorldEventManager.WorldEventType.None && eventLabel != null)
        {
            eventLabel.text = "";
            return;
        }
        string input = newEvent.ToString();
        string result = Regex.Replace(input, "(\\B[A-Z])", " $1");
        if (eventLabel == null) return;
            eventLabel.text = timerLabel.text = result;
            StopCoroutine(FadeOutText());
            StartCoroutine(FadeOutText());
    }

    IEnumerator FadeOutText()
    {
        if (eventLabel == null) yield break;

        // Use resolvedStyle to capture the currently applied color (including stylesheets)
        Color originalColor = eventLabel.resolvedStyle.color;
        float duration = Mathf.Max(0.0001f, fadeDuration);
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            Color c = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            eventLabel.style.color = new StyleColor(c);
            yield return null;
        }
        // Ensure fully transparent at the end
        Color final = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        eventLabel.style.color = new StyleColor(final);
    }

   void HandleObjectiveHealthChanged(float newHealth, float maxHealth)
    {
        objectiveHealthLabel.text = $"{Mathf.RoundToInt(newHealth)}/{Mathf.RoundToInt(maxHealth)}";
    }
}
