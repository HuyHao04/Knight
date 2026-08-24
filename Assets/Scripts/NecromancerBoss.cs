using UnityEngine;
using System.Collections;

public class NecromancerBoss : MonoBehaviour
{
    // ==================================================
    // HEALTH
    // ==================================================

    [Header("Health")]
    [SerializeField] private int maxHP = 100;

    private int currentHP;
    private bool isDead = false;


    // ==================================================
    // UI
    // ==================================================

    [Header("UI")]
    [SerializeField] private BossHealthBar bossHealthBar;


    // ==================================================
    // ATTACK SETTINGS
    // ==================================================

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f;

    [Range(0f, 1f)]
    [SerializeField] private float fireballChance = 0.6f;

    private float attackTimer;


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
    // PLAYER GROUND CHECK
    // ==================================================

    [Header("Player Ground Check")]
    [SerializeField] private float playerGroundCheckDistance = 0.15f;


    // ==================================================
    // LIGHTNING BARRAGE
    // ==================================================

    [Header("Lightning Barrage")]

    [SerializeField] private GameObject lightningStrikePrefab;

    [SerializeField] private Transform[] lightningPoints;

    [SerializeField] private Transform bossSkillPosition;
    [SerializeField] private float lightningYOffset = 4f;
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
    // BOSS COMPONENTS
    // ==================================================

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D bossCollider;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        // Health
        currentHP = maxHP;

        // Components
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();


        // ==================================================
        // FIND PLAYER
        // ==================================================

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;

