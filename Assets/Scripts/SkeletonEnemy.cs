using System.Collections;
using UnityEngine;

/// <summary>
/// Shared ground-melee AI for the Skeleton Warrior and Skeleton Spearman prefabs.
/// The sprite sheets face right by default; only the SpriteRenderer is flipped.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class SkeletonEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float detectionRange = 6f;
    [Tooltip("Extra distance Player must move away after detection before this enemy returns to Patrol. Prevents rapid state/flip changes at the range boundary.")]
    [SerializeField, Min(0f)] private float detectionExitBuffer = 0.75f;
    [SerializeField, Min(0f)] private float attackRange = 1.2f;
    [SerializeField, Min(0f)] private float maxVerticalDetectionDifference = 2.5f;
    [SerializeField, Min(0.01f)] private float facingUpdateThreshold = 0.1f;

    [Header("Attack")]
    [SerializeField, Min(0.01f)] private float attackCooldown = 1.2f;
    [SerializeField, Min(0)] private int attackDamage = 2;
    [SerializeField, Min(0f)] private float attackHitDelay = 0.28f;
    [SerializeField, Min(0.01f)] private float attackAnimationDuration = 0.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField, Min(0f)] private float attackPointOffset = 0.85f;
    [SerializeField, Min(0.01f)] private float attackHitRadius = 0.4f;
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Patrol Sensors")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float wallCheckDistance = 0.15f;
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.55f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Actor Collision")]
    [Tooltip("Prevents the skeleton from being physically blocked by the player or other enemies. Trigger hitboxes remain active.")]
    [SerializeField] private bool ignoreActorBodyCollisions = true;
    [Tooltip("Only colliders belonging to terrain tagged ground can block horizontal movement. This prevents decorative colliders from trapping the skeleton.")]
    [SerializeField] private bool ignoreNonGroundCollisions = true;

    [Header("Temporary AI Debug")]
    [SerializeField] private bool debugLogging = false;
    [SerializeField, Min(0.001f)] private float stoppedVelocityThreshold = 0.01f;

    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Attack = Animator.StringToHash("Attack");

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Collider2D playerCollider;
    private bool canAttack = true;
    private bool isAttacking;
    private bool playerWasDetected;
    private int facingDirection = 1;
    private EnemyState currentState;
    private bool hasLoggedInitialState;
    private EnemyState lastLoggedState;
    private bool stopSnapshotLogged;
    private string lastMovementCommand = "None";
    private string lastStopReason = "No StopMoving call recorded";
    private Collider2D lastWallCollider;
    private Collider2D lastGroundCollider;
    private Vector2 lastWallOrigin;
    private Vector2 lastGroundOrigin;
    private Collider2D lastPhysicalCollision;
    private Vector2 lastPhysicalContactPoint;
    private Vector2 lastPhysicalContactNormal;
    private int lastPhysicalContactCount;

    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = facingDirection < 0;

        EnsureAttackPoint();

        if (wallCheck == null)
        {
            wallCheck = transform.Find("WallCheck");
        }

        if (groundCheck == null)
        {
            // Supports Skeletons created before the refactor without requiring
            // manual scene repair. New prefabs use the explicit GroundCheck name.
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                groundCheck = transform.Find("EdgeCheck");
            }
        }
    }

    private void EnsureAttackPoint()
    {
        if (attackPoint == null)
        {
            attackPoint = transform.Find("AttackPoint");
        }

        if (attackPoint != null)
        {
            return;
        }

        // Older Skeleton objects were placed directly in scenes instead of being
        // instantiated from the prefab, so they have no AttackPoint child. Create
        // the marker at runtime so their chase can always transition into Attack.
        GameObject pointObject = new GameObject("AttackPoint");
        attackPoint = pointObject.transform;
        attackPoint.SetParent(transform, false);
        attackPoint.localPosition = new Vector3(attackPointOffset, 0.35f, 0f);
    }

    private void Start()
    {
        FindPlayer();
        ConfigureActorBodyCollisions();
        UpdateChildCheckPositions();
    }

    private void FixedUpdate()
    {
        if (isAttacking)
        {
            StopMoving("Attack animation in progress");
            return;
        }

        if (player == null)
        {
            FindPlayer();

            if (player == null)
            {
                currentState = EnemyState.Patrol;
                LogStateChangeIfNeeded();
                Patrol();
                return;
            }
        }

        currentState = DetermineState();
        LogStateChangeIfNeeded();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                Chase();
                break;

            case EnemyState.Attack:
                AttackPlayer();
                break;
        }
    }

    private void Patrol()
    {
        if (IsWallAhead() || !HasGroundAhead())
        {
            TurnAround();
        }

        MoveInDirection(facingDirection);
    }

    private void Chase()
    {
        float directionToPlayer = player.position.x - body.position.x;
        SetFacingDirection(directionToPlayer);

        // Patrol must avoid ledges, but Chase must pursue Player instead of
        // freezing at the first missing tile under its forward probe. The old
        // single-ray GroundCheck could land on a Tilemap seam and incorrectly
        // convert a clear route into a permanent stop. A real vertical wall is
        // still respected; falling into a hole follows the level's normal hazard
        // rules rather than producing a running-in-place enemy.
        bool hasSensors = HasExplicitPatrolSensors();
        bool wallDetected = hasSensors && IsWallAhead();
        if (wallDetected)
        {
            StopMoving("Wall detected by WallCheck");
            return;
        }

        MoveInDirection(facingDirection);
    }

    private void AttackPlayer()
    {
        SetFacingDirection(player.position.x - body.position.x);
        StopMoving("Attack range and AttackPoint overlap");

        if (canAttack)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private EnemyState DetermineState()
    {
        if (!IsPlayerDetected())
        {
            return EnemyState.Patrol;
        }

        // Staying in Chase until the weapon can actually touch Player guarantees
        // there is no stop/idle dead zone between movement and attacking.
        return IsWithinAttackRange() && CanReachPlayerWithAttack()
            ? EnemyState.Attack
            : EnemyState.Chase;
    }

    private bool IsPlayerDetected()
    {
        if (player == null)
        {
            playerWasDetected = false;
            return false;
        }

        // Use hysteresis: enter Chase at detectionRange, but stay in Chase until
        // Player has moved beyond the wider exit range. This avoids switching
        // Patrol/Chase every physics step near the boundary, which was repeatedly
        // reversing the Warrior's facing direction.
        float activeDetectionRange = playerWasDetected
            ? detectionRange + detectionExitBuffer
            : detectionRange;
        bool detected = Mathf.Abs(player.position.x - body.position.x) <= activeDetectionRange &&
                        Mathf.Abs(player.position.y - body.position.y) <= maxVerticalDetectionDifference;
        playerWasDetected = detected;
        return detected;
    }

    private bool IsWithinAttackRange()
    {
        return GetHorizontalGapToPlayer() <= attackRange;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Transform foundPlayer = playerObject != null ? playerObject.transform : null;

        if (player == foundPlayer)
        {
            return;
        }

        player = foundPlayer;
        playerCollider = player != null
            ? player.GetComponentInChildren<Collider2D>()
            : null;
        ConfigureActorBodyCollisions();
    }

    private float GetHorizontalGapToPlayer()
    {
        if (bodyCollider == null || playerCollider == null)
        {
            return float.PositiveInfinity;
        }

        Bounds skeletonBounds = bodyCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;

        if (playerBounds.min.x > skeletonBounds.max.x)
        {
            return playerBounds.min.x - skeletonBounds.max.x;
        }

        if (skeletonBounds.min.x > playerBounds.max.x)
        {
            return skeletonBounds.min.x - playerBounds.max.x;
        }

        return 0f;
    }

    private void ConfigureActorBodyCollisions()
    {
        if (!ignoreActorBodyCollisions)
        {
            return;
        }

        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>(true);

        if (player != null)
        {
            IgnoreSolidColliders(ownColliders, player.GetComponentsInChildren<Collider2D>(true));
        }

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("enemy"))
        {
            if (enemy != gameObject)
            {
                IgnoreSolidColliders(ownColliders, enemy.GetComponentsInChildren<Collider2D>(true));
            }
        }
    }

    private static void IgnoreSolidColliders(Collider2D[] ownColliders, Collider2D[] otherColliders)
    {
        foreach (Collider2D ownCollider in ownColliders)
        {
            if (ownCollider == null || !ownCollider.enabled || ownCollider.isTrigger)
            {
                continue;
            }

            foreach (Collider2D otherCollider in otherColliders)
            {
                if (otherCollider != null && otherCollider.enabled && !otherCollider.isTrigger)
                {
                    Physics2D.IgnoreCollision(ownCollider, otherCollider, true);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RecordPhysicalCollision(collision);
        IgnoreNonBlockingCollision(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Handles a collision that already existed when a skeleton is enabled or
        // spawned. Without this, the Rigidbody can remain pressed against a
        // decoration while its run animation keeps playing.
        RecordPhysicalCollision(collision);
        IgnoreNonBlockingCollision(collision.collider);
    }

    private void RecordPhysicalCollision(Collision2D collision)
    {
        Collider2D otherCollider = collision.collider;
        if (otherCollider != null && !otherCollider.isTrigger &&
            HasTagInHierarchy(otherCollider.transform, "ground"))
        {
            lastPhysicalCollision = otherCollider;

            // Capture the contact with the strongest horizontal normal. A normal
            // close to (+/-1, 0) proves a side wall; a normal close to (0, 1)
            // is simply floor support. This is diagnostic only and does not
            // participate in movement or collision handling.
            lastPhysicalContactCount = collision.contactCount;
            float strongestHorizontalNormal = -1f;
            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);
                float horizontalNormal = Mathf.Abs(contact.normal.x);
                if (horizontalNormal > strongestHorizontalNormal)
                {
                    strongestHorizontalNormal = horizontalNormal;
                    lastPhysicalContactPoint = contact.point;
                    lastPhysicalContactNormal = contact.normal;
                }
            }
        }
    }

    private void IgnoreNonBlockingCollision(Collider2D otherCollider)
    {
        if (otherCollider == null || otherCollider.isTrigger)
        {
            return;
        }

        bool isActor = otherCollider.CompareTag("enemy") ||
                       otherCollider.GetComponentInParent<PlayerController>() != null;
        bool isNonGroundDecoration = ignoreNonGroundCollisions &&
                                     !HasTagInHierarchy(otherCollider.transform, "ground");

        // Covers actors spawned after this skeleton's Start call and untagged
        // decorative colliders. Attack hitboxes are triggers, so J attacks still
        // connect normally. Terrain tagged ground remains solid.
        if ((ignoreActorBodyCollisions && isActor) || isNonGroundDecoration)
        {
            Physics2D.IgnoreCollision(bodyCollider, otherCollider, true);
        }
    }

    private static bool HasTagInHierarchy(Transform current, string expectedTag)
    {
        while (current != null)
        {
            if (current.CompareTag(expectedTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool HasExplicitPatrolSensors()
    {
        return wallCheck != null && groundCheck != null;
    }

    private void SetFacingDirection(float horizontalDistance)
    {
        if (Mathf.Abs(horizontalDistance) < facingUpdateThreshold)
        {
            return;
        }

        int newDirection = horizontalDistance > 0f ? 1 : -1;
        if (newDirection == facingDirection)
        {
            return;
        }

        facingDirection = newDirection;
        spriteRenderer.flipX = facingDirection < 0;
        UpdateChildCheckPositions();
    }

    private void TurnAround()
    {
        SetFacingDirection(-facingDirection);
    }

    private void UpdateChildCheckPositions()
    {
        if (attackPoint != null)
        {
            attackPoint.localPosition = new Vector3(
                facingDirection * attackPointOffset,
                attackPoint.localPosition.y,
                attackPoint.localPosition.z
            );
        }

        if (wallCheck != null)
        {
            wallCheck.localPosition = new Vector3(
                facingDirection * Mathf.Abs(wallCheck.localPosition.x),
                wallCheck.localPosition.y,
                wallCheck.localPosition.z
            );
        }

        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(
                facingDirection * Mathf.Abs(groundCheck.localPosition.x),
                groundCheck.localPosition.y,
                groundCheck.localPosition.z
            );
        }
    }

    private void MoveInDirection(int direction)
    {
        lastMovementCommand = currentState + " requested velocity X = " +
                              (direction * moveSpeed).ToString("F2");
        body.linearVelocity = new Vector2(
            direction * moveSpeed,
            body.linearVelocity.y
        );
    }

    private void StopMoving(string reason)
    {
        lastStopReason = reason;
        lastMovementCommand = "StopMoving(" + reason + ")";
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        animator.SetBool(IsMoving, false);
    }

    private void LateUpdate()
    {
        // The run animation must show measured Rigidbody movement, rather than
        // the AI's intention to move. This prevents visual running-in-place.
        if (!isAttacking)
        {
            animator.SetBool(IsMoving, Mathf.Abs(body.linearVelocity.x) > 0.01f);
        }

        LogStopSnapshotIfNeeded();
    }

    private void LogStateChangeIfNeeded()
    {
        if (!debugLogging)
        {
            return;
        }

        if (!hasLoggedInitialState || currentState != lastLoggedState)
        {
            string previousState = hasLoggedInitialState ? lastLoggedState.ToString() : "None";
            Debug.Log(
                "[" + name + "] " + previousState + " -> " + currentState +
                " | " + BuildStateDecisionDebug(),
                this
            );
            lastLoggedState = currentState;
            hasLoggedInitialState = true;
        }
    }

    private string BuildStateDecisionDebug()
    {
        if (player == null)
        {
            return "Reason: Player reference is null";
        }

        float horizontalDistance = Mathf.Abs(player.position.x - body.position.x);
        float verticalDistance = Mathf.Abs(player.position.y - body.position.y);
        bool detected = IsPlayerDetected();
        bool inAttackRange = IsWithinAttackRange();
        bool attackPointCanHit = CanReachPlayerWithAttack();

        string reason;
        if (!detected)
        {
            reason = "Player outside detection rule";
        }
        else if (!inAttackRange)
        {
            reason = "Collider gap is outside Attack Range";
        }
        else if (!attackPointCanHit)
        {
            reason = "AttackPoint has not reached Player collider";
        }
        else
        {
            reason = "Attack conditions satisfied";
        }

        return "Reason: " + reason +
               " | Detected=" + detected +
               " | X=" + horizontalDistance.ToString("F3") + "/" + detectionRange.ToString("F3") +
               " | Y=" + verticalDistance.ToString("F3") + "/" + maxVerticalDetectionDifference.ToString("F3") +
               " | Gap=" + GetHorizontalGapToPlayer().ToString("F3") + "/" + attackRange.ToString("F3") +
               " | AttackPointCanHit=" + attackPointCanHit +
               " | CanAttack=" + canAttack +
               " | IsAttacking=" + isAttacking;
    }

    private void LogStopSnapshotIfNeeded()
    {
        bool playerDetected = IsPlayerDetected();
        bool stoppedOutsideAttack = playerDetected &&
                                   currentState != EnemyState.Attack &&
                                   !isAttacking &&
                                   Mathf.Abs(body.linearVelocity.x) <= stoppedVelocityThreshold;

        if (!stoppedOutsideAttack)
        {
            stopSnapshotLogged = false;
            return;
        }

        if (!debugLogging || stopSnapshotLogged)
        {
            return;
        }

        bool hasSensors = HasExplicitPatrolSensors();
        bool wallDetected = hasSensors && IsWallAhead();
        bool groundAhead = !hasSensors || HasGroundAhead();
        float centerDistance = player != null
            ? Vector2.Distance(body.position, player.position)
            : float.PositiveInfinity;
        float horizontalDistance = player != null
            ? Mathf.Abs(body.position.x - player.position.x)
            : float.PositiveInfinity;
        bool inAttackRange = IsWithinAttackRange();
        bool animatorMoving = animator.GetBool(IsMoving);

        string stopReason = lastMovementCommand.StartsWith("StopMoving(")
            ? lastStopReason
            : "AI requested movement; Rigidbody velocity resolved to zero (check physical collider).";

        Debug.Log(
            "\n================================\n" +
            "SKELETON STOP DEBUG\n" +
            "================================\n" +
            "Enemy: " + name + "\n" +
            "Current State: " + currentState + "\n" +
            "Enemy Position: " + body.position + "\n" +
            "Player Position: " + (player != null ? player.position.ToString() : "None") + "\n" +
            "Center Distance: " + centerDistance.ToString("F3") + "\n" +
            "Horizontal Distance: " + horizontalDistance.ToString("F3") + "\n" +
            "Collider Gap: " + GetHorizontalGapToPlayer().ToString("F3") + "\n" +
            "Detection Range: " + detectionRange.ToString("F3") + "\n" +
            "Attack Range: " + attackRange.ToString("F3") + "\n" +
            "Player Detected: " + playerDetected + "\n" +
            "Player In Attack Range: " + inAttackRange + "\n" +
            "Can Attack: " + canAttack + "\n" +
            "Attack Cooldown Ready: " + canAttack + "\n" +
            "Is Attacking: " + isAttacking + "\n" +
            "Wall Detected: " + wallDetected + " (" + DescribeCollider(lastWallCollider) + ")\n" +
            "Ground Ahead: " + groundAhead + " (" + DescribeCollider(lastGroundCollider) + ")\n" +
            "Wall Ray: " + lastWallOrigin + " -> " + (Vector2.right * facingDirection) + " / " + wallCheckDistance.ToString("F2") + "\n" +
            "Ground Ray: " + lastGroundOrigin + " -> Down / " + groundCheckDistance.ToString("F2") + "\n" +
            "Facing Direction: " + facingDirection + "\n" +
            "Rigidbody Body Type: " + body.bodyType + "\n" +
            "Rigidbody Constraints: " + body.constraints + "\n" +
            "Rigidbody Velocity: " + body.linearVelocity + "\n" +
            "Animator IsMoving: " + animatorMoving + "\n" +
            "Last Physical Collision: " + DescribeCollider(lastPhysicalCollision) + "\n" +
            "Last Contact Count: " + lastPhysicalContactCount + "\n" +
            "Last Contact Point: " + lastPhysicalContactPoint + "\n" +
            "Last Contact Normal: " + lastPhysicalContactNormal +
            " (side if |X| is near 1; floor if Y is near 1)\n" +
            "Skeleton Collider Bounds: " + (bodyCollider != null ? bodyCollider.bounds.ToString() : "None") + "\n" +
            "Ground Collider Bounds: " + (lastPhysicalCollision != null ? lastPhysicalCollision.bounds.ToString() : "None") + "\n" +
            "Last Movement Command: " + lastMovementCommand + "\n" +
            "MOVEMENT STOPPED BY: " + stopReason + "\n" +
            "================================",
            this
        );

        stopSnapshotLogged = true;
    }

    private static string DescribeCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return "None";
        }

        return collider.name + " | Layer: " + LayerMask.LayerToName(collider.gameObject.layer) +
               " | Tag: " + collider.tag;
    }

    private bool IsWallAhead()
    {
        Vector2 origin = wallCheck != null
            ? wallCheck.position
            : GetFallbackWallCheckPosition();
        lastWallOrigin = origin;
        lastWallCollider = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            Vector2.right * facingDirection,
            wallCheckDistance,
            groundLayers
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (IsWalkableGround(hit.collider))
            {
                lastWallCollider = hit.collider;
                return true;
            }
        }

        return false;
    }

    private bool HasGroundAhead()
    {
        Vector2 origin = groundCheck != null
            ? groundCheck.position
            : GetFallbackGroundCheckPosition();
        lastGroundOrigin = origin;
        lastGroundCollider = null;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            Vector2.down,
            groundCheckDistance,
            groundLayers
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (IsWalkableGround(hit.collider))
            {
                lastGroundCollider = hit.collider;
                return true;
            }
        }

        return false;
    }

    private Vector2 GetFallbackWallCheckPosition()
    {
        Bounds bounds = bodyCollider.bounds;
        return new Vector2(
            bounds.center.x + facingDirection * (bounds.extents.x + 0.02f),
            bounds.center.y
        );
    }

    private Vector2 GetFallbackGroundCheckPosition()
    {
        Bounds bounds = bodyCollider.bounds;
        return new Vector2(
            bounds.center.x + facingDirection * (bounds.extents.x + 0.08f),
            bounds.min.y + 0.04f
        );
    }

    private bool IsWalkableGround(Collider2D collider)
    {
        if (collider == null || collider.isTrigger ||
            collider.transform.IsChildOf(transform))
        {
            return false;
        }

        // Some existing level tilemaps use the Default layer or an untagged child.
        // A solid collider below the edge is still safe terrain unless it belongs to
        // a gameplay actor or hazard.
        return !collider.CompareTag("Player") &&
               !collider.CompareTag("enemy") &&
               !collider.CompareTag("hole") &&
               !collider.CompareTag("Boss");
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;
        StopMoving("Attack coroutine started");
        animator.SetTrigger(Attack);

        yield return new WaitForSeconds(attackHitDelay);
        PerformAttackHit();

        yield return new WaitForSeconds(
            Mathf.Max(0f, attackAnimationDuration - attackHitDelay)
        );
        isAttacking = false;

        yield return new WaitForSeconds(
            Mathf.Max(0f, attackCooldown - attackAnimationDuration)
        );
        canAttack = true;
    }

    /// <summary>
    /// Called at weapon-contact timing. It is public so an Animation Event can replace
    /// the coroutine timing later without changing the damage rules.
    /// </summary>
    public void PerformAttackHit()
    {
        if (TryGetAttackTarget(out PlayerController playerController))
        {
            playerController.TakeDamage(attackDamage);
        }
    }

    private bool CanReachPlayerWithAttack()
    {
        return TryGetAttackTarget(out _);
    }

    private bool TryGetAttackTarget(out PlayerController target)
    {
        target = null;
        if (attackPoint == null || player == null || playerCollider == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackHitRadius,
            playerLayers
        );

        foreach (Collider2D hit in hits)
        {
            PlayerController playerController = hit.GetComponentInParent<PlayerController>();
            if (playerController == null || playerController.transform != player)
            {
                continue;
            }

            float playerDirection = player.position.x - transform.position.x;
            if (playerDirection * facingDirection <= 0f)
            {
                return false;
            }

            target = playerController;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackHitRadius);
        }

        Collider2D gizmoCollider = bodyCollider != null
            ? bodyCollider
            : GetComponent<Collider2D>();

        if (wallCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + Vector3.right * facingDirection * wallCheckDistance
            );
        }
        else if (gizmoCollider != null)
        {
            Bounds bounds = gizmoCollider.bounds;
            Vector3 origin = new Vector3(
                bounds.center.x + facingDirection * (bounds.extents.x + 0.02f),
                bounds.center.y,
                0f
            );
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.85f);
            Gizmos.DrawLine(origin, origin + Vector3.right * facingDirection * wallCheckDistance);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance
            );
        }
        else if (gizmoCollider != null)
        {
            Bounds bounds = gizmoCollider.bounds;
            Vector3 origin = new Vector3(
                bounds.center.x + facingDirection * (bounds.extents.x + 0.08f),
                bounds.min.y + 0.04f,
                0f
            );
            Gizmos.color = new Color(1f, 1f, 0f, 0.85f);
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
        }
    }
}
