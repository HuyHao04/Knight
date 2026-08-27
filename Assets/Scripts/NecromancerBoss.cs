using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NecromancerBoss : MonoBehaviour
{
    // ==================================================
    // HEALTH
    // ==================================================

    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int phase2HP = 50;

    private int currentHP;

    private bool isDead = false;
    private bool isPhase2 = false;
    private bool isChangingPhase = false;
    private bool isInvincible = false;
    private bool combatStarted = false;


    // ==================================================
    // UI
    // ==================================================

    [Header("UI")]
    [SerializeField] private BossHealthBar bossHealthBar;


    // ==================================================
    // ANIMATION
    // ==================================================

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string movingBoolName = "IsMoving";
    [SerializeField] private string lightningCastingBoolName = "IsCastingLightning";
    [SerializeField] private string obeliskHitBoolName = "IsObeliskHit";
    [SerializeField] private string obeliskFallStunBoolName = "IsObeliskFallStun";


    // ==================================================
    // PHASE SETTINGS
    // ==================================================

    [Header("Phase Settings")]

    [SerializeField] private float phase1AttackCooldown = 2f;
    [SerializeField] private float phase2AttackCooldown = 1.5f;


    // ==================================================
    // PHASE 1 SKILL CHANCE
    // ==================================================

    [Header("Phase 1 Skill Chance")]

    [Range(0f, 1f)]
    [SerializeField] private float phase1FireballChance = 0.6f;


    // ==================================================
    // PHASE 2 SKILL CHANCE
    // ==================================================

    [Header("Phase 2 Skill Chance")]

    [Range(0f, 1f)]
    [SerializeField] private float phase2FireballChance = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float phase2GroundSpellChance = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float phase2ChargeChance = 0.20f;

    // Phần còn lại sẽ là Lightning
    //
    // Ví dụ:
    //
    // Fireball    35%
    // GroundSpell 25%
    // Charge      20%
    // Lightning   20%


    // ==================================================
    // STRONG SKILL COOLDOWN
    // ==================================================

    [Header("Strong Skill Cooldown")]

    // Charge và Lightning dùng chung cooldown này
    [SerializeField] private float strongSkillCooldown = 5f;

    private float lastStrongSkillTime = -999f;


    // ==================================================
    // PHASE 2 TRANSITION
    // ==================================================

    [Header("Phase 2 Transition")]

    [SerializeField] private GameObject phaseFlashPrefab;

    [SerializeField] private GameObject phaseAuraPrefab;

    // Hiệu ứng luôn xuất hiện theo world position hiện tại của boss.
    // Không dùng phaseFlashPoint làm vị trí spawn để tránh hiệu ứng bị bỏ lại
    // ở một marker cũ trong scene.
    [SerializeField] private Vector3 phaseFlashOffset = Vector3.zero;

    // Aura là child của boss, vì vậy offset này là local position.
    [SerializeField] private Vector3 phaseAuraOffset = Vector3.zero;

    private GameObject currentPhaseAura;
    private readonly List<GameObject> activeSkillInstances = new List<GameObject>();

    // Marker cũ trong scene, giữ lại để không làm mất Inspector reference cũ.
    // Phase 2 dùng phaseFlashOffset để Flash luôn xuất hiện ngay trên boss.
    [SerializeField] private Transform phaseFlashPoint;

    // Boss tới BossSkillPosition rồi đứng yên một chút
    [SerializeField] private float delayBeforePhaseFlash = 0.5f;

    // Flash tồn tại bao lâu
    [SerializeField] private float phaseFlashDuration = 0.8f;

    // Boss đáp xuống rồi nghỉ trước khi đánh
    [SerializeField] private float phase2StartDelay = 0.5f;


    // ==================================================
    // FIREBALL
    // ==================================================

    [Header("Fireball Skill")]

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform castPoint;

    [Header("Phase 2 Fireball Storm")]
    [SerializeField, Min(2)] private int phase2FireballCount = 16;
    [SerializeField, Min(0.02f)] private float phase2FireballShotInterval = 0.1f;
    [SerializeField, Min(0f)] private float phase2FireballMinSpawnOffset = 0.15f;
    [SerializeField, Min(0f)] private float phase2FireballMaxSpawnOffset = 0.65f;
    [SerializeField, Min(1)] private int phase2FireballAimedShotEvery = 4;
    [SerializeField, Range(0f, 45f)] private float phase2FireballAimJitter = 10f;

    private bool isUsingFireballStorm;


    // ==================================================
    // GROUND SPELL
    // ==================================================

    [Header("Ground Spell Skill")]

    [SerializeField] private GameObject groundSpellPrefab;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float groundSpellYOffset = 0.1f;

    [SerializeField] private float groundRayDistance = 20f;

    [SerializeField, Min(0.01f)] private float groundSpellRayStartOffset = 0.05f;


    // ==================================================
    // GROUND CHECK
    // ==================================================

    [Header("Ground Check")]

    [SerializeField] private float playerGroundCheckDistance = 0.15f;

    [SerializeField] private float bossGroundCheckDistance = 0.2f;


    // ==================================================
    // CHARGE SKILL
    // ==================================================

    [Header("Charge Skill")]

    [SerializeField] private float chargeSpeed = 12f;

    // Chuẩn bị trước mỗi cú lao
    [SerializeField] private float chargePrepareTime = 0.6f;

    // Mỗi cú lao tối đa bao lâu
    [SerializeField] private float maxChargeTime = 1.5f;

    // Nghỉ giữa Charge 1 và Charge 2
    [SerializeField] private float delayBetweenCharges = 0.6f;

    [SerializeField] private int chargeDamage = 3;

    [SerializeField] private float minChargeDistance = 4f;

    // Giới hạn mép arena
    [SerializeField] private Transform chargeLeftLimit;
    [SerializeField] private Transform chargeRightLimit;


    private bool isCharging = false;
    private bool chargeHitPlayer = false;


    // ==================================================
    // LIGHTNING BARRAGE
    // ==================================================

    [Header("Lightning Barrage")]

    [SerializeField] private GameObject lightningStrikePrefab;

    // Size = 9
    //
    // Element 0 = Point1
    // ...
    // Element 8 = Point9
    [SerializeField] private Transform[] lightningPoints;


    // Boss bay tới đây khi:
    // - Lightning
    // - Chuyển Phase 2
    [SerializeField] private Transform bossSkillPosition;


    // Nếu prefab Lightning đã căn đúng chân sét
    // thì để 0
    [SerializeField] private float lightningYOffset = 0f;


    [SerializeField] private float flySpeed = 5f;

    [SerializeField] private float delayBeforeWave = 0.5f;

    [SerializeField] private float delayBetweenWaves = 1.5f;

    [SerializeField] private float delayAfterLightning = 1f;


    private bool isUsingLightning = false;


    // ==================================================
    // TWO OBELISK COUNTER
    // ==================================================

    [Header("Two Obelisk Counter")]
    [SerializeField] private ObeliskManager obeliskManager;
    [SerializeField] private Transform obeliskBeamTarget;
    [SerializeField, Min(0.1f)] private float obeliskStunDuration = 2.5f;

    private bool lightningInterrupted;
    private bool obeliskCounterResolved;
    private bool isObeliskStunned;


    // ==================================================
    // PLAYER
    // ==================================================

    private Transform player;
    private Collider2D playerCollider;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;

    private Collider2D bossCollider;


    // Vị trí đứng yên hợp lệ sau khi boss kết thúc một skill chủ động.
    // Chỉ khóa trục X khi Idle, không can thiệp Charge / Lightning / đổi Phase.
    private float idleAnchorX;

    private bool hasIdleAnchor;


    // ==================================================
    // TIMER
    // ==================================================

    private float attackTimer = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public bool IsDefeated => isDead;
    public Transform ObeliskBeamTarget => obeliskBeamTarget != null
        ? obeliskBeamTarget
        : transform;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        // ==============================================
        // HEALTH
        // ==============================================

        currentHP = maxHP;


        // ==============================================
        // COMPONENTS
        // ==============================================

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        rb =
            GetComponent<Rigidbody2D>();

        bossCollider =
            GetComponent<Collider2D>();


        ConfigureIdlePhysics();
        SetIdleAnchorToCurrentPosition();


        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }


        // ==============================================
        // PLAYER
        // ==============================================

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");


        if (playerObject != null)
        {
            player =
                playerObject.transform;

            playerCollider =
                playerObject.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogWarning(
                "NecromancerBoss: Không tìm thấy Player!"
            );
        }


        // ==============================================
        // HEALTH BAR
        // ==============================================

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(
                currentHP,
                maxHP
            );

            bossHealthBar.gameObject.SetActive(false);
        }


        attackTimer = 0f;
    }

    public void StartCombat()
    {
        if (isDead || combatStarted)
        {
            return;
        }

        combatStarted = true;
        isInvincible = false;
        attackTimer = 0f;

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHP, maxHP);
            bossHealthBar.gameObject.SetActive(true);
        }

        FacePlayer();
    }

    /// <summary>
    /// Returns the boss encounter to its initial phase after the player respawns.
    /// It is intentionally called on player death only, never from Update.
    /// </summary>
    public void ResetBossEncounter()
    {
        StopAllCoroutines();
        CleanupActiveSkills();

        if (currentPhaseAura != null)
        {
            Destroy(currentPhaseAura);
            currentPhaseAura = null;
        }

        currentHP = maxHP;
        isDead = false;
        isPhase2 = false;
        isChangingPhase = false;
        isInvincible = false;
        isCharging = false;
        chargeHitPlayer = false;
        isUsingLightning = false;
        isUsingFireballStorm = false;
        lightningInterrupted = false;
        obeliskCounterResolved = false;
        isObeliskStunned = false;
        attackTimer = 0f;
        lastStrongSkillTime = -999f;

        if (bossCollider != null)
        {
            bossCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }

        obeliskManager?.ResetEncounter();

        transform.SetPositionAndRotation(initialPosition, initialRotation);

        if (rb != null)
        {
            rb.simulated = true;
            rb.position = initialPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        SetMoving(false);
        SetLightningCasting(false);
        SetObeliskHitAnimation(false);
        SetObeliskFallStunAnimation(false);
        SetIdleAnchorToCurrentPosition();

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHP, maxHP);
            bossHealthBar.gameObject.SetActive(combatStarted);
        }
    }

    private void TrackActiveSkill(GameObject skillInstance)
    {
        if (skillInstance != null)
        {
            activeSkillInstances.Add(skillInstance);
        }
    }

    private void CleanupActiveSkills()
    {
        foreach (GameObject skillInstance in activeSkillInstances)
        {
            if (skillInstance != null)
            {
                Destroy(skillInstance);
            }
        }

        activeSkillInstances.Clear();
    }


    // ==================================================
    // IDLE PHYSICS
    // ==================================================

    private void ConfigureIdlePhysics()
    {
        if (rb == null)
        {
            Debug.LogWarning(
                "NecromancerBoss: Missing Rigidbody2D. Idle physics lock is disabled."
            );

            return;
        }


        // Preserve all existing constraints; only prevent collision torque from
        // rotating the boss around Z.
        rb.constraints |=
            RigidbodyConstraints2D.FreezeRotation;
    }


    private void FixedUpdate()
    {
        if (
            rb == null ||
            isDead ||
            !rb.simulated ||
            isCharging ||
            isUsingLightning ||
            isChangingPhase
        )
        {
            return;
        }


        // Player contact must not give an idle boss horizontal momentum or spin.
        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        rb.angularVelocity =
            0f;


        // Collision resolution can still create tiny position drift even when
        // horizontal velocity is reset. Keep the boss at its latest valid idle X.
        if (hasIdleAnchor)
        {
            rb.position =
                new Vector2(
                    idleAnchorX,
                    rb.position.y
                );
        }
    }


    private void SetIdleAnchorToCurrentPosition()
    {
        if (rb == null)
            return;


        idleAnchorX =
            transform.position.x;


        hasIdleAnchor =
            true;
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (isDead)
            return;


        if (player == null)
            return;


        // ==============================================
        // FACE PLAYER
        // ==============================================

        FacePlayer();

        if (!combatStarted)
            return;


        // ==============================================
        // BOSS ĐANG BẬN
        // ==============================================

        if (isChangingPhase)
            return;


        if (isCharging)
            return;


        if (isUsingLightning)
            return;


        if (isUsingFireballStorm)
            return;


        if (isObeliskStunned)
            return;


        // ==============================================
        // ATTACK TIMER
        // ==============================================

        attackTimer +=
            Time.deltaTime;


        float currentCooldown =
            isPhase2
            ? phase2AttackCooldown
            : phase1AttackCooldown;


        if (attackTimer >=
            currentCooldown)
        {
            attackTimer = 0f;


            if (isPhase2)
            {
                UsePhase2Skill();
            }
            else
            {
                UsePhase1Skill();
            }
        }
    }


    // ==================================================
    // PHASE 1 SKILLS
    // ==================================================

    private void UsePhase1Skill()
    {
        // ==============================================
        // PLAYER TRÊN KHÔNG
        // ==============================================

        if (!IsPlayerOnGround())
        {
            Debug.Log(
                "PHASE 1 -> FIREBALL"
            );


            ShootProjectile();

            return;
        }


        // ==============================================
        // PLAYER ĐỨNG GROUND
        // ==============================================

        float randomValue =
            Random.value;


        if (randomValue <
            phase1FireballChance)
        {
            Debug.Log(
                "PHASE 1 -> FIREBALL"
            );


            ShootProjectile();
        }
        else
        {
            Debug.Log(
                "PHASE 1 -> GROUND SPELL"
            );


            CastGroundSpell();
        }
    }


    // ==================================================
    // PHASE 2 SKILLS
    // ==================================================

    private void UsePhase2Skill()
    {
        bool playerOnGround =
            IsPlayerOnGround();


        bool bossOnGround =
            IsBossOnGround();


        bool strongSkillReady =
            Time.time -
            lastStrongSkillTime
            >= strongSkillCooldown;


        // ==============================================
        // PLAYER ĐANG TRÊN KHÔNG
        // ==============================================

        if (!playerOnGround)
        {
            // GroundSpell và Charge không phù hợp

            if (strongSkillReady &&
                Random.value < 0.25f)
            {
                Debug.Log(
                    "PHASE 2 -> LIGHTNING BARRAGE"
                );


                lastStrongSkillTime =
                    Time.time;


                StartCoroutine(
                    LightningBarrage()
                );
            }
            else
            {
                Debug.Log(
                    "PHASE 2 -> FIREBALL"
                );


                ShootProjectile();
            }


            return;
        }


        // ==============================================
        // PLAYER ĐANG DƯỚI ĐẤT
        // ==============================================

        float randomValue =
            Random.value;


        float fireballEnd =
            phase2FireballChance;


        float groundSpellEnd =
            fireballEnd +
            phase2GroundSpellChance;


        float chargeEnd =
            groundSpellEnd +
            phase2ChargeChance;


        // ==============================================
        // FIREBALL
        // ==============================================

        if (randomValue <
            fireballEnd)
        {
            Debug.Log(
                "PHASE 2 -> FIREBALL"
            );


            ShootProjectile();

            return;
        }


        // ==============================================
        // GROUND SPELL
        // ==============================================

        if (randomValue <
            groundSpellEnd)
        {
            Debug.Log(
                "PHASE 2 -> GROUND SPELL"
            );


            CastGroundSpell();

            return;
        }


        // ==============================================
        // CHARGE
        // ==============================================

        if (randomValue <
            chargeEnd)
        {
            if (
                strongSkillReady &&
                bossOnGround &&
                CanUseCharge()
            )
            {
                Debug.Log(
                    "PHASE 2 -> CHARGE x2"
                );


                lastStrongSkillTime =
                    Time.time;


                StartCoroutine(
                    ChargeAttack()
                );
            }
            else
            {
                Debug.Log(
                    "CHARGE không dùng được -> FIREBALL"
                );


                ShootProjectile();
            }


            return;
        }


        // ==============================================
        // LIGHTNING
        // ==============================================

        if (strongSkillReady)
        {
            Debug.Log(
                "PHASE 2 -> LIGHTNING BARRAGE"
            );


            lastStrongSkillTime =
                Time.time;


            StartCoroutine(
                LightningBarrage()
            );
        }
        else
        {
            Debug.Log(
                "LIGHTNING cooldown -> GROUND SPELL"
            );


            CastGroundSpell();
        }
    }


    // ==================================================
    // FACE PLAYER
    // ==================================================

    private void FacePlayer()
    {
        if (spriteRenderer == null ||
            player == null)
            return;


        if (
            player.position.x <
            transform.position.x
        )
        {
            spriteRenderer.flipX =
                true;
        }
        else
        {
            spriteRenderer.flipX =
                false;
        }
    }


    // ==================================================
    // ATTACK ANIMATION
    // ==================================================

    private void PlayAttackAnimation()
    {
        if (animator == null)
            return;


        animator.ResetTrigger(
            attackTriggerName
        );


        animator.SetTrigger(
            attackTriggerName
        );
    }


    // ==================================================
    // MOVE ANIMATION
    // ==================================================

    private void SetMoving(
        bool moving)
    {
        if (animator == null)
            return;


        animator.SetBool(
            movingBoolName,
            moving
        );
    }

    private void SetLightningCasting(bool casting)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(lightningCastingBoolName, casting);
    }

    private void SetObeliskHitAnimation(bool hit)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(obeliskHitBoolName, hit);
    }

    private void SetObeliskFallStunAnimation(bool active)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(obeliskFallStunBoolName, active);
    }


    // ==================================================
    // PLAYER GROUND CHECK
    // ==================================================

    private bool IsPlayerOnGround()
    {
        if (player == null)
            return false;


        if (playerCollider == null)
        {
            playerCollider =
                player.GetComponent<Collider2D>();


            if (playerCollider == null)
                return false;
        }


        Vector2 feetPosition =
            new Vector2(
                playerCollider.bounds.center.x,
                playerCollider.bounds.min.y +
                0.02f
            );


        RaycastHit2D hit =
            Physics2D.Raycast(
                feetPosition,
                Vector2.down,
                playerGroundCheckDistance,
                groundLayer
            );


        return
            hit.collider != null;
    }


    // ==================================================
    // BOSS GROUND CHECK
    // ==================================================

    private bool IsBossOnGround()
    {
        if (bossCollider == null)
            return false;


        Vector2 feetPosition =
            new Vector2(
                bossCollider.bounds.center.x,
                bossCollider.bounds.min.y +
                0.02f
            );


        RaycastHit2D hit =
            Physics2D.Raycast(
                feetPosition,
                Vector2.down,
                bossGroundCheckDistance,
                groundLayer
            );


        return
            hit.collider != null;
    }


    // ==================================================
    // FIREBALL
    // ==================================================

    private void ShootProjectile()
    {
        if (
            projectilePrefab == null ||
            castPoint == null ||
            player == null
        )
        {
            return;
        }


        if (!isPhase2)
        {
            PlayAttackAnimation();

            Vector2 direction =
                (
                    player.position -
                    castPoint.position
                ).normalized;

            SpawnFireball(direction, 0f);
            return;
        }

        if (!isUsingFireballStorm)
        {
            StartCoroutine(Phase2FireballStorm());
        }
    }

    private IEnumerator Phase2FireballStorm()
    {
        isUsingFireballStorm = true;
        PlayAttackAnimation();

        int fireballCount = Mathf.Max(2, phase2FireballCount);
        int aimedShotEvery = Mathf.Max(1, phase2FireballAimedShotEvery);
        float minSpawnOffset = Mathf.Min(
            phase2FireballMinSpawnOffset,
            phase2FireballMaxSpawnOffset
        );
        float maxSpawnOffset = Mathf.Max(
            phase2FireballMinSpawnOffset,
            phase2FireballMaxSpawnOffset
        );

        for (int index = 0; index < fireballCount; index++)
        {
            if (isDead || !combatStarted || !isPhase2)
                break;

            bool isAimedShot = index % aimedShotEvery == 0;
            Vector2 direction = isAimedShot
                ? GetJitteredDirectionToPlayer()
                : GetRandomStormDirection();
            float spawnOffset = Random.Range(minSpawnOffset, maxSpawnOffset);

            SpawnFireball(direction, spawnOffset);

            if (index < fireballCount - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0.02f, phase2FireballShotInterval)
                );
            }
        }

        isUsingFireballStorm = false;
        attackTimer = 0f;
    }

    private Vector2 GetJitteredDirectionToPlayer()
    {
        Vector2 directionToPlayer =
            (
                player.position -
                castPoint.position
            ).normalized;

        float jitter = Random.Range(
            -phase2FireballAimJitter,
            phase2FireballAimJitter
        );
        Vector3 rotatedDirection =
            Quaternion.Euler(0f, 0f, jitter) * directionToPlayer;

        return new Vector2(rotatedDirection.x, rotatedDirection.y).normalized;
    }

    private Vector2 GetRandomStormDirection()
    {
        // The reference pattern fills the air irregularly instead of producing
        // one evenly-spaced fan. A small downward component is allowed, but the
        // upward bias prevents most projectiles from dying against the floor as
        // soon as they leave a ground-level cast point.
        Vector2 direction = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-0.15f, 1f)
        );

        if (direction.sqrMagnitude < 0.04f)
        {
            direction = Random.value < 0.5f
                ? Vector2.left
                : Vector2.right;
        }

        return direction.normalized;
    }

    private void SpawnFireball(Vector2 direction, float spawnOffset)
    {
        Vector2 normalizedDirection = direction.normalized;
        Vector3 spawnPosition = castPoint.position
            + (Vector3)(normalizedDirection * Mathf.Max(0f, spawnOffset));

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        TrackActiveSkill(projectile);

        NecromancerProjectile projectileScript =
            projectile.GetComponent<NecromancerProjectile>();

        if (projectileScript != null)
        {
            projectileScript.SetDirection(normalizedDirection);
        }
    }


    // ==================================================
    // GROUND SPELL
    // ==================================================

    private void CastGroundSpell()
    {
        if (
            groundSpellPrefab == null ||
            player == null
        )
        {
            return;
        }


        // ==============================================
        // PLAYER PHẢI ĐỨNG GROUND
        // ==============================================

        if (!IsPlayerOnGround())
        {
            ShootProjectile();

            return;
        }


        // ==============================================
        // ANIMATION
        // ==============================================

        PlayAttackAnimation();


        // ==============================================
        // RAYCAST
        // ==============================================

        if (playerCollider == null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (playerCollider == null)
        {
            return;
        }

        // Cast from just above the player's feet. Casting from above the player
        // would incorrectly hit an Obelisk platform hanging over their head.
        Vector2 rayStart = new Vector2(
            playerCollider.bounds.center.x,
            playerCollider.bounds.min.y + groundSpellRayStartOffset
        );


        RaycastHit2D hit =
            Physics2D.Raycast(
                rayStart,
                Vector2.down,
                groundRayDistance,
                groundLayer
            );


        if (hit.collider == null)
            return;


        // ==============================================
        // SPAWN
        // ==============================================

        Vector3 spawnPosition =
            new Vector3(
                hit.point.x,
                hit.point.y +
                groundSpellYOffset,
                0f
            );


        GameObject groundSpell = Instantiate(
            groundSpellPrefab,
            spawnPosition,
            Quaternion.identity
        );

        TrackActiveSkill(groundSpell);
    }


    // ==================================================
    // CAN USE CHARGE
    // ==================================================

    private bool CanUseCharge()
    {
        if (player == null)
            return false;


        if (
            !IsPlayerOnGround() ||
            !IsBossOnGround()
        )
        {
            return false;
        }


        float distance =
            Mathf.Abs(
                player.position.x -
                transform.position.x
            );


        return
            distance >=
            minChargeDistance;
    }


    // ==================================================
    // CHARGE x2
    // ==================================================

    private IEnumerator ChargeAttack()
    {
        if (!CanUseCharge())
            yield break;


        if (isCharging)
            yield break;


        if (isUsingLightning)
            yield break;


        // ==============================================
        // START
        // ==============================================

        isCharging = true;

        attackTimer = 0f;


        Debug.Log(
            "===== CHARGE x2 START ====="
        );


        // ==============================================
        // CHARGE 2 LẦN
        // ==============================================

        for (
            int chargeNumber = 1;
            chargeNumber <= 2;
            chargeNumber++
        )
        {
            if (
                isDead ||
                player == null
            )
            {
                break;
            }


            // ==========================================
            // PREPARE
            // ==========================================

            SetMoving(false);

            FacePlayer();


            yield return new WaitForSeconds(
                chargePrepareTime
            );


            if (
                isDead ||
                player == null
            )
            {
                break;
            }


            // ==========================================
            // LOCK DIRECTION
            // ==========================================

            float direction =
                player.position.x >
                transform.position.x
                ? 1f
                : -1f;


            if (spriteRenderer != null)
            {
                spriteRenderer.flipX =
                    direction < 0f;
            }


            chargeHitPlayer = false;


            // ==========================================
            // MOVE ANIMATION
            // ==========================================

            SetMoving(true);


            float chargeTimer =
                0f;


            // ==========================================
            // DASH
            // ==========================================

            while (
                chargeTimer <
                maxChargeTime
            )
            {
                if (isDead)
                    break;


                chargeTimer +=
                    Time.deltaTime;


                Vector3 nextPosition =
                    transform.position +
                    Vector3.right *
                    direction *
                    chargeSpeed *
                    Time.deltaTime;


                // ======================================
                // LEFT LIMIT
                // ======================================

                if (
                    direction < 0f &&
                    chargeLeftLimit != null &&
                    nextPosition.x <=
                    chargeLeftLimit.position.x
                )
                {
                    nextPosition.x =
                        chargeLeftLimit.position.x;


                    transform.position =
                        nextPosition;


                    break;
                }


                // ======================================
                // RIGHT LIMIT
                // ======================================

                if (
                    direction > 0f &&
                    chargeRightLimit != null &&
                    nextPosition.x >=
                    chargeRightLimit.position.x
                )
                {
                    nextPosition.x =
                        chargeRightLimit.position.x;


                    transform.position =
                        nextPosition;


                    break;
                }


                transform.position =
                    nextPosition;


                // ======================================
                // HIT PLAYER
                // ======================================

                if (chargeHitPlayer)
                {
                    break;
                }


                yield return null;
            }


            SetMoving(false);


            // ==========================================
            // WAIT FOR SECOND CHARGE
            // ==========================================

            if (chargeNumber < 2)
            {
                yield return new WaitForSeconds(
                    delayBetweenCharges
                );
            }
        }


        // ==============================================
        // END
        // ==============================================

        SetMoving(false);


        SetIdleAnchorToCurrentPosition();


        isCharging =
            false;


        chargeHitPlayer =
            false;


        attackTimer =
            0f;


        FacePlayer();


        Debug.Log(
            "===== CHARGE x2 END ====="
        );
    }


    // ==================================================
    // CHARGE COLLISION
    // ==================================================

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (!isCharging)
            return;


        if (chargeHitPlayer)
            return;


        if (
            !collision.gameObject
            .CompareTag("Player")
        )
        {
            return;
        }


        PlayerController playerController =
            collision.gameObject
            .GetComponent<PlayerController>();


        if (playerController == null)
            return;


        playerController.TakeDamage(
            chargeDamage
        );


        chargeHitPlayer =
            true;


        Debug.Log(
            "CHARGE HIT PLAYER!"
        );
    }


    // ==================================================
    // LIGHTNING BARRAGE
    // ==================================================

    private IEnumerator LightningBarrage()
    {
        if (
            isUsingLightning ||
            isCharging ||
            isChangingPhase
        )
        {
            yield break;
        }


        if (
            lightningStrikePrefab == null ||
            bossSkillPosition == null
        )
        {
            yield break;
        }


        if (
            lightningPoints == null ||
            lightningPoints.Length < 9
        )
        {
            Debug.LogWarning(
                "LightningPoints cần đủ Point1 -> Point9!"
            );


            yield break;
        }


        isUsingLightning = true;
        lightningInterrupted = false;
        obeliskCounterResolved = false;
        attackTimer = 0f;

        Vector3 originalPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        SetMoving(true);

        while (Vector2.Distance(transform.position, bossSkillPosition.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                bossSkillPosition.position,
                flySpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = bossSkillPosition.position;
        SetMoving(false);
        SetLightningCasting(true);

        // This explicit call is the only place that enables the obelisks. The
        // phase transition also uses BossSkillPosition, but never opens a window.
        obeliskManager?.OpenWindow();

        yield return WaitForLightningDelay(delayBeforeWave);
        if (lightningInterrupted)
        {
            yield return ResolveInterruptedLightning(originalPosition);
            yield break;
        }

        Debug.Log("LIGHTNING WAVE 1");
        SpawnLightning(4);
        SpawnLightning(5);
        SpawnLightning(6);

        yield return WaitForLightningDelay(delayBetweenWaves);
        if (lightningInterrupted)
        {
            yield return ResolveInterruptedLightning(originalPosition);
            yield break;
        }

        Debug.Log("LIGHTNING WAVE 2");
        SpawnLightning(2);
        SpawnLightning(3);
        SpawnLightning(4);
        SpawnLightning(6);
        SpawnLightning(7);
        SpawnLightning(8);

        yield return WaitForLightningDelay(delayBetweenWaves);
        if (lightningInterrupted)
        {
            yield return ResolveInterruptedLightning(originalPosition);
            yield break;
        }

        Debug.Log("LIGHTNING WAVE 3");
        for (int i = 1; i <= 9; i++)
        {
            SpawnLightning(i);
        }

        yield return WaitForLightningDelay(delayAfterLightning);
        if (lightningInterrupted)
        {
            yield return ResolveInterruptedLightning(originalPosition);
            yield break;
        }

        obeliskManager?.CloseFailedWindow();
        SetLightningCasting(false);
        yield return ReturnFromLightning(originalPosition);

        isUsingLightning = false;
        lightningInterrupted = false;
        obeliskCounterResolved = false;
        attackTimer = 0f;
        FacePlayer();
    }

    private IEnumerator WaitForLightningDelay(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !lightningInterrupted && !isDead)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ResolveInterruptedLightning(Vector3 previousGroundPosition)
    {
        SetLightningCasting(false);

        // Keep Malakor at BossSkillPosition until the two beams have completed.
        while (!obeliskCounterResolved && !isDead)
        {
            yield return null;
        }

        if (isDead)
        {
            yield break;
        }

        // Sprite 100 is only visible while both beams are touching the boss.
        // Once the beams finish, switch to sprite 113 for the fall and stun.
        SetObeliskHitAnimation(false);
        SetObeliskFallStunAnimation(true);
        yield return FallStraightDownAfterObelisk(previousGroundPosition);

        isUsingLightning = false;
        lightningInterrupted = false;
        obeliskCounterResolved = false;
        isObeliskStunned = true;
        isInvincible = false;
        attackTimer = 0f;
        FacePlayer();

        yield return new WaitForSeconds(obeliskStunDuration);

        SetObeliskFallStunAnimation(false);
        isObeliskStunned = false;
        attackTimer = 0f;
        FacePlayer();
    }

    private IEnumerator FallStraightDownAfterObelisk(Vector3 previousGroundPosition)
    {
        float lockedX = transform.position.x;
        Vector3 landingPosition = FindObeliskFallLandingPosition(
            lockedX,
            previousGroundPosition);

        SetMoving(false);

        while (Mathf.Abs(transform.position.y - landingPosition.y) > 0.05f)
        {
            Vector3 position = transform.position;
            position.x = lockedX;
            position.y = Mathf.MoveTowards(
                position.y,
                landingPosition.y,
                flySpeed * Time.deltaTime);
            transform.position = position;

            yield return null;
        }

        transform.position = landingPosition;

        if (rb != null)
        {
            rb.position = landingPosition;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        SetIdleAnchorToCurrentPosition();
    }

    private Vector3 FindObeliskFallLandingPosition(
        float lockedX,
        Vector3 previousGroundPosition)
    {
        float pivotToFeet = bossCollider != null
            ? transform.position.y - bossCollider.bounds.min.y
            : 0f;
        float requiredDistance = Mathf.Abs(
            transform.position.y - previousGroundPosition.y) + 5f;
        float rayDistance = Mathf.Max(groundRayDistance, requiredDistance);
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(lockedX, transform.position.y),
            Vector2.down,
            rayDistance,
            groundLayer);

        RaycastHit2D nearestGroundHit = default;
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null
                || hit.collider.isTrigger
                || hit.collider == bossCollider
                || hit.collider.GetComponentInParent<ObeliskPlatformSurface>() != null)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestGroundHit = hit;
            }
        }

        float landingY = nearestGroundHit.collider != null
            ? nearestGroundHit.point.y + pivotToFeet
            : previousGroundPosition.y;

        if (nearestGroundHit.collider == null)
        {
            Debug.LogWarning(
                "NecromancerBoss: no ground found directly below BossSkillPosition; "
                + "using the previous grounded Y while keeping the skill-position X.",
                this);
        }

        return new Vector3(lockedX, landingY, transform.position.z);
    }

    private IEnumerator ReturnFromLightning(Vector3 groundPosition)
    {
        SetMoving(true);

        while (Vector2.Distance(transform.position, groundPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                groundPosition,
                flySpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = groundPosition;
        SetMoving(false);

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        SetIdleAnchorToCurrentPosition();
    }


    // ==================================================
    // SPAWN LIGHTNING
    // ==================================================

    private void SpawnLightning(
        int pointNumber)
    {
        int index =
            pointNumber - 1;


        if (
            lightningStrikePrefab == null ||
            lightningPoints == null
        )
        {
            return;
        }


        if (
            index < 0 ||
            index >= lightningPoints.Length
        )
        {
            return;
        }


        Transform point =
            lightningPoints[index];


        if (point == null)
            return;


        Vector3 spawnPosition =
            new Vector3(
                point.position.x,
                point.position.y +
                lightningYOffset,
                0f
            );


        GameObject lightningStrike = Instantiate(
            lightningStrikePrefab,
            spawnPosition,
            Quaternion.identity
        );

        TrackActiveSkill(lightningStrike);
    }


    // ==================================================
    // DAMAGE
    // ==================================================

    public void TakeDamage(
        int damage)
    {
        if (isDead || !combatStarted)
            return;


        // ==============================================
        // INVINCIBLE DURING PHASE TRANSITION
        // ==============================================

        if (isInvincible)
        {
            Debug.Log(
                "Boss đang chuyển Phase -> IMMUNE!"
            );


            return;
        }


        ApplyDamage(damage, true);
    }

    public bool RequestObeliskCounterInterrupt()
    {
        if (isDead
            || !combatStarted
            || !isPhase2
            || isChangingPhase
            || !isUsingLightning
            || lightningInterrupted)
        {
            return false;
        }

        lightningInterrupted = true;
        obeliskCounterResolved = false;
        SetMoving(false);
        return true;
    }

    public bool TakeObeliskDamage(int damage)
    {
        if (isDead
            || !combatStarted
            || !isPhase2
            || !isUsingLightning
            || !lightningInterrupted
            || damage <= 0)
        {
            return false;
        }

        // This counter hit may bypass temporary flying/transition immunity,
        // but it never bypasses death or an invalid counter state.
        ApplyDamage(damage, false);

        if (!isDead)
        {
            SetObeliskFallStunAnimation(false);
            SetObeliskHitAnimation(true);
        }

        return true;
    }

    public void CompleteObeliskCounter()
    {
        if (!isDead && isUsingLightning && lightningInterrupted)
        {
            obeliskCounterResolved = true;
        }
    }

    private void ApplyDamage(int damage, bool allowPhaseTransition)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHP, maxHP);
        }

        Debug.Log("Necromancer HP: " + currentHP + " / " + maxHP);

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        if (allowPhaseTransition
            && !isPhase2
            && !isChangingPhase
            && currentHP <= phase2HP)
        {
            StartCoroutine(EnterPhase2());
        }
    }

    // ==================================================
    // PHASE EFFECT VALIDATION
    // ==================================================

    private void ValidatePhaseEffectPrefab(
        GameObject effectPrefab,
        string effectName)
    {
        SpriteRenderer effectRenderer =
            effectPrefab.GetComponentInChildren<SpriteRenderer>(
                true
            );

        if (effectRenderer == null)
        {
            Debug.LogWarning(
                effectName
                + " is missing a SpriteRenderer."
            );

            return;
        }

        if (effectRenderer.sprite == null)
        {
            Debug.LogWarning(
                effectName
                + " SpriteRenderer has no sprite assigned."
            );
        }

        if (effectRenderer.color.a <= 0f)
        {
            Debug.LogWarning(
                effectName
                + " SpriteRenderer alpha is zero."
            );
        }

        Animator effectAnimator =
            effectPrefab.GetComponentInChildren<Animator>(
                true
            );

        if (
            effectAnimator == null ||
            effectAnimator.runtimeAnimatorController == null
        )
        {
            Debug.LogWarning(
                effectName
                + " has no Animator Controller assigned."
            );
        }
    }


    // ==================================================
    // ENTER PHASE 2
    // ==================================================

    private IEnumerator EnterPhase2()
    {
        if (
            isPhase2 ||
            isChangingPhase ||
            isDead
        )
        {
            yield break;
        }


        // ==============================================
        // LOCK BOSS
        // ==============================================

        isChangingPhase =
            true;


        isInvincible =
            true;


        attackTimer =
            0f;


        SetMoving(false);


        Debug.Log(
            "===== PHASE 2 TRANSITION START ====="
        );


        // ==============================================
        // CHECK POSITION
        // ==============================================

        if (bossSkillPosition == null)
        {
            Debug.LogError(
                "NecromancerBoss: Chưa gán BossSkillPosition!"
            );


            isChangingPhase =
                false;


            isInvincible =
                false;


            yield break;
        }


        // ==============================================
        // SAVE ORIGINAL POSITION
        // ==============================================

        Vector3 originalPosition =
            transform.position;


        // ==============================================
        // PHYSICS OFF
        // ==============================================

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;


            rb.simulated =
                false;
        }


        // ==============================================
        // FLY TO BOSSSKILLPOSITION
        // ==============================================

        Debug.Log(
            "Boss bay lên BossSkillPosition..."
        );


        SetMoving(true);


        while (
            Vector2.Distance(
                transform.position,
                bossSkillPosition.position
            ) > 0.05f
        )
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    bossSkillPosition.position,
                    flySpeed *
                    Time.deltaTime
                );


            yield return null;
        }


        transform.position =
            bossSkillPosition.position;


        SetMoving(false);


        Debug.Log(
            "Boss reached BossSkillPosition"
        );


        // ==============================================
        // WAIT BEFORE FLASH
        // ==============================================

        yield return new WaitForSeconds(
            delayBeforePhaseFlash
        );


        // ==============================================
        // PHASE FLASH
        // ==============================================

        Debug.Log(
            "Spawning PhaseFlash at: "
            + (transform.position + phaseFlashOffset)
        );


        GameObject flash =
            null;


        if (phaseFlashPrefab == null)
        {
            Debug.LogError(
                "ERROR: PhaseFlash Prefab is not assigned!"
            );
        }
        else
        {
            ValidatePhaseEffectPrefab(
                phaseFlashPrefab,
                "PhaseFlash"
            );

            flash =
                Instantiate(
                    phaseFlashPrefab,
                    transform.position + phaseFlashOffset,
                    Quaternion.identity
                );

            TrackActiveSkill(flash);

            Debug.Log(
                "PhaseFlash created successfully"
            );
        }


        // Aura được tạo cùng frame với Flash và chỉ bị hủy khi boss chết.
        if (phaseAuraPrefab == null)
        {
            Debug.LogError(
                "ERROR: PhaseAura Prefab is not assigned!"
            );
        }
        else if (currentPhaseAura == null)
        {
            Debug.Log(
                "Spawning PhaseAura"
            );

            ValidatePhaseEffectPrefab(
                phaseAuraPrefab,
                "PhaseAura"
            );

            currentPhaseAura =
                Instantiate(
                    phaseAuraPrefab,
                    transform
                );

            currentPhaseAura.transform.localPosition =
                phaseAuraOffset;

            currentPhaseAura.transform.localRotation =
                Quaternion.identity;

            Debug.Log(
                "PhaseAura parent: "
                + currentPhaseAura.transform.parent.name
            );
        }


        // ==============================================
        // WAIT FLASH
        // ==============================================

        yield return new WaitForSeconds(
            phaseFlashDuration
        );


        // ==============================================
        // DESTROY FLASH
        // ==============================================

        if (flash != null)
        {
            Debug.Log(
                "Destroying PhaseFlash"
            );

            Destroy(
                flash
            );
        }


        // ==============================================
        // PHASE 2 IS NOW UNLOCKED
        // ==============================================

        isPhase2 =
            true;


        // ==============================================
        // FLY BACK
        // ==============================================

        Debug.Log(
            "Boss returning to original position"
        );


        SetMoving(true);


        while (
            Vector2.Distance(
                transform.position,
                originalPosition
            ) > 0.05f
        )
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    originalPosition,
                    flySpeed *
                    Time.deltaTime
                );


            yield return null;
        }


        transform.position =
            originalPosition;


        SetMoving(false);


        // ==============================================
        // PHYSICS ON
        // ==============================================

        if (rb != null)
        {
            rb.simulated =
                true;


            rb.linearVelocity =
                Vector2.zero;
        }


        SetIdleAnchorToCurrentPosition();


        // ==============================================
        // SHORT DELAY
        // ==============================================

        yield return new WaitForSeconds(
            phase2StartDelay
        );


        // ==============================================
        // END TRANSITION
        // ==============================================

        isInvincible =
            false;


        isChangingPhase =
            false;


        attackTimer =
            0f;


        FacePlayer();


        Debug.Log(
            "===== PHASE 2 ACTIVE - 4 SKILLS ENABLED ====="
        );
    }


    // ==================================================
    // DIE
    // ==================================================

    private void Die()
    {
        if (isDead)
            return;


        isDead =
            true;


        obeliskManager?.AbortEncounter();


        SetLightningCasting(false);
        SetObeliskHitAnimation(false);
        SetObeliskFallStunAnimation(false);


        StopAllCoroutines();


        SetMoving(false);


        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }


        if (rb != null)
        {
            rb.simulated =
                true;


            rb.linearVelocity =
                Vector2.zero;
        }


        if (bossCollider != null)
        {
            bossCollider.enabled =
                false;
        }


        if (currentPhaseAura != null)
        {
            Destroy(
                currentPhaseAura
            );
        }


        Debug.Log(
            "Necromancer defeated!"
        );

        ScoreReward scoreReward = GetComponent<ScoreReward>();
        if (scoreReward != null)
        {
            scoreReward.TryAwardDefeat();
        }

        PlayerController playerController = player != null
            ? player.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();

        if (playerController != null)
        {
            playerController.CompleteLevel();
        }
        else
        {
            Debug.LogError("NecromancerBoss could not show Victory Panel because PlayerController was not found.");
        }


        Destroy(
            gameObject,
            0.5f
        );
    }
}
