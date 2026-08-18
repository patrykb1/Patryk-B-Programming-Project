using UnityEngine;

public class FastEnemy : Enemy
{
    private bool readyToDash = true;
    protected override void Start()
    {
        moveSpeed = 7f; // Set a higher speed for the fast enemy
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
        float random = Random.Range(0f, 1f);
        if (random < 0.001f && readyToDash) // 0.1% chance each frame to dash
        {
            DashTowardsTarget();
            readyToDash = false;
        }
    }

    public void DashTowardsTarget()
    {
        if (!target) return; // Can't dash towards target if there is no target
        moveSpeed = 20f; // Temporarily increase speed for dash
        Invoke(nameof(ResetSpeed), 0.5f); // Reset speed after 0.5 seconds
    }

    public override void TakeDamage(float amount, bool isHeadshot = false)
    {
        float random = Random.Range(0f, 1f);
        if (random < 0.2f) return; // 20% chance to evade damage
        base.TakeDamage(amount, isHeadshot);
    }
    private void ResetSpeed()
    {
        moveSpeed = 7f;
        readyToDash = true;
    }
}