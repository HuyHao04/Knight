using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Damage")]
    [SerializeField] private int damage = 2;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    // Đảm bảo mỗi BAT chỉ gây damage 1 lần cho Player
    private bool hasHitPlayer = false;

    void Start()
    {
        mainCamera = Camera.main;

        spriteRenderer = GetComponent<SpriteRenderer>();

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

        // Không gây damage nhiều lần
        if (hasHitPlayer)
            return;

        PlayerController player =
            collision.GetComponent<PlayerController>();

        if (player != null)
        {
            hasHitPlayer = true;

            // Trừ máu Player
            player.TakeDamage(damage);
        }

        // KHÔNG Destroy BAT
        // BAT tiếp tục bay xuyên qua Player
    }

    // ==================== DIE ====================

    public void Die()
    {
        Destroy(gameObject);
    }
}