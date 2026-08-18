using Unity.Netcode;
using UnityEngine;

public class BladeHitbox : MonoBehaviour
{
    public float damage = 20f;
    private TankEnemy ownerEnemy;
    private CapsuleCollider hitboxCollider;

    public void EnableHitbox() => hitboxCollider.enabled = true;
    public void DisableHitbox() => hitboxCollider.enabled = false;

    private void Start()
    {   // Caching references
        ownerEnemy = transform.root.GetComponent<TankEnemy>();
        hitboxCollider = GetComponent<CapsuleCollider>();
        DisableHitbox();
    }
    private void OnTriggerEnter(Collider target)
    {
        if (target.transform.root.CompareTag("Objective"))
        {   // Objective hit
            target.transform.root.GetComponent<ObjectiveScript>().TakeDamage(damage);
        }
        // Check if the collider has a PlayerHealth component
        if (target.TryGetComponent<PlayerHealth>(out var playerHealth) 
        && !ownerEnemy.hitPlayers.Contains(target.transform))
        {   // Player hit
            ownerEnemy.Attack(target.transform, damage);
            ownerEnemy.hitPlayers.Add(target.transform);
        }
    }
}