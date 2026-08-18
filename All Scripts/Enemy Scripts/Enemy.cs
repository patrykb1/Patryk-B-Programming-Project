using System;
using System.Collections;
using System.IO;
using Unity.Jobs;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.CullingGroup;
public class Enemy : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    protected float currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    protected Transform target = null;
    private float detectionRange = 50f;
    [Tooltip("Seconds between target detection checks to reduce physics queries")]
    [SerializeField] private float detectionTickRate = 0.2f;
    private float detectionTimer = 0f;
    [SerializeField] private LayerMask playerLayerMask;
    public bool canMove = true;

    [Header("Attack")]
    public float damage = 10f;
    public float attackRange = 1f;
    protected float attackCooldown = 1.5f;
    protected float lastAttackTime;
    bool pathFinished = true;

    Vector3[] path;
    int targetIndex;
    protected Rigidbody rb;
    public Animator enemyAnimator;
    protected Vector3 lastPosition;
    private Vector3 desiredMoveDirection = Vector3.zero;
    private bool isDirectChase = false;
    private Transform objective;
    [SerializeField] private LayerMask groundLayer;
    private Collider[] overlapResults = new Collider[20];
    Vector3 groundTargetPos;
    public bool IsDead => currentHealth <= 0;
    public enum EnemyState
    {
        Idle = 0,
        Chasing = 1,
    } 
    public NetworkVariable<EnemyState> State = new( EnemyState.Chasing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public static event Action<Enemy> OnEnemyKilled;
    protected virtual void Start()
    {
        objective = GameObject.FindGameObjectWithTag("Objective").transform;
        groundLayer = LayerMask.GetMask("Ground");
        playerLayerMask = LayerMask.GetMask("Player");

    }
    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += OnStateChanged;
        if (!IsServer) return;
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= OnStateChanged;
    }

    void OnStateChanged(EnemyState oldState, EnemyState newState)
    {
        enemyAnimator.SetInteger("State", (int)newState);
    }

    protected virtual void Update()
    {
        if (!IsServer || !IsSpawned) return;// Run target detection on a reduced tick rate to cut down physics queries
        if (transform.position.y < -10f)
        {
            Debug.LogWarning($"{name} fell out of the world and will be destroyed.");
            DieServerRpc();
            return;
        }
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            FindTarget();
            detectionTimer = detectionTickRate;
        }
        EnemyStateHandler();
        if (target == null) return;
        float distSq = (target.position - transform.position).sqrMagnitude;
        float attackRangeSq = attackRange * attackRange;
        float detectionRangeSq = detectionRange * detectionRange;

        FaceTarget();

        // Attack
        if (distSq <= attackRangeSq)
        {
            TryAttack();
            return;
        }

        if (!canMove) return;

        // Direct chase (player inside detection range)
        if (distSq <= detectionRangeSq)
        {
            StopPathIfRunning();
            // compute desired direction and let FixedUpdate apply movement for physics consistency
            Vector3 dir = (target.position - rb.position);
            dir.y = 0f;
            dir.Normalize();
            desiredMoveDirection = dir;
            isDirectChase = true;
        }
        // Pathfind (target outside detection range = objective)
        else
        {
            isDirectChase = false;
            RequestPathIfNeeded();
        }

        SpeedControl();
    }
    private void StopPathIfRunning()
    {
        if (!pathFinished)
        {
            StopCoroutine(nameof(FollowPath));
            pathFinished = true;
        }
    }
    private void RequestPathIfNeeded()
    {
        if (pathFinished)
        {
            // Use the ground-projected target position for pathfinding so paths are generated on walkable surfaces
            PathRequestManager.RequestPath(
                transform.position,
                groundTargetPos,
                OnPathFound
            );
        }
    }
    protected virtual void FixedUpdate()
    {
        lastPosition = transform.position;
        // Apply direct chase movement here so physics collision works properly
        if (isDirectChase && rb != null)
        {
            rb.MovePosition(rb.position + desiredMoveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    protected void FindTarget()
    {
        // Use a layer mask to only query player colliders when possible
        int layerMask = playerLayerMask != 0 ? (int)playerLayerMask : ~0;
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRange,
            overlapResults,
            layerMask
        );

        Transform closestPlayer = null;
        float closestDistSq = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;

            if (!col.CompareTag("Player")) continue;

            float distSq = (col.transform.position - myPos).sqrMagnitude;

            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestPlayer = col.transform;
            }
        }
        if (closestPlayer != null)
        {
            target = closestPlayer;
            // project target onto ground for pathfinding
            groundTargetPos = target.position;
            if (Physics.Raycast(target.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f, groundLayer))
            {
                groundTargetPos = hit.point;
            }

        }
        else
        {
            target = objective;
            groundTargetPos = target != null ? target.position : transform.position;
        }
    }
    void FaceTarget()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // stop tilting up/down

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * 8f // turn speed
        );
    }
    protected virtual void TryAttack()
    {
        if (!target) return;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown && pathFinished)
        {
            Attack(10f);
            lastAttackTime = Time.time;
            enemyAnimator.SetTrigger("Attack");
        }
    }

    public void Attack(Transform _target, float damage)
    {
        // Example: damage player
        Debug.Log($"{name} attacks for {damage} damage");
        var playerHealth = _target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamageServerRpc(damage);
        }
    }

    // Overload for default target usage:
    protected virtual void Attack(float damage)
    {
        Attack(target, damage);
    }

    public virtual void TakeDamage(float amount, bool headshot=false)
    {
        currentHealth -= amount;

        if (currentHealth <= 0 || headshot)
            DieServerRpc();
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]

    protected virtual void DieServerRpc()
    {

        if (!IsServer) return;
        OnEnemyKilled?.Invoke(this);
        NetworkObject.Despawn(true);
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (!pathSuccessful)
        {
            Debug.LogWarning($"{nameof(Unit)} could not find a path to the target.", this);
            return;
        }
        if (newPath == null)
        {
            Debug.LogWarning($"{nameof(Unit)} received a null path.", this);
            return;
        }
        if (newPath.Length == 0)
            {
            Debug.LogWarning($"{nameof(Unit)} received an empty path.", this);
            return;
        }
        if (!pathSuccessful || newPath == null || newPath.Length == 0)
        {
            Debug.LogWarning($"{nameof(Unit)} received an invalid path. Ensure the target is reachable.", this);
            return;
        }

        path = newPath;
        targetIndex = 0;
        pathFinished = false;
        StopCoroutine(nameof(FollowPath));
        StartCoroutine(nameof(FollowPath));
    }

    protected IEnumerator FollowPath()
    {
        if (path == null || path.Length == 0)
        {
            yield break;
        }
        WaitForFixedUpdate wait = new();

        Vector3 currentWaypoint = path[0];
        while (true)
        {
            float distance = Vector3.Distance(transform.position, currentWaypoint);
            if (distance <= 0.1)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    pathFinished = true;
                    yield break;
                }

                currentWaypoint = path[targetIndex];
            }
            Vector3 direction = (currentWaypoint - rb.position);
            direction.y = 0;
            direction.Normalize();
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            // While following a ground path, compare to the ground-projected target position so vertical offsets don't interfere
            if (Vector3.Distance(transform.position, groundTargetPos) < attackRange)
            {
                targetIndex = path.Length;
                pathFinished = true;
                yield break;
            }
            yield return wait;
        }
    }
    public void OnDrawGizmos()
    {
        if (path != null)
        {
            for (int i = targetIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one);
                if (i == targetIndex)
                {
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
        }
    }
    public void SpeedControl()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVelocity.magnitude > moveSpeed)
        {
            flatVelocity = flatVelocity.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(flatVelocity.x, rb.linearVelocity.y, flatVelocity.z);
        }
    }

    public void EnemyStateHandler()
    {
        if (target == null)
        {
            State.Value = EnemyState.Idle;
            return;
        }
        State.Value = EnemyState.Chasing;
    }

    protected bool CanSeeTarget()
    {
        if (target == null) return false;
        RaycastHit hit;
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, detectionRange))
        {
            if (hit.transform == target)
            {
                return true;
            }
        }
        return false;
    }
}


