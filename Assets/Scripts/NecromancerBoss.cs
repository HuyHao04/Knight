using UnityEngine;
using System.Collections;

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

    // Nếu để None:
    // Flash sẽ spawn ngay tại vị trí Boss
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


    // ==================================================
    // GROUND SPELL
    // ==================================================

    [Header("Ground Spell Skill")]

    [SerializeField] private GameObject groundSpellPrefab;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float groundSpellYOffset = 0.1f;

    [SerializeField] private float groundRayDistance = 20f;

    [SerializeField] private float rayStartHeight = 5f;


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


    // ==================================================
    // TIMER
    // ==================================================

    private float attackTimer = 0f;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
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
        }


        attackTimer = 0f;
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


        // ==============================================
        // BOSS ĐANG BẬN
        // ==============================================

        if (isChangingPhase)
            return;


        if (isCharging)
            return;


        if (isUsingLightning)
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


        // ==============================================
        // ANIMATION
        // ==============================================

        PlayAttackAnimation();


        // ==============================================
        // DIRECTION
        // ==============================================

        Vector2 direction =
            (
                player.position -
                castPoint.position
            ).normalized;


        // ==============================================
        // SPAWN
        // ==============================================

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                castPoint.position,
                Quaternion.identity
            );


        NecromancerProjectile projectileScript =
            projectile.GetComponent<NecromancerProjectile>();


        if (projectileScript != null)
        {
            projectileScript.SetDirection(
                direction
            );
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

        Vector2 rayStart =
            new Vector2(
                player.position.x,
                player.position.y +
                rayStartHeight
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


        Instantiate(
            groundSpellPrefab,
            spawnPosition,
            Quaternion.identity
        );
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


        // ==============================================
        // START
        // ==============================================

        isUsingLightning =
            true;


        attackTimer =
            0f;


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
        // FLY UP
        // ==============================================

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


        yield return new WaitForSeconds(
            delayBeforeWave
        );


        // ==============================================
        // WAVE 1
        // Point 4,5,6
        // ==============================================

        Debug.Log(
            "LIGHTNING WAVE 1"
        );


        SpawnLightning(4);
        SpawnLightning(5);
        SpawnLightning(6);


        yield return new WaitForSeconds(
            delayBetweenWaves
        );


        // ==============================================
        // WAVE 2
        // Point 2,3,4,6,7,8
        // ==============================================

        Debug.Log(
            "LIGHTNING WAVE 2"
        );


        SpawnLightning(2);
        SpawnLightning(3);
        SpawnLightning(4);

        SpawnLightning(6);
        SpawnLightning(7);
        SpawnLightning(8);


        yield return new WaitForSeconds(
            delayBetweenWaves
        );


        // ==============================================
        // WAVE 3
        // Point 1 -> 9
        // ==============================================

        Debug.Log(
            "LIGHTNING WAVE 3"
        );


        for (
            int i = 1;
            i <= 9;
            i++
        )
        {
            SpawnLightning(i);
        }


        yield return new WaitForSeconds(
            delayAfterLightning
        );


        // ==============================================
        // FLY BACK
        // ==============================================

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


        isUsingLightning =
            false;


        attackTimer =
            0f;


        FacePlayer();
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


        Instantiate(
            lightningStrikePrefab,
            spawnPosition,
            Quaternion.identity
        );
    }


    // ==================================================
    // DAMAGE
    // ==================================================

    public void TakeDamage(
        int damage)
    {
        if (isDead)
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


        // ==============================================
        // DAMAGE
        // ==============================================

        currentHP -=
            damage;


        currentHP =
            Mathf.Clamp(
                currentHP,
                0,
                maxHP
            );


        // ==============================================
        // HEALTH BAR
        // ==============================================

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(
                currentHP,
                maxHP
            );
        }


        Debug.Log(
            "Necromancer HP: "
            + currentHP
            + " / "
            + maxHP
        );


        // ==============================================
        // DEAD
        // ==============================================

        if (currentHP <= 0)
        {
            Die();

            return;
        }


        // ==============================================
        // ENTER PHASE 2
        // ==============================================

        if (
            !isPhase2 &&
            !isChangingPhase &&
            currentHP <= phase2HP
        )
        {
            StartCoroutine(
                EnterPhase2()
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
            "===== PHASE FLASH ====="
        );


        GameObject flash =
            null;


        if (phaseFlashPrefab != null)
        {
            Vector3 flashPosition;


            if (phaseFlashPoint != null)
            {
                flashPosition =
                    phaseFlashPoint.position;
            }
            else
            {
                flashPosition =
                    transform.position;
            }


            flash =
                Instantiate(
                    phaseFlashPrefab,
                    flashPosition,
                    Quaternion.identity
                );
        }
        else
        {
            Debug.LogWarning(
                "NecromancerBoss: Chưa gán PhaseFlash Prefab!"
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
            "Boss đang đáp xuống..."
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


        StopAllCoroutines();


        SetMoving(false);


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


        Debug.Log(
            "Necromancer defeated!"
        );


        Destroy(
            gameObject,
            0.5f
        );
    }
}