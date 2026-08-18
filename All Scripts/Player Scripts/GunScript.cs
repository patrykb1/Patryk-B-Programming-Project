using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms.Impl;

public class GunScript : NetworkBehaviour
{
    private PlayerInputHandler playerInput;
    private AmmoManager ammoManager;
    public UnityEvent OnFire;
    public GameObject tracerPrefab;
    public Transform barrel;
    private float nextFireTime = 0f;
    private Transform holder;
    private Transform playerBarrel;
    private bool isAiming = false;
    public bool canShoot = true;


    private void Awake()
    {   // Reference the ammo manager
        if (ammoManager != null) return;
        ammoManager = GetComponent<AmmoManager>();
    }
    // Methods to initialize input handler and gun holder
    public void InitializeInput(PlayerInputHandler inputHandler) => playerInput = inputHandler;
    public void SetHolder(Transform gunHolder) => holder = gunHolder;

    public override void OnNetworkSpawn() => StartCoroutine(WaitForPlayerObject());
    private System.Collections.IEnumerator WaitForPlayerObject()
    {   // Wait for the player object to be assigned
        while (true)
        {   // Keep waiting until the player object is available
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out var client)
                && client.PlayerObject != null)
                break;
            yield return null;
        }
        var player = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject;
        if (player != null)
        {   // Set all script references in the player object
            if (playerInput != null) playerInput = player.GetComponent<PlayerInputHandler>();   // InitializeInput fallback
            if (holder != null) holder = player.transform.
                Find("Visuals/HitmanMesh/mixamorig1:Hips/Real World Gun Holder/Gun Offset");  //SetHolder fallback
            var rigScript = player.transform.GetComponentInChildren<RealWorldRigScript>();
            rigScript.OnWeaponSpawn(gameObject);    // Set weapon reference in rig for hand animations
            var ammoScript = GetComponent<AmmoManager>();
            ammoScript.SetPlayer(player.transform);     // Set player reference in ammo script for UI purposes
            if (!IsOwner) yield return null;
            playerBarrel = GameObject.FindWithTag("Barrel").transform;  // Only set local barrel for owner
        }
        else Debug.LogError($"PlayerObject never spawned for client {OwnerClientId}");
    }

    private void Update()
    {   // Runs every frame
        if (playerInput == null) return;
        bool shootPressed = playerInput.ShootInput;
        bool reloadPressed = playerInput.ReloadInput;  
        isAiming = playerInput.AimInput;    // Take in shoot, aim, and reload inputs
        if (shootPressed && canShoot) FireCheck();  // Shoot
        if (reloadPressed && ammoManager != null) ammoManager.Reload(); // Reload
    }

    private void LateUpdate()
    {   // Set the gun's position
        if (!IsOwner || holder == null) return;
        transform.SetPositionAndRotation(holder.position, holder.rotation); 
    }

    public void FireCheck()
    {   // Check if the gun can fire
        if (ammoManager == null) return;
        if (ammoManager.currentAmmo > 0) Shoot();
    }

    public void Shoot()
    {   // Handle shooting logic
        if (Time.time < nextFireTime || barrel == null) return; // Check if the gun can fire
        float shotDelay = 0.1f;
        nextFireTime = Time.time + shotDelay;
        Vector3 targetPoint;
        Collider targetCollider;
        Ray ray = BulletSpread();   // Add random spread
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {   // If the ray hits something
            targetPoint = hit.point;
            targetCollider = hit.collider;
        }
        else
        {   // If the ray doesn't hit anything
            targetPoint = ray.GetPoint(1000f);
            targetCollider = null;
        }
        FireBulletServerRpc(barrel.position, targetPoint);
        if (targetCollider == null) return;
        if (targetCollider.gameObject.CompareTag("Enemy"))
        {   // If the target is an enemy
            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();
            if (enemy == null) return;
            // If the enemy has an enemy script attached
            enemy.TakeDamage(30);
            int score = enemy.IsDead ? 100 : 30;
            if (IsOwnedByServer)
            {   // Servers cannot call server RPCs
                if (enemy.IsDead) LeaderboardManager.Instance.IncrementKills(OwnerClientId);
                LeaderboardManager.Instance.IncrementScore(OwnerClientId, score);
            }
            else
            {
                if (enemy.IsDead) LeaderboardManager.Instance.IncrementKillsServerRpc(OwnerClientId);
                LeaderboardManager.Instance.IncrementScoreServerRpc(OwnerClientId, score);
            }
        }
        else if (targetCollider.gameObject.CompareTag("EnemyHead"))
        {   // If the target is an enemy head
            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();
            if (enemy == null) return;
            enemy.TakeDamage(30, true);
            if (IsOwnedByServer)
            {   // Servers cannot call server RPCs
                LeaderboardManager.Instance.IncrementKills(OwnerClientId);
                LeaderboardManager.Instance.IncrementScore(OwnerClientId, 150);
            }
            else
            {
                LeaderboardManager.Instance.IncrementKillsServerRpc(OwnerClientId);
                LeaderboardManager.Instance.IncrementScoreServerRpc(OwnerClientId, 150);
            }
        }
        else if (targetCollider.gameObject.CompareTag("Player"))
        {   // Player hit
            PlayerHealth playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();
            playerHealth?.TakeDamageServerRpc(10);
        }
        OnFire?.Invoke();
    }

    private Ray BulletSpread()
    {   // Create a ray with random spread
        Camera cam = Camera.main;
        float radius = isAiming ? 5f : 30f;
        float offsetX = Random.Range(-1f, 1f) * radius;
        float offsetY = Random.Range(-1f, 1f) * radius;
        Vector2 viewportCenter = new (0.5f, 0.5f);  //Midpoint of screen
        Vector2 offset = new (offsetX / Screen.width, offsetY / Screen.height);
        Vector2 targetViewportPoint = viewportCenter + offset;

        return cam.ViewportPointToRay(new Vector3(targetViewportPoint.x, targetViewportPoint.y, 0f));
    }

    private void CreateTracer(Vector3 start, Vector3 end)
    {   // Create a tracer effect
        float tracerDuration = 0.1f;
        if (tracerPrefab == null) return;
        GameObject tracer = Instantiate(tracerPrefab);
        if (tracer.TryGetComponent<LineRenderer>(out var lr))
        {   // Set the start and end positions of the tracer
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }   // Destroy the tracer after a short duration
        Destroy(tracer, tracerDuration);
    }

    [ServerRpc]
    private void FireBulletServerRpc(Vector3 startpoint, Vector3 endpoint)
    {   // Anyone can call this RPC to fire a bullet on all clients
        FireBulletClientRpc(startpoint, endpoint);
    }

    [ClientRpc]
    private void FireBulletClientRpc(Vector3 startpoint, Vector3 endpoint)
    {   // Fire the bullet on all clients
        Vector3 tracerStart;
        if (IsOwner) tracerStart = playerBarrel != null ? playerBarrel.position : startpoint;
        else tracerStart = startpoint;
        CreateTracer(tracerStart, endpoint);
    }
}