using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 5f;

    private Vector2 direction;

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;

        // Xoay mũi tên theo hướng bay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        // Tự hủy sau một khoảng thời gian
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Bay thẳng
        transform.position +=
            (Vector3)direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu trúng Player
        if (collision.CompareTag("Player"))
        {
            PlayerController player =
                collision.GetComponent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(2);
            }

            Destroy(gameObject);
        }

        // Nếu đụng Ground
        if (collision.CompareTag("ground"))
        {
            Destroy(gameObject);
        }
    }
}