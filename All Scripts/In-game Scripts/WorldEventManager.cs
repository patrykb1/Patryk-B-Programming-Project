using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class WorldEventManager : NetworkBehaviour
{
    public static WorldEventManager Instance;
    Vector3 defaultGravity = Physics.gravity;
    private int queueRefillThreshold = 5;
    private float linearDampingDefault;
    private float angularDampingDefault;
    private WorldEventType prevEvent;
    public enum WorldEventType
    {
        None,
        LowGravity,
        SuperLowGravity,
        OneHealthPoint,
        IcyFloor,
        ExtremeGravity,
        InvertedCamera,

        /* Ideas:
        HeadshotsOnly: Players can only deal damage with headshots.
        Faster: All players move at increased speed.
        EvenFaster: All players move at greatly increased speed.
        Slower: All players move at decreased speed.
        EvenSlower: All players move at greatly decreased speed.
        DoubleDamage: All players deal double damage.
        MeleeOnly: Players can only deal damage with melee attacks.
        StrongGusts: Periodic strong wind gusts that push players around.
        TraitorPlayer: One player becomes a traitor who has to kill an ally or die.
        FakeHUD: Players' HUDs display false information.
        TunnelVision: Players have reduced FOV and peripheral vision.

        


                    */
    }

    public NetworkVariable<WorldEventType> currentEvent = new NetworkVariable<WorldEventType>(WorldEventType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    Queue<WorldEventType> eventQueue = new();
    public static event Action<WorldEventType> OnWorldEventChanged;

    public override void OnNetworkSpawn()
    {   // Ensures only one instance is active
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!IsServer) return; // Only the server manages events
        currentEvent.Value = WorldEventType.None; // Ensure starting with no event
        eventQueue.Clear(); // Ensures queue is empty
        CheckRefillQueue(); // Initial fill of event queue
        currentEvent.OnValueChanged += OnEventChangedClientRpc;
        GameManager.OnRoundStarted += HandleRoundStart;
        GameManager.OnRoundEnded += HandleRoundEnd;

    }

    public override void OnNetworkDespawn()
    {   // Ensures cleanup on network despawn
        if (!IsServer) return;
        currentEvent.OnValueChanged -= OnEventChangedClientRpc;
        GameManager.OnRoundStarted -= HandleRoundStart;
        GameManager.OnRoundEnded -= HandleRoundEnd;
    }

    private void CheckRefillQueue() // Refills 20 next events without adjacent duplicates
    {
        while (eventQueue.Count < queueRefillThreshold)
        {
            WorldEventType newEvent = GetRandomEvent();
            if (eventQueue.Count == 0) 
            {
                eventQueue.Enqueue(newEvent);
                continue;
            }
            WorldEventType lastEvent = eventQueue.Peek();
            if (newEvent != lastEvent) eventQueue.Enqueue(newEvent); 
        }
    }

    private WorldEventType GetRandomEvent()
    {   // Get a random event from the enum
        int eventCount = Enum.GetValues(typeof(WorldEventType)).Length;
        return (WorldEventType)UnityEngine.Random.Range(1, eventCount); // Exclude 'None'
    }

    [ClientRpc]
    private void OnEventChangedClientRpc(WorldEventType oldEvent, WorldEventType newEvent)
    {   // Inform all clients when the world event changes
        OnWorldEventChanged?.Invoke(newEvent);
        if (!IsServer) return; // Only the server applies event effects
        ApplyEvent(newEvent);
    }

    private void ApplyEvent(WorldEventType newEvent)
    {
        switch (newEvent)
        {
            case WorldEventType.None:
                ResetWorld();
                break;
            case WorldEventType.LowGravity:
                Physics.gravity = defaultGravity * 0.5f; //50% of initial gravity
                break;
            case WorldEventType.SuperLowGravity:
                Physics.gravity = defaultGravity * 0.3f; //30% of initial gravity
                break;
            case WorldEventType.ExtremeGravity:
                Physics.gravity = defaultGravity * 5f; //500% of initial gravity
                break;
            case WorldEventType.OneHealthPoint: // Everyones health is set to 1
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var playerHealth = player.PlayerObject.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.maxHealth = 1f;
                        playerHealth.TakeDamageServerRpc(playerHealth.health.Value - 1); // Set health to 1
                    }
                }
                break;
            case WorldEventType.IcyFloor: // Players slide on the floor
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var rb = player.PlayerObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        linearDampingDefault = rb.linearDamping;
                        angularDampingDefault = rb.angularDamping;
                        rb.linearDamping = 0f;
                        rb.angularDamping = 0f;
                    }
                }
                break;
            case WorldEventType.InvertedCamera: // Camera controls are inverted
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var cameraController = player.PlayerObject.GetComponent<CameraScript>();
                    if (cameraController != null)
                    {
                        cameraController.invertCamera = !cameraController.invertCamera;
                    }
                }
                break;
        }
    }

  
    private void ResetWorld() // Resets all modifiers
    {
        switch (prevEvent)
        {
            case WorldEventType.LowGravity:
            case WorldEventType.SuperLowGravity:
            case WorldEventType.ExtremeGravity: // Reset gravity
                Physics.gravity = defaultGravity;
                break;

            case WorldEventType.OneHealthPoint: // Reset health to default
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var playerHealth = player.PlayerObject.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.maxHealth = 100f;
                        playerHealth.HealServerRpc(playerHealth.maxHealth); 

                    }
                }
                break;

            case WorldEventType.IcyFloor:   // Reset icy floor effect
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var rb = player.PlayerObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearDamping = linearDampingDefault;
                        rb.angularDamping = angularDampingDefault;
                    }
                }
                break;

            case WorldEventType.InvertedCamera: // Reset camera controls
                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var cameraController = player.PlayerObject.GetComponent<CameraScript>();
                    if (cameraController != null)
                    {
                        cameraController.invertCamera = !cameraController.invertCamera;
                    }
                }
                break;
        }
    }

    private void HandleRoundStart()
    {   // Get a new event from the queue
        if (!IsServer || !IsSpawned) return;
        if (eventQueue.Count == 0)
        {   // Ensure queue is filled
            CheckRefillQueue();
            if (eventQueue.Count == 0) return;
        }
        WorldEventType newEvent = eventQueue.Dequeue();
        currentEvent.Value = newEvent;

    }

    private void HandleRoundEnd()
    {
        if (!IsServer || !IsSpawned) return;
        prevEvent = currentEvent.Value; // Store current event before resetting
        currentEvent.Value = WorldEventType.None; // End any active event at round end
        CheckRefillQueue(); // Refill event queue for next round
    }
}
