using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    bool connecting = false;
    public GameObject lobbyItemPrefab;
    public Transform lobbyListContainer;

    public GameObject playerIdPrefab;
    public Transform playerListContainer;

    private List<GameObject> playerList = new();
    private List<GameObject> lobbyItems = new List<GameObject>();

    private Lobby currentLobby;
    public TMP_Text lobbyIdText;
    private LobbyEventCallbacks lobbyCallbacks;
    private Allocation hostAllocation;
    private string hostJoinCode;
    private bool isSendingHeartbeat = false;
    private CancellationTokenSource heartbeatCancellation;
    public GameObject lobbyEditorPanel;
    public PopupUI popupUI;
    private bool leftLobbyWillingly = false;
    bool servicesInitialized = false;
    private string previousHostId = "";
    public GameObject lobbyNameEditor;
    public Button readyButton;
    public TMP_Text deviceId;
    private bool initializingServices = false;
    private bool localClientConnectedHandlerRegistered = false;

    public static LobbyManager Instance;

    private void Awake()
    {   // Ensure only one instance of lobby manager exists
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    // Ensure references are reloaded when the scene is loaded
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {   // Reload references and initialize services
            FindFirstObjectByType<ReferenceReloader>().ReloadLobbyManager();
            if (!servicesInitialized) Start();
        }
        if (scene.name == "nature scene" && !string.IsNullOrEmpty(currentLobby?.Id))
        {   // Just in case, leave the lobby after the game (nature) scene loads    
            StartCoroutine(LeaveLobbyAfterSceneLoad(3f));
        }
    }
    private IEnumerator LeaveLobbyAfterSceneLoad(float delaySeconds)
    {   // Wait (delaySeconds) seconds and leave the lobby
        yield return new WaitForSeconds(delaySeconds);
        if (currentLobby != null) LeaveLobby();
    }

    async void Start()
    {   // Initialize Unity Services
        if (servicesInitialized) return;
        try
        {   // Check if services are already initializing
            if (initializingServices) return;
            initializingServices = true;
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            servicesInitialized = true;
            if (deviceId == null) return;
            deviceId.text = $"Player device ID: {AuthenticationService.Instance.PlayerId}";
        }
        catch (System.Exception ex)
        {   // Handle initialization errors
            Debug.LogError($"Failed to initialize Unity Services: {ex.Message}");
            popupUI?.ShowMessage($"Failed to initialize services, error message: {ex.Message}. Please try again later.");
        }
    }

    public async void CreateLobby()
    {
        if (!servicesInitialized)
        {   // Can't create lobby if services aren't initialized
            Debug.LogWarning("Unity Services not ready yet!");
            popupUI.ShowMessage("Services are still initializing. Please wait...");
            return;
        }
        string lobbyName = "MyLobby"; // Default lobby name
        const int maxPlayers = 6;   // Maximimum number of players (6) in one lobby
        CreateLobbyOptions options = new CreateLobbyOptions
        {   // Set up lobby options, no relay code until the host attempts to start the game
            Data = new Dictionary<string, DataObject>
            {
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "") },
                { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") }
            }
        };
        options.IsPrivate = false;  // Ensure all players can see options
        if (currentLobby != null)
        {   // Can only be in one lobby at a time
            Debug.LogWarning("Already in a lobby. Please leave the current lobby first.");
            popupUI.ShowMessage("Already in a lobby. Please leave the current lobby first.");
            return;
        }
        try
        {   // Attempt to create the lobby
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            previousHostId = currentLobby.HostId;
            await SubscribeToLobbyEvents();
            Debug.Log($"Lobby created with ID: {currentLobby.Id}");
            await CreatePlayerData();
            lobbyEditorPanel.SetActive(true);   // Ensure panel is visible
            lobbyNameEditor.SetActive(true);
            lobbyIdText.text = currentLobby.Id;
            readyButton.GetComponentInChildren<TMP_Text>().text = "START GAME"; // Host does not need to ready up, only start the game
            StartHeartbeat();
            ListPlayers();
            RegisterLocalClientConnectedHandler();
        }
        catch (System.Exception ex) { Debug.LogError($"Failed to create lobby: {ex.Message}"); }  // Log error for debug purposes
    }

    private void RegisterLocalClientConnectedHandler()
    {   // Register the local client connected handler
        if (localClientConnectedHandlerRegistered) return;
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnLocalClientConnected;
        localClientConnectedHandlerRegistered = true;
    }
    private void UnregisterLocalClientConnectedHandler()
    {   // Unregister the local client connected handler
        if (!localClientConnectedHandlerRegistered) return;
        if (NetworkManager.Singleton == null) { localClientConnectedHandlerRegistered = false; return; }
        NetworkManager.Singleton.OnClientConnectedCallback -= OnLocalClientConnected;
        localClientConnectedHandlerRegistered = false;
    }
    private void OnLocalClientConnected(ulong clientId)
    {   // Once the client connects, unregister the handler
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        UnregisterLocalClientConnectedHandler();
    }

    public async void ListLobbies()
    {   // List all available lobbies
        try
        {   // Clear existing lobby items
            foreach (var item in lobbyItems) Destroy(item);
            lobbyItems.Clear();
            var lobbyList = await LobbyService.Instance.QueryLobbiesAsync();
            foreach (var lobby in lobbyList.Results)
            {   // Generate new lobby items and set them up
                Debug.Log($"Lobby ID: {lobby.Id}, Name: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}");
                GameObject lobbyItem = Instantiate(lobbyItemPrefab, lobbyListContainer);
                lobbyItem.GetComponent<LobbyItemScript>().Setup(lobby, this);
                lobbyItems.Add(lobbyItem);
            }
            Debug.Log($"Total lobbies found: {lobbyList.Results.Count}");
        }
        catch (System.Exception ex)
        {   // Log errors
            Debug.LogError($"Failed to list lobbies: {ex.Message}");
        }
    }
    
    public async void JoinLobby(string lobbyId)
    {   // Join a lobby by ID
        currentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        if (currentLobby == null) return; // Can't join lobby if not found
        previousHostId = currentLobby.HostId;
        string playerId = AuthenticationService.Instance.PlayerId;
        if (currentLobby.Players.Exists(p => p.Id == playerId)) return; // Can't join lobby if already in lobby
        currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        await SubscribeToLobbyEvents();
        Debug.Log("Joined lobby: " + currentLobby.Id);
        await CreatePlayerData();
        lobbyEditorPanel.SetActive(true);   // Ensure panel is active, but not the name editor as not the host
        lobbyNameEditor.SetActive(false);
        lobbyIdText.text = currentLobby.Id;
        readyButton.GetComponentInChildren<TMP_Text>().text = "READY UP!";
        readyButton.onClick.RemoveAllListeners();   // Clear previous assignments to prevent null errors
        readyButton.onClick.AddListener(async () =>
        {   // Handle ready button click
            var player = currentLobby.Players.Find(p => p.Id == playerId);
            if (player == null) return; // Can't ready up if player is not found
            bool isReady = player.Data != null && player.Data.TryGetValue("Ready", out PlayerDataObject readyData)&& bool.TryParse(readyData.Value, out bool val) 
                && val; 
            var updateOptions = new UpdatePlayerOptions
            {   // Update player options to reflect readiness
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, (!isReady).ToString()) },
                    { "Username", player.Data.TryGetValue("Username", out PlayerDataObject usernameData)
                        ? usernameData
                        : new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayfabLogin.Instance.username)
                    }
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, player.Id, updateOptions);
            ListPlayers();  // ListPlayers refreshes player list to show new ready status
        });
        ListPlayers();  // Display all players after joining 
    }

    private async void TryJoinRelay(string relayCode)
    {   // Try to join the relay server when the host presses start game
        if (connecting || NetworkManager.Singleton.IsConnectedClient) return;   // Prevent multiple connections
        connecting = true;
        relayCode = relayCode?.Trim();  // Removes start/end whitespace
        int attempts = 0;
        const int maxAttempts = 10; // ten tries to join the relay
        while (attempts < maxAttempts)
        {   // Retry loop
            try
            {   // Attempt to join the relay
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
                await Task.Delay(200);  // Small delay for clients to process relay data
                bool started = NetworkManager.Singleton.StartClient();
                Debug.Log("StartClient result: " + started);
                connecting = false;
                return;
            }
            catch (System.Exception ex)
            {
                // "Not Found" error indicates the join code has not updated on the server yet, try again
                if (ex.Message != null && ex.Message.Contains("Not Found"))
                {
                    Debug.LogWarning($"Join code not found (attempt {attempts + 1}/{maxAttempts}), retrying: {relayCode}");
                    await Task.Delay(500);
                    await RefreshLobbyAsync();
                    currentLobby.Data.TryGetValue("RelayCode", out DataObject rd);
                    relayCode = rd?.Value?.Trim();  // Grab the relay code again
                    attempts++;
                    continue;
                }
                Debug.LogError("Failed to join relay: " + ex.Message);  // If the error is not a Not Found error, don't try again
                connecting = false;
                return;
            }
        }
        Debug.LogError("Failed to join relay after retries.");
        connecting = false;  // Allow new connections
    }

    private async void OnLobbyChanged(ILobbyChanges changes)
    {   // Callback to when the lobby changes
        Debug.Log("Lobby changed event received");
        if (currentLobby == null) return;
        await RefreshLobbyAsync();
        string newHostId = currentLobby.HostId;
        if (newHostId != previousHostId)
        {   // Host has changed
            Debug.Log($"Host changed from {previousHostId} to {newHostId}");
            if (newHostId == AuthenticationService.Instance.PlayerId)
                popupUI.ShowMessage("You are now the host of the lobby");
            previousHostId = newHostId;
            return;
        }
        if (currentLobby.Data.TryGetValue("RelayCode", out DataObject relayData))
        {   // Relay code exists: game has started
            string relayCode = relayData.Value;
            if (!string.IsNullOrEmpty(relayCode))
            {   // Try to join the relay server
                TryJoinRelay(relayCode);
                return;
            }
        }
    }
    // Start Button Pressed: run Task TryGameStart()
    public void OnStartPressed() => _ = TryGameStart();

    private async Task TryGameStart()
    {   // Try starting game 
        if (currentLobby == null) return; // Can't start game if not a lobby
        if (currentLobby.HostId != AuthenticationService.Instance.PlayerId) return; // Only the host can start the game
        bool allReady = currentLobby.Players.TrueForAll(p =>
            p.Data != null &&
            p.Data.TryGetValue("Ready", out PlayerDataObject readyData) &&
            bool.TryParse(readyData.Value, out bool isReady) && isReady
        );
        if (!allReady) return; // Don't start if everyone isn't ready
        await TryHostRelay();
        StartCoroutine(WaitForPlayersThenLoad(currentLobby.Players.Count));
    }

    private async Task TryHostRelay()
    {
        try
        {
            int playerCount = currentLobby.Players.Count;
            hostAllocation = await RelayService.Instance.CreateAllocationAsync(playerCount);    // Create a relay server with playerCount spaces
            hostJoinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);   // Get the join code for the relay server
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(hostAllocation, "dtls"));    // Use DTLS protocol
            bool hostStarted = NetworkManager.Singleton.StartHost();    // Start the host
            Debug.Log("Immediate host start result: " + hostStarted);
            var initialData = new Dictionary<string, DataObject>
                {   // Publish Relay join code without starting the game
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, hostJoinCode) },
                    { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") }
                };
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions { Data = initialData });
            Debug.Log("Published host relay join code");
        }
        catch (System.Exception ex)
        {   // Log errors with starting game
            Debug.LogWarning("Failed to start host immediately: " + ex.Message);
        }
    }

    IEnumerator WaitForPlayersThenLoad(int expectedPlayers)
    {   // Wait for all players to load
        while (NetworkManager.Singleton.ConnectedClientsIds.Count < expectedPlayers) yield return null;
        Debug.Log("All players connected, loading scene!");
        NetworkManager.Singleton.SceneManager.LoadScene("nature scene", LoadSceneMode.Single);
        // Start background deletion that runs after the scene has changed and players had time to load.
        StartCoroutine(DeleteLobbyAfterSceneLoad(10f));
    }

    private IEnumerator DeleteLobbyAfterSceneLoad(float graceSeconds)
    {   // Wait a bit to give clients time to load the scene.
        yield return new WaitForSeconds(graceSeconds);
        // Perform cleanup 
        var cleanupTask = CleanupLobbyAsync();
        yield return new WaitUntil(() => cleanupTask.IsCompleted);
    }

    private async Task CleanupLobbyAsync()
    {   // Cleanup lobby resources
        try
        {   
            heartbeatCancellation?.Cancel();    // Kill the lobby
            UnsubscribeFromLobbyEvents();   // Unsubscribe from lobby events
            currentLobby = null;    // Ensure all lobby variables are set to null
            hostAllocation = null;
            hostJoinCode = null;
            foreach (var item in playerList) if (item) Destroy(item);   // Destroy all non-null player items
            playerList.Clear();
            foreach (var item in lobbyItems) if (item) Destroy(item);   // Destroy all non-null lobby items
            lobbyItems.Clear();
        }
        catch (System.Exception ex){ Debug.LogWarning("Failed to clear lobby UI/state: " + ex.Message);}
        await Task.CompletedTask;
    }

    public void ListPlayers()
    {   // List all players in the lobby
        if (currentLobby == null || currentLobby.Players == null) return;   // Can't list players if there are no players or if there is no lobby
        foreach (var item in playerList) Destroy(item); // Destroy all player gameobjects
        playerList.Clear(); // Clear the player list
        foreach (var player in currentLobby.Players)
        {
            if (player == null || player.Data == null) continue;    // Skip invalid players
            GameObject playerItem = Instantiate(playerIdPrefab, playerListContainer);   // Create a player item in the player container
            playerItem.GetComponent<PlayerLobbyUIScript>().Setup(currentLobby, player, this);   // Setup the player item (name and kick button)
            playerList.Add(playerItem); // Add this item to the list to track
        }
    }

    private async Task CreatePlayerData()
    {   // Create data for the new player
        var playerData = new Dictionary<string, PlayerDataObject>
        {   // Store player username and readiness status
            { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId == currentLobby.HostId ? "true" : "false")},
            { "Username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayfabLogin.Instance.username) }
        };
        var updateOptions = new UpdatePlayerOptions { Data = playerData };  // Update the player's options
        await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, updateOptions);
    }

    private async Task RefreshLobbyAsync()
    {   // Refresh the lobby data and relist players
        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        ListPlayers();
    }

    private async void StartHeartbeat()
    {   // Start the heartbeat to prevent disconnection
        if (isSendingHeartbeat) return; // Ensures one heartbeat at a time
        isSendingHeartbeat = true;
        heartbeatCancellation = new CancellationTokenSource();
        var token = heartbeatCancellation.Token;
        try
        {   // Send heartbeat pings
            while (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId && !token.IsCancellationRequested)
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                await Task.Delay(15000, token);
            }
        }
        catch (TaskCanceledException) { }   // Ignore cancellation error (not really an error if it is called as intended)
        catch (System.Exception e) { Debug.LogWarning("Heartbeat failed: " + e.Message); }
        isSendingHeartbeat = false;
    }

    private async Task SubscribeToLobbyEvents()
    {   // Subscribe to lobby events
        lobbyCallbacks = new LobbyEventCallbacks();
        lobbyCallbacks.LobbyChanged += OnLobbyChanged;
        lobbyCallbacks.KickedFromLobby += OnKicked;
        lobbyCallbacks.LobbyDeleted += OnLobbyDeleted;
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(currentLobby.Id, lobbyCallbacks);
    }

    private void UnsubscribeFromLobbyEvents()
    {   // Unsubscribe from lobby events
        if (lobbyCallbacks == null) return;
        lobbyCallbacks.LobbyChanged -= OnLobbyChanged;
        lobbyCallbacks.KickedFromLobby -= OnKicked;
        lobbyCallbacks.LobbyDeleted -= OnLobbyDeleted;
        lobbyCallbacks = null;
    }

    private void OnKicked()
    {   // Handle being kicked from the lobby
        if (leftLobbyWillingly) { leftLobbyWillingly = false; return; }
        Debug.Log("You were kicked from the lobby");
        popupUI.ShowMessage("You were kicked from the lobby");
        lobbyEditorPanel.SetActive(false);
        UnsubscribeFromLobbyEvents();
        currentLobby = null;
    }

    private void OnLobbyDeleted()
    {   // Handle lobby deletion
        Debug.Log("Lobby was deleted");
        UnsubscribeFromLobbyEvents();
        currentLobby = null;
    }

    public async void LeaveLobby()
    {   // Leave the current lobby
        if (currentLobby == null) return;
        try
        {   // Leave the lobby
            leftLobbyWillingly = true;
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            Debug.Log("Left lobby: " + currentLobby.Id);
            lobbyEditorPanel?.SetActive(false);
            currentLobby = null;
            heartbeatCancellation?.Cancel();    // Only cancel heartbeat if it was started
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to leave lobby: {ex.Message}");
        }
    }
    public async void KickPlayer(string lobbyId, string deviceId)
    {   // Kick a player from the lobby by deviceId
        string selfPlayerId = AuthenticationService.Instance.PlayerId;
        if (currentLobby == null) return; // Can't kick from lobby if not in one
        if (deviceId == selfPlayerId) return; // Can't kick yourself
        try
        {   // Remove player
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, deviceId);
            Debug.Log($"Player {deviceId} kicked from lobby {lobbyId}");
            ListPlayers(); // Refresh player list
        }
        catch (System.Exception ex)
        {   // Handle errors
            Debug.LogError($"Failed to kick player: {ex.Message}");
        }
    }
    // Called by the text input field once the player submits a new name
    public void EditLobbyName(string newName) => _ = EditLobbyNameAsync(newName);

    public async Task EditLobbyNameAsync(string newName)
    {
        if (currentLobby == null) return;
        try
        {
            // Update the lobby's name on the server
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Name = newName
                }
            );
            currentLobby = updatedLobby;
            Debug.Log($"Lobby name updated to: {updatedLobby.Name}");   // Ensure correct change through logs
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby name: {e}");
        }
    }
    // Unsubscribe from lobby events when the object is destroyed
    private void OnDestroy() => UnsubscribeFromLobbyEvents();
    
}