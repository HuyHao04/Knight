using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    [SerializeField] private float jump = 5f;
    [Header("Jump Settings")]
    [SerializeField] private float normalGravity = 1f;
    [SerializeField] private float fallGravity = 2.5f;
    [SerializeField] private float lowJumpGravity = 3f;

    [Header("Health and Respawn")]
    [SerializeField, Min(1)] private int maxHP = 10;
    [SerializeField, Min(0f)] private float deathDelay = 1f;
    [SerializeField, Min(0f)] private float respawnInvincibilityDuration = 1.5f;
    [SerializeField, Min(0.05f)] private float respawnFlashInterval = 0.12f;

    [Header("UI")]
    [SerializeField] private GameObject VictoryPanel;
    [SerializeField] private GameObject GameOverPanel;

    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private PlayerHealthUI healthUI;
    [FormerlySerializedAs("HPText")]
    [SerializeField] private TextMeshProUGUI legacyHPText;

    [Header("Victory UI")]
    [SerializeField] private TextMeshProUGUI victoryHP;
    [SerializeField] private TextMeshProUGUI victoryScore;
    [SerializeField] private TextMeshProUGUI victoryKill;
    [SerializeField] private TextMeshProUGUI victoryTotalScore;

    [Header("Attack")]
    [SerializeField] private BoxCollider2D attackCollider;
    [SerializeField, Min(0.01f)] private float attackHitboxDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.7f;

    private int currentHP;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CameraController cameraController;

    private bool isGrounded = true;
    private bool isDead = false;
    private bool canTakeDamage = true;
    private bool levelCompleted = false;
    private bool gameOver = false;
    private bool canControl = true;
    private bool canAttack = true;
    private Coroutine attackRoutine;
    private Coroutine damageCooldownRoutine;
    private Coroutine respawnRoutine;
    private Coroutine respawnInvincibilityRoutine;
    private Coroutine respawnFlashRoutine;
    private SpriteRenderer[] portalRenderers;
    private Color[] portalBaseColors;

    public bool IsLevelCompleted => levelCompleted;
    public bool IsGameOver => gameOver;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cameraController = FindFirstObjectByType<CameraController>();
        currentHP = maxHP;

        UpdateHealthUi();
        ScoreManager.Instance.BindScoreText(score);
        CheckpointManager checkpointManager = CheckpointManager.Instance;
        Vector3 registeredSpawn = checkpointManager.RegisterPlayerSpawn(transform.position);

        if (checkpointManager.PlayerSpawnRestoredFromReload)
        {
            if (rb != null)
            {
                rb.position = registeredSpawn;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                transform.position = registeredSpawn;
            }

            cameraController?.SnapToPlayer();
        }

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    void Update()
    {
        if (isDead || levelCompleted || !canControl) return;

        Move();
        Jump();

        if (Input.GetKeyDown(KeyCode.J) && canAttack)
        {
            Attack();
        }
    }

    //==================== MOVE ====================

    void Move()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            animator.SetBool("ItsRun", true);
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            animator.SetBool("ItsRun", true);
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            animator.SetBool("ItsRun", false);
        }
    }

    //==================== JUMP ====================

    void Jump()
{
    // Nhấn Space để nhảy
    if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        rb.AddForce(Vector2.up * jump, ForceMode2D.Impulse);

        animator.SetBool("ItsJump", true);

        isGrounded = false;

        AudioManager.instance.PlayJump();
    }

    // Nếu đang rơi -> tăng gravity để rơi nhanh hơn
    if (rb.linearVelocity.y < 0)
    {
        rb.gravityScale = fallGravity;
    }
    // Nếu đang bay lên nhưng đã thả Space -> cắt độ cao
    else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
    {
        rb.gravityScale = lowJumpGravity;
    }
    // Đang bay lên và vẫn giữ Space
    else
    {
        rb.gravityScale = normalGravity;
    }

    // Khi chạm đất
    if (isGrounded)
    {
        rb.gravityScale = normalGravity;
        animator.SetBool("ItsJump", false);
    }
}

    //==================== ATTACK ====================

    void Attack()
    {
        canAttack = false;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayAttack();
        }

        animator.SetTrigger("Attack1");
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        AttackHitBox hitBox = attackCollider != null
            ? attackCollider.GetComponent<AttackHitBox>()
            : null;

        hitBox?.BeginAttack();

        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }

        yield return new WaitForSeconds(attackHitboxDuration);

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }

        hitBox?.EndAttack();

        yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown - attackHitboxDuration));

        canAttack = true;
        attackRoutine = null;
    }

    void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            attackCollider.GetComponent<AttackHitBox>()?.EndAttack();
        }

        canAttack = false;
    }

    //==================== DEAD ====================

    private void Dead()
    {
        if (isDead || levelCompleted)
        {
            return;
        }

        isDead = true;
        canTakeDamage = false;
        CancelAttack();

        if (damageCooldownRoutine != null)
        {
            StopCoroutine(damageCooldownRoutine);
            damageCooldownRoutine = null;
        }

        if (respawnInvincibilityRoutine != null)
        {
            StopCoroutine(respawnInvincibilityRoutine);
            respawnInvincibilityRoutine = null;
        }

        if (respawnFlashRoutine != null)
        {
            StopCoroutine(respawnFlashRoutine);
            respawnFlashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (animator != null)
        {
            animator.SetBool("ItsRun", false);
            animator.SetBool("ItsJump", false);
            animator.SetBool("ItsDead", true);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayDeath();
        }

        ResetActiveBossEncounter();
        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(deathDelay);

        Vector3 respawnPosition = CheckpointManager.Instance.RespawnPosition;
        Scene activeScene = SceneManager.GetActiveScene();

        CheckpointManager.PrepareSceneReloadRespawn(activeScene.name, respawnPosition);
        Time.timeScale = 1f;

        // Reloading the scene recreates every destroyed coin and enemy at its
        // authored position. The checkpoint handoff above places the new Player
        // back at the latest checkpoint after the scene has loaded.
        yield return SceneManager.LoadSceneAsync(activeScene.name);
    }

    private IEnumerator RespawnInvincibilityRoutine()
    {
        canTakeDamage = false;
        respawnFlashRoutine = StartCoroutine(RespawnFlashRoutine());

        yield return new WaitForSeconds(respawnInvincibilityDuration);

        if (respawnFlashRoutine != null)
        {
            StopCoroutine(respawnFlashRoutine);
            respawnFlashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (!isDead)
        {
            canTakeDamage = true;
        }

        respawnInvincibilityRoutine = null;
    }

    private IEnumerator RespawnFlashRoutine()
    {
        while (!isDead && !levelCompleted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(respawnFlashInterval);
        }
    }

    private void ResetActiveBossEncounter()
    {
        NecromancerBoss boss = FindFirstObjectByType<NecromancerBoss>();
        if (boss != null)
        {
            boss.ResetBossEncounter();
        }
    }

    //==================== DAMAGE ====================

    private IEnumerator DamageCooldown()
    {
        canTakeDamage = false;

        yield return new WaitForSeconds(0.5f);

        if (!isDead)
        {
            canTakeDamage = true;
        }

        damageCooldownRoutine = null;
    }

    private void RegisterEnemyDefeat(GameObject enemyRoot)
    {
        ScoreReward reward = enemyRoot.GetComponent<ScoreReward>();
        if (reward == null)
        {
            Debug.LogWarning("Enemy '" + enemyRoot.name + "' is missing ScoreReward and awarded no score.");
            return;
        }

        reward.TryAwardDefeat();
    }

    public void BounceAfterEnemyStomp()
    {
        if (rb == null || isDead || levelCompleted || gameOver)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jump, ForceMode2D.Impulse);
        isGrounded = false;

        if (animator != null)
        {
            animator.SetBool("ItsJump", true);
        }
    }

    //==================== COLLISION ====================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = true;
        }

        if (collision.gameObject.CompareTag("hole"))
        {
            Dead();
        }

        if (collision.gameObject.CompareTag("enemy"))
        {
            if (collision.gameObject.GetComponentInParent<NecromancerBoss>() != null)
            {
                collision.gameObject.GetComponentInParent<ContactDamage>()?.ApplyTo(this);
                return;
            }

            ContactPoint2D contact = collision.GetContact(0);

            // Đạp lên đầu quái
            if (contact.normal.y > 0.5f)
            {
                GameObject enemyRoot = collision.rigidbody != null
                    ? collision.rigidbody.gameObject
                    : collision.gameObject;

                RegisterEnemyDefeat(enemyRoot);
                Destroy(enemyRoot);

                rb.AddForce(Vector2.up * jump, ForceMode2D.Impulse);
            }
            else
            {
                ContactDamage contactDamage = collision.gameObject.GetComponentInParent<ContactDamage>();
                if (contactDamage != null)
                {
                    contactDamage.ApplyTo(this);
                }
                else
                {
                    Debug.LogWarning(
                        "Enemy '" + collision.gameObject.name
                        + "' has no ContactDamage component and caused no contact damage.",
                        collision.gameObject);
                }
            }
        }
    }

    //==================== TRIGGER ====================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            ScoreManager.Instance.AddCoin();

            AudioManager.instance.PlayCoin();

            Destroy(collision.gameObject);
        }
        
        if (collision.CompareTag("Heart"))
        {
            if (Heal(1))
            {
                Destroy(collision.gameObject);
            }
        }

    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled && !isDead && !levelCompleted && !gameOver;

        if (!canControl)
        {
            canTakeDamage = false;
            CancelAttack();

            if (animator != null)
            {
                animator.SetBool("ItsRun", false);
            }

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            return;
        }

        canAttack = true;

        if (damageCooldownRoutine == null && respawnInvincibilityRoutine == null)
        {
            canTakeDamage = true;
        }
    }

    public void FaceTowards(float worldX)
    {
        float direction = worldX - transform.position.x;
        if (Mathf.Abs(direction) <= 0.01f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    public IEnumerator MoveToPortalCenter(float portalCenterX, float duration)
    {
        SetControlEnabled(false);

        yield return MoveHorizontallyForPortal(portalCenterX, duration);
    }

    public void PrepareForPortalArrival(float portalCenterX)
    {
        SetControlEnabled(false);

        Vector3 position = transform.position;
        position.x = portalCenterX;
        transform.position = position;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        SetPortalOpacity(0f);
    }

    public IEnumerator MoveFromPortal(float destinationX, float duration)
    {
        SetControlEnabled(false);

        yield return MoveHorizontallyForPortal(destinationX, duration);
    }

    private IEnumerator MoveHorizontallyForPortal(float destinationX, float duration)
    {
        float startX = transform.position.x;
        float direction = destinationX - startX;
        Vector3 scale = transform.localScale;

        if (Mathf.Abs(direction) > 0.01f)
        {
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
            transform.localScale = scale;
        }

        if (animator != null)
        {
            animator.SetBool("ItsRun", true);
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float smoothedT = t * t * (3f - 2f * t);
            Vector3 position = transform.position;
            position.x = Mathf.Lerp(startX, destinationX, smoothedT);
            transform.position = position;
            yield return null;
        }

        Vector3 finalPosition = transform.position;
        finalPosition.x = destinationX;
        transform.position = finalPosition;

        if (animator != null)
        {
            animator.SetBool("ItsRun", false);
        }
    }

    public IEnumerator FadeOutForPortal(float duration)
    {
        yield return FadePortalOpacity(1f, 0f, duration);
    }

    public IEnumerator FadeInFromPortal(float duration)
    {
        yield return FadePortalOpacity(0f, 1f, duration);
    }

    private IEnumerator FadePortalOpacity(float from, float to, float duration)
    {
        CachePortalRenderers();

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        SetPortalOpacity(from);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetPortalOpacity(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration)));
            yield return null;
        }

        SetPortalOpacity(to);
    }

    private void CachePortalRenderers()
    {
        if (portalRenderers != null)
        {
            return;
        }

        portalRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        portalBaseColors = new Color[portalRenderers.Length];

        for (int i = 0; i < portalRenderers.Length; i++)
        {
            portalBaseColors[i] = portalRenderers[i].color;
        }
    }

    private void SetPortalOpacity(float opacity)
    {
        CachePortalRenderers();
        float clampedOpacity = Mathf.Clamp01(opacity);

        for (int i = 0; i < portalRenderers.Length; i++)
        {
            Color color = portalBaseColors[i];
            color.a *= clampedOpacity;
            portalRenderers[i].color = color;
        }
    }

    public void CompleteLevel()
    {
        if (levelCompleted || gameOver)
            return;

        levelCompleted = true;
        CancelAttack();
        AudioManager.instance.PlayVictory();
        if (healthUI != null)
        {
            healthUI.gameObject.SetActive(false);
        }

        if (legacyHPText != null)
        {
            legacyHPText.gameObject.SetActive(false);
        }

        score.gameObject.SetActive(false);

        ScoreManager scores = ScoreManager.Instance;
        victoryHP.text = "HP Remaining: " + currentHP + "/" + maxHP;
        victoryScore.text = "Coins Collected: " + scores.CoinCount;
        victoryKill.text = "Enemies Defeated: " + scores.EnemyKillCount;
        if (victoryTotalScore != null)
        {
            victoryTotalScore.text = "Total Score: " + scores.TotalScore.ToString("D6");
        }
        VictoryPanel.SetActive(true);
    }

    public void TriggerGameOver()
    {
        if (gameOver || levelCompleted)
        {
            return;
        }

        gameOver = true;
        isDead = true;
        canControl = false;
        canTakeDamage = false;

        CancelAttack();
        StopAllCoroutines();

        damageCooldownRoutine = null;
        respawnRoutine = null;
        respawnInvincibilityRoutine = null;
        respawnFlashRoutine = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (animator != null)
        {
            animator.SetBool("ItsRun", false);
            animator.SetBool("ItsJump", false);
            animator.SetBool("ItsDead", true);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayGameOver();
        }

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("PlayerController: GameOverPanel is not assigned.", this);
        }

        Time.timeScale = 0f;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || levelCompleted || !canTakeDamage)
        {
            return;
        }

        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
        UpdateHealthUi();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayHurt();
        }

        if (currentHP <= 0)
        {
            Dead();
            return;
        }

        damageCooldownRoutine = StartCoroutine(DamageCooldown());
    }

    public bool Heal(int amount)
    {
        if (amount <= 0 || isDead || levelCompleted || currentHP >= maxHP)
        {
            return false;
        }

        int previousHealth = currentHP;
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        UpdateHealthUi();
        return currentHP > previousHealth;
    }

    public void ResetHealth()
    {
        currentHP = maxHP;
        UpdateHealthUi();
    }

    private void UpdateHealthUi()
    {
        if (healthUI != null)
        {
            healthUI.SetHealth(currentHP, maxHP);
        }
        else if (legacyHPText != null)
        {
            legacyHPText.text = "HP: " + currentHP;
        }
    }
}
