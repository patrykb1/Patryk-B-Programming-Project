using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankEnemy : Enemy
{
    [SerializeField] public float armour = 51f;
    [SerializeField] public float damageReduction = 0.5f;
    bool hasArmour = true;
    bool isAttacking = false;
    [SerializeField] private BladeHitbox[] bladeHitboxes;
    public HashSet<Transform> hitPlayers = new();

    // Keep these the same to ensure base logic works as intended
    protected override void Start()
    {
        attackCooldown = 80f / 30f;
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }

    public override void TakeDamage(float amount, bool headshot = false)
    {   // Take damage works differently since the tank has armour that takes damage first
        if (hasArmour)
        {   // Armour takes reduced damage
            armour -= amount * damageReduction;
            if (armour <= 0) hasArmour = false;
        }
        else currentHealth -= amount;
        if (currentHealth <= 0 || headshot) DieServerRpc();
    }

    protected override void TryAttack()
    {   // Same base logic, but tank enemy calls PerformMeleeAttack() instead of Attack()
        if (!target) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange || Time.time < lastAttackTime + attackCooldown || isAttacking) return;
        isAttacking = true;
        lastAttackTime = Time.time;
        enemyAnimator.SetTrigger("Attack");
        StartCoroutine(nameof(PerformMeleeAttack));
    }

    private IEnumerator PerformMeleeAttack()
    {   // Performs the slashing attack
        canMove = false;
        int lifetimeFrames = 2; // How many frames each hitbox stays active
        yield return new WaitForSeconds(35f / 30f); // Wait for the attack animation to reach the hit frame
        hitPlayers.Clear();
        // Make sure all hitboxes start disabled
        foreach (var hb in bladeHitboxes) hb.DisableHitbox();
        for (int i = 0; i < bladeHitboxes.Length; i++)
        {   // Enable current hitbox
            bladeHitboxes[i].EnableHitbox();
            // If a previous hitbox has exceeded its lifetime, disable it
            int toDisable = i - lifetimeFrames;
            if (toDisable >= 0) bladeHitboxes[toDisable].DisableHitbox();
            // Wait exactly 1 frame before spawning the next
            yield return null;
        }
        // Cleanup: disable any remaining active hitboxes
        for (int i = bladeHitboxes.Length - lifetimeFrames; i < bladeHitboxes.Length; i++)
        {
            if (i >= 0 && i < bladeHitboxes.Length) bladeHitboxes[i].DisableHitbox();
            yield return null;
        }
        yield return new WaitForSeconds((80f - 45f) / 30f); // Wait for the rest of the attack animation
        isAttacking = false; canMove = true;
    }
}

