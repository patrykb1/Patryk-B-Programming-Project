//DOCUMENTED CODE 
using UnityEngine;

public class KillPlaneScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object has the "Player" tag
        if (other.CompareTag("Enemy"))
        {
            var root = other.transform.root; // Get the root of the hierarchy (in case of nested colliders)
            Enemy enemy = root.GetComponent<Enemy>(); // Try to get the Enemy component from the root
            if (enemy != null)
            {
                enemy.TakeDamage(1000f);
            }
            else
            {
                Debug.LogWarning("Collided object tagged 'Enemy' does not have an Enemy component.");
            }
        }
    }
}
