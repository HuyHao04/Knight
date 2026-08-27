using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int damage = 1;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Collider2D hitbox;

    // Đảm bảo mỗi BAT chỉ gây damage 1 lần cho Player
    private bool hasHitPlayer = false;
    private bool isDead;

    void Start()
    {
        mainCamera = Camera.main;

        spriteRenderer = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();

        // BAT mặc định nhìn sang phải
        // Flip lại để nhìn sang trái
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }
    }

    void Update()
    {
        // Bay từ phải sang trái
        transform.Translate(
            Vector2.left * moveSpeed * Time.deltaTime
        );

        // Destroy khi bay ra khỏi màn hình bên trái
        if (mainCamera != null)
        {
            Vector3 screenPos =
                mainCamera.WorldToViewportPoint(transform.position);

            if (screenPos.x < -0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    // ==================== PLAYER DAMAGE ====================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        PlayerController player = collision.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        // Bat dùng trigger để có thể bay xuyên qua Player, vì vậy cú đạp phải
        // được nhận diện tại đây thay vì OnCollisionEnter2D như Slime.
        if (CanBeStompedBy(collision))
        {
            player.BounceAfterEnemyStomp();
            Die();
            return;
        }

        // Không gây damage nhiều lần
        if (hasHitPlayer)
            return;

        hasHitPlayer = true;

        // Trừ máu Player khi va từ bên cạnh hoặc từ dưới lên.
        player.TakeDamage(damage);

        // KHÔNG Destroy BAT
        // BAT tiếp tục bay xuyên qua Player
    }

    private bool CanBeStompedBy(Collider2D playerCollider)
    {
        Rigidbody2D playerBody = playerCollider.attachedRigidbody;
        if (playerBody == null || playerBody.linearVelocity.y > 0.05f)
        {
            return false;
        }

        float batCenterY = hitbox != null
            ? hitbox.bounds.center.y
            : transform.position.y;

        // Chân Player phải ở phía trên tâm Bat. Điều kiện này loại bỏ va chạm
        // ngang ngay cả khi trigger tròn của Bat chạm Player từ khá xa.
        return playerCollider.bounds.min.y >= batCenterY;
    }

    // ==================== DIE ====================

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        GetComponent<ScoreReward>()?.TryAwardDefeat();
        Destroy(gameObject);
    }
}
