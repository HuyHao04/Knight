using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    [SerializeField] private float jump = 5f;
    [Header("Jump Settings")]
    [SerializeField] private float normalGravity = 1f;
    [SerializeField] private float fallGravity = 2.5f;
    [SerializeField] private float lowJumpGravity = 3f;

    [Header("UI")]
    [SerializeField] private GameObject VictoryPanel;
    [SerializeField] private GameObject GameOverPanel;

    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI HPText;

    [Header("Victory UI")]
    [SerializeField] private TextMeshProUGUI victoryHP;
    [SerializeField] private TextMeshProUGUI victoryScore;
    [SerializeField] private TextMeshProUGUI victoryKill;

    [Header("Attack")]
    [SerializeField] private BoxCollider2D attackCollider;

    private int HP = 10;
    private int coin = 0;
    private int enemyKill = 0;

    private Animator animator;
    private Rigidbody2D rb;

    private bool isGrounded = true;
    private bool isDead = false;
    private bool canTakeDamage = true;
    private bool levelCompleted = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        HPText.text = "HP: " + HP;
        score.text = "Score: " + coin;

        attackCollider.enabled = false;
    }

    void Update()
    {
        if (isDead || levelCompleted) return;

        Move();
        Jump();

        if (Input.GetKeyDown(KeyCode.J))
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
        AudioManager.instance.PlayAttack();
        animator.SetTrigger("Attack1");
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        attackCollider.enabled = true;

        yield return new WaitForSeconds(0.2f);

        attackCollider.enabled = false;
    }

    //==================== DEAD ====================

    void Dead()
    {
        if (isDead) return;

        isDead = true;
        AudioManager.instance.PlayGameOver();
        animator.SetBool("ItsDead", true);

        StartCoroutine(DeadRoutine());
    }

    IEnumerator DeadRoutine()
    {
        yield return new WaitForSeconds(1f);

        GameOverPanel.SetActive(true);

        Destroy(gameObject, 0.5f);
    }

    //==================== DAMAGE ====================

    IEnumerator DamageCooldown()
    {
        canTakeDamage = false;

        yield return new WaitForSeconds(0.5f);

        canTakeDamage = true;
    }

    public void EnemyKilled()
    {
        enemyKill++;
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
            ContactPoint2D contact = collision.GetContact(0);

            // Đạp lên đầu quái
            if (contact.normal.y > 0.5f)
            {
                EnemyKilled();

                Destroy(collision.gameObject);

                rb.AddForce(Vector2.up * jump, ForceMode2D.Impulse);
            }
            else
            {
                TakeDamage(2);
            }
        }
    }

    //==================== TRIGGER ====================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            coin++;

            score.text = "Score: " + coin;

            AudioManager.instance.PlayCoin();

            Destroy(collision.gameObject);
        }
        
        if (collision.CompareTag("Heart"))
        {
            HP++;

            HPText.text = "HP: " + HP;

            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("gate"))
        {
            CompleteLevel();
            Destroy(gameObject, 0.5f);
        }
    }

    public void CompleteLevel()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;
        AudioManager.instance.PlayVictory();
        HPText.gameObject.SetActive(false);
        score.gameObject.SetActive(false);

        victoryHP.text = "HP Remaining : " + HP;
        victoryScore.text = "Coins : " + coin;
        victoryKill.text = "Enemies Defeated : " + enemyKill;
        VictoryPanel.SetActive(true);
    }
    public void TakeDamage(int damage)
{
    if (isDead)
        return;

    if (!canTakeDamage)
        return;

    HP -= damage;

    HP = Mathf.Max(HP, 0);

    HPText.text = "HP: " + HP;

    if (AudioManager.instance != null)
    {
        AudioManager.instance.PlayHurt();
    }

    StartCoroutine(DamageCooldown());

    if (HP <= 0)
    {
        Dead();
    }
}
}