            playerCollider =
                playerObject.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogWarning(
                "NecromancerBoss: Không tìm thấy Player!"
            );
        }


        // ==================================================
        // HEALTH BAR
        // ==================================================

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(
                currentHP,
                maxHP
            );
        }


        // Boss chờ cooldown
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


        // Boss luôn nhìn Player
        FacePlayer();


        // ==================================================
        // TEST LIGHTNING BẰNG PHÍM L
        // ==================================================

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!isUsingLightning)
            {
                StartCoroutine(LightningBarrage());
            }
        }


        // ==================================================
        // KHÔNG ATTACK FIREBALL/GROUND SPELL
        // KHI ĐANG DÙNG LIGHTNING
        // ==================================================

        if (isUsingLightning)
            return;


        // ==================================================
        // ATTACK TIMER
        // ==================================================

        attackTimer += Time.deltaTime;


        if (attackTimer >= attackCooldown)
        {
            UseRandomSkill();

            attackTimer = 0f;
        }
    }


    // ==================================================
    // FACE PLAYER
    // ==================================================

    private void FacePlayer()
    {
        if (spriteRenderer == null)
            return;


        if (player.position.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }


    // ==================================================
    // RANDOM FIREBALL / GROUND SPELL
    // ==================================================

    private void UseRandomSkill()
    {
        if (player == null)
            return;


        bool playerOnGround = IsPlayerOnGround();


        // ==================================================
        // PLAYER ĐANG TRÊN KHÔNG
        // CHỈ FIREBALL
        // ==================================================

        if (!playerOnGround)
        {
            Debug.Log(
                "Player đang trên không -> FIREBALL!"
            );

            ShootProjectile();

            return;
        }


        // ==================================================
        // PLAYER ĐANG ĐỨNG GROUND
        // RANDOM FIREBALL / GROUND SPELL
        // ==================================================

        float randomValue = Random.value;


        if (randomValue < fireballChance)
        {
            Debug.Log(
                "Necromancer sử dụng FIREBALL!"
            );

            ShootProjectile();
        }
        else
        {
            Debug.Log(
                "Necromancer sử dụng GROUND SPELL!"
            );

            CastGroundSpell();
        }
    }


    // ==================================================
    // CHECK PLAYER ON GROUND
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
                playerCollider.bounds.min.y + 0.02f
            );


        RaycastHit2D hit =
            Physics2D.Raycast(
                feetPosition,
                Vector2.down,
                playerGroundCheckDistance,
                groundLayer
            );


        return hit.collider != null;
    }


    // ==================================================
    // FIREBALL
    // ==================================================

    private void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                "NecromancerBoss: Chưa gán Projectile Prefab!"
            );

            return;
        }


        if (castPoint == null)
        {
            Debug.LogWarning(
                "NecromancerBoss: Chưa gán CastPoint!"
            );

            return;
        }


        if (player == null)
            return;


        // ==================================================
        // HƯỚNG TỚI PLAYER
        // ==================================================

        Vector2 direction =
            (player.position - castPoint.position).normalized;


        // ==================================================
        // SPAWN PROJECTILE
        // ==================================================

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                castPoint.position,
                Quaternion.identity
            );


        // ==================================================
        // SET DIRECTION
        // ==================================================

        NecromancerProjectile projectileScript =
            projectile.GetComponent<NecromancerProjectile>();


        if (projectileScript != null)
        {
            projectileScript.SetDirection(direction);
        }
        else
        {
            Debug.LogError(
                "Projectile Prefab không có NecromancerProjectile.cs!"
            );
        }
    }


    // ==================================================
    // GROUND SPELL
    // ==================================================

    private void CastGroundSpell()
    {
        if (groundSpellPrefab == null)
        {
            Debug.LogWarning(
                "NecromancerBoss: Chưa gán GroundSpell Prefab!"
            );

            return;
        }


        if (player == null)
            return;


        // ==================================================
        // PLAYER PHẢI ĐANG ĐỨNG GROUND
        // ==================================================

        if (!IsPlayerOnGround())
        {
            Debug.Log(
                "Player đang trên không -> Hủy GroundSpell!"
            );

            return;
        }


        // ==================================================
        // RAYCAST TỪ TRÊN PLAYER XUỐNG
        // ==================================================

        Vector2 rayStart =
            new Vector2(
                player.position.x,
                player.position.y + rayStartHeight
            );


        RaycastHit2D hit =
            Physics2D.Raycast(
                rayStart,
                Vector2.down,
                groundRayDistance,
                groundLayer
            );


        // ==================================================
        // TÌM THẤY GROUND
        // ==================================================

        if (hit.collider != null)
        {
            Vector3 spawnPosition =
                new Vector3(
                    hit.point.x,
                    hit.point.y + groundSpellYOffset,
                    0f
                );


            Instantiate(
                groundSpellPrefab,
                spawnPosition,
                Quaternion.identity
            );


            Debug.Log(
                "GroundSpell spawn tại: "
                + spawnPosition
            );
        }
        else
        {
            Debug.LogWarning(
                "GroundSpell không tìm thấy Ground!"
            );
        }
    }


    // ==================================================
    // LIGHTNING BARRAGE
    // ==================================================

    private IEnumerator LightningBarrage()
    {
        if (isUsingLightning)
            yield break;


        // ==================================================
        // CHECK SETUP
        // ==================================================

        if (lightningStrikePrefab == null)
        {
            Debug.LogError(
                "NecromancerBoss: Chưa gán LightningStrike Prefab!"
            );

            yield break;
        }


        if (bossSkillPosition == null)
        {
            Debug.LogError(
                "NecromancerBoss: Chưa gán BossSkillPosition!"
            );

            yield break;
        }


        if (lightningPoints == null ||
            lightningPoints.Length < 9)
        {
            Debug.LogError(
                "NecromancerBoss: Lightning Points phải có đủ 9 Point!"
            );

            yield break;
        }


        isUsingLightning = true;

        Debug.Log(
            "Necromancer bắt đầu LIGHTNING BARRAGE!"
        );


        // ==================================================
        // LƯU VỊ TRÍ BAN ĐẦU
        // ==================================================

        Vector3 originalPosition =
            transform.position;


        // ==================================================
        // TẠM DỪNG VẬT LÝ BOSS
        // ==================================================

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;

            rb.simulated = false;
        }


        // ==================================================
        // BOSS BAY LÊN
        // ==================================================

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
                    flySpeed * Time.deltaTime
                );


            yield return null;
        }


        transform.position =
            bossSkillPosition.position;


        yield return new WaitForSeconds(
            delayBeforeWave
        );


        // ==================================================
        // WAVE 1
        //
        // POINT 4
        // POINT 5
        // POINT 6
        // ==================================================

        Debug.Log(
            "LIGHTNING WAVE 1 - Point 4, 5, 6"
        );


        SpawnLightning(4);
        SpawnLightning(5);
        SpawnLightning(6);


        yield return new WaitForSeconds(
            delayBetweenWaves
        );


        // ==================================================
        // WAVE 2
        //
        // POINT 2
        // POINT 3
        // POINT 4
        // POINT 6
        // POINT 7
        // POINT 8
        // ==================================================

        Debug.Log(
            "LIGHTNING WAVE 2 - Point 2, 3, 4, 6, 7, 8"
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


        // ==================================================
        // WAVE 3
        //
        // POINT 1 -> POINT 9
        // ==================================================

        Debug.Log(
            "LIGHTNING WAVE 3 - ALL POINTS"
        );


        for (int i = 1; i <= 9; i++)
        {
            SpawnLightning(i);
        }


        yield return new WaitForSeconds(
            delayAfterLightning
        );


        // ==================================================
        // BOSS BAY VỀ VỊ TRÍ BAN ĐẦU
        // ==================================================

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
                    flySpeed * Time.deltaTime
                );


            yield return null;
        }


        transform.position =
            originalPosition;


        // ==================================================
        // BẬT LẠI PHYSICS
        // ==================================================

        if (rb != null)
        {
            rb.simulated = true;

            rb.linearVelocity = Vector2.zero;
        }


        isUsingLightning = false;

        attackTimer = 0f;


        Debug.Log(
            "Lightning Barrage kết thúc."
        );
    }


    // ==================================================
    // SPAWN LIGHTNING
    // ==================================================

    private void SpawnLightning(int pointNumber)
    {
        // Point1 = index 0
        // Point2 = index 1
        // ...
        // Point9 = index 8

        int index =
            pointNumber - 1;


        // ==================================================
        // CHECK PREFAB
        // ==================================================

        if (lightningStrikePrefab == null)
        {
            Debug.LogError(
                "NecromancerBoss: Chưa gán LightningStrike Prefab!"
            );

            return;
        }


        // ==================================================
        // CHECK ARRAY
        // ==================================================

        if (lightningPoints == null)
        {
            Debug.LogError(
                "NecromancerBoss: Lightning Points chưa được gán!"
            );

            return;
        }


        // ==================================================
        // CHECK INDEX
        // ==================================================

        if (index < 0 ||
            index >= lightningPoints.Length)
        {
            Debug.LogError(
                "NecromancerBoss: Point"
                + pointNumber
                + " không tồn tại!"
            );

            return;
        }


        // ==================================================
        // GET POINT
        // ==================================================

        Transform spawnPoint =
            lightningPoints[index];


        if (spawnPoint == null)
        {
            Debug.LogError(
                "NecromancerBoss: Point"
                + pointNumber
                + " đang bị None!"
            );

            return;
        }


        // ==================================================
        // SPAWN
        // ==================================================

        Vector3 spawnPosition = new Vector3(
    spawnPoint.position.x,
    spawnPoint.position.y + lightningYOffset,
    0f
);

Instantiate(
    lightningStrikePrefab,
    spawnPosition,
    Quaternion.identity
);


        Debug.Log(
            "Spawn Lightning tại Point"
            + pointNumber
        );
    }


    // ==================================================
    // TAKE DAMAGE
    // ==================================================

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;


        currentHP -= damage;


        currentHP =
            Mathf.Clamp(
                currentHP,
                0,
                maxHP
            );


        Debug.Log(
            "Necromancer HP: "
            + currentHP
            + " / "
            + maxHP
        );


        // ==================================================
        // UPDATE HEALTH BAR
        // ==================================================

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(
                currentHP,
                maxHP
            );
        }


        // ==================================================
        // DEATH
        // ==================================================

        if (currentHP <= 0)
        {
            Die();
        }
    }


    // ==================================================
    // DIE
    // ==================================================

    private void Die()
    {
        if (isDead)
            return;


        isDead = true;


        Debug.Log(
            "Necromancer defeated!"
        );


        StopAllCoroutines();


        if (rb != null)
        {
            rb.simulated = true;

            rb.linearVelocity = Vector2.zero;
        }


        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }


        Destroy(
            gameObject,
            0.5f
        );
    }


    // ==================================================
    // DEBUG GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;


        // ==================================================
        // GROUND SPELL RAY
        // ==================================================

        Vector3 spellRayStart =
            new Vector3(
                player.position.x,
                player.position.y + rayStartHeight,
                0f
            );


        Gizmos.DrawLine(
            spellRayStart,
            spellRayStart +
            Vector3.down * groundRayDistance
        );


        // ==================================================
        // PLAYER GROUND CHECK
        // ==================================================

        Collider2D col =
            player.GetComponent<Collider2D>();


        if (col != null)
        {
            Vector3 feetPosition =
                new Vector3(
                    col.bounds.center.x,
                    col.bounds.min.y + 0.02f,
                    0f
                );


            Gizmos.DrawLine(
                feetPosition,
                feetPosition +
                Vector3.down *
                playerGroundCheckDistance
            );
        }
    }
}