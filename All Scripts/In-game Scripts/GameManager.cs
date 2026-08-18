using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    [SerializeField] private List<GameObject> enemyPrefabs; // Assign enemy prefabs in Inspector
    [SerializeField] private Transform spawnAreaCenter;     // Center point of spawn area
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(0.1f, 0f, 0.1f); // Width/Length of spawn area

    private List<GameObject> activeEnemies = new List<GameObject>();
    public static event Action<int> OnRoundChanged;
    public static event Action<float> OnRoundCountdown; // Countdown in seconds
    public static event Action OnRoundEnded;
    public static event Action OnRoundStarted;
    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1);


    public override void OnNetworkSpawn()
    {   // Set up instance and network events
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        CurrentRound.OnValueChanged += OnRoundValueChanged;
        if (IsServer) Enemy.OnEnemyKilled += HandleEnemyKilled;
        StartCoroutine(LateJoinerSyncRound());
    }

    private IEnumerator LateJoinerSyncRound()
    {   // Wait a frame to ensure UI is ready
        yield return new WaitForSeconds(1f);
        OnRoundChanged?.Invoke(CurrentRound.Value);
    }

    public override void OnNetworkDespawn()
    {   // Clean up network events
        if (IsServer) Enemy.OnEnemyKilled -= HandleEnemyKilled;
        CurrentRound.OnValueChanged -= OnRoundValueChanged;
    }
    public void StartGame() => StartRound(CurrentRound.Value);
    public void StartRound(int round) => StartCoroutine(RoundRoutine(round));
    int GetEnemyCountForRound(int round) => 5 + 3 * round;

   private void SpawnEnemies(int count)
    {   // Spawns {count} random enemies at random locations
        for (int i = 0; i < count; i++)
        {   //Calculate spawn position
            GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Count)];
            Vector2 randomPoint = UnityEngine.Random.insideUnitCircle.normalized *
                Mathf.Sqrt(UnityEngine.Random.Range(20f * 20f, 50f * 50f));
            Vector3 rayOrigin = spawnAreaCenter.position + new Vector3(randomPoint.x, 100f, randomPoint.y);
            Vector3 spawnPos = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 200f,
            LayerMask.GetMask("Ground"))
                ? hit.point
                : spawnAreaCenter.position + new Vector3(randomPoint.x, 0f, randomPoint.y);
            // Spawn enemy if correctly instantiated
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
            else { Destroy(enemy); continue; }
            activeEnemies.Add(enemy);
        }
    }
    public void EndRound()
    {   if (!IsServer) return;
        OnRoundEnded?.Invoke();
        foreach (var enemy in activeEnemies) enemy?.GetComponent<NetworkObject>().Despawn(true);
        activeEnemies.Clear();
        CurrentRound.Value++;
        StartRound(CurrentRound.Value);
    }   
    private IEnumerator RoundRoutine(int round)
    {   // Coroutine that handles what happens before every round
        int enemyCount = GetEnemyCountForRound(round);
        float countdownDuration = 5f;
        OnRoundCountdown?.Invoke(countdownDuration);
        yield return new WaitForSeconds(countdownDuration);
        OnRoundStarted?.Invoke();
        SpawnEnemies(enemyCount);
    }
    void OnRoundValueChanged(int oldRound, int newRound) => OnRoundChanged?.Invoke(newRound);
    private void HandleEnemyKilled(Enemy enemy)
    {
        activeEnemies.Remove(enemy.gameObject);
        if (activeEnemies.Count <= 0) EndRound();
    }
}