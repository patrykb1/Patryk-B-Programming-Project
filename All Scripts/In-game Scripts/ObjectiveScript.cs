using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using System;

public class ObjectiveScript : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 1000f;

    public static event Action<float, float> OnHealthChanged;
    public static event Action OnObjectiveDestroyed;

    private NetworkVariable<float> currentHealth = new(
        writePerm: NetworkVariableWritePermission.Server
    );

    public static ObjectiveScript Instance;

    private void Awake()
    {   //Ensure only one instance of the objective exists
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {  // Set initial health
        if (IsServer) currentHealth.Value = maxHealth;
        currentHealth.OnValueChanged += OnHealthUpdated;
    }

    public override void OnNetworkDespawn()
    {   //Subscribe and unsubscribe to health updated
        currentHealth.OnValueChanged -= OnHealthUpdated;
    }

    private void OnHealthUpdated(float oldValue, float newValue)
    {   //Invoke health changed event
        OnHealthChanged.Invoke(newValue, maxHealth);
        //Destroy objective if health is zero
        if (newValue <= 0f)
            HandleDestroyed();
    }

    public void TakeDamage(float amount)
    {   // Only the server can deal damage
        if (!IsServer || amount <= 0f || currentHealth.Value <= 0f)
            return;

        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
    }

    public float GetCurrentHealth() => currentHealth.Value;
    public float GetMaxHealth() => maxHealth;

    private void HandleDestroyed()
    {   //Invoke objective destroyed event
        OnObjectiveDestroyed.Invoke();
    }
}