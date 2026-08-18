using System;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Regeneration")]
    [SerializeField] private float regenRate = 5f;        // health per second
    [SerializeField] private float regenDelay = 3f;       // delay after taking damage
    private float lastDamageTime;
    public NetworkVariable<float> health = new(100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float maxHealth = 100f;
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;
    public bool IsAlive => health.Value > 0f;
    public override void OnNetworkSpawn()
    {
        health.OnValueChanged += HandleHealthChanged;
    }
    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
    }
    private void Update()
    {
        if (!IsServer) return; // Only server modifies health
        if (!IsAlive) return;  // Don't regen if dead
        if (health.Value >= maxHealth) return;

        // Wait for regen delay after taking damage
        if (Time.time < lastDamageTime + regenDelay) return;

        health.Value += regenRate * Time.deltaTime;
        health.Value = Mathf.Min(health.Value, maxHealth);
    }
    private void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue);
        Debug.Log($"{OwnerClientId} health changed from {oldValue} to {newValue}");
        if (newValue <= 0f && oldValue > 0f)
        {
            Die();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float damage)
    {
        health.Value -= damage;
        Debug.Log($"{OwnerClientId} took {damage} damage. Current health: {health.Value}");
        lastDamageTime = Time.time;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HealServerRpc(float amount)
    {
        health.Value = Mathf.Min(health.Value + amount, maxHealth);
    }
    public void Die()
    {
        if (!IsServer) return;
        health.Value = 0f;
        Debug.Log($"{OwnerClientId} has died.");
        DeathRoutineClientRpc();
    }
    [ClientRpc]
    private void DeathRoutineClientRpc()
    {
        if (!IsOwner) return;
        OnDeath?.Invoke();

    }


}
