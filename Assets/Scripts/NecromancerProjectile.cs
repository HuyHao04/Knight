using UnityEngine;

public class NecromancerProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private int damage = 2;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;
    private bool hasHitPlayer = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Tự hủy nếu bay quá lâu
        Destroy(gameObject, lifeTime);
    }

    public void SetDirection(Vector2 direction)
    {
        if (rb == null)
            return;

        direction = direction.normalized;

        rb.linearVelocity = direction * speed;

        Debug.Log(
            "Projectile spawned! Direction: "
            + direction
            + " Velocity: "
            + rb.linearVelocity
        );
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // =========================
        // PLAYER
        // =========================

        if (collision.CompareTag("Player"))
        {
            if (hasHitPlayer)
                return;

            hasHitPlayer = true;

            PlayerController player =
                collision.GetComponent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }

            // Fireball biến mất sau khi trúng Player
            Destroy(gameObject);

            return;
        }

        // =========================
        // GROUND
        // =========================

        if (collision.CompareTag("ground"))
        {
            Destroy(gameObject);

            return;
        }
    }
}