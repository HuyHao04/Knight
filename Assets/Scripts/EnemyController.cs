using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float patrolDistance = 3f;

    [Header("Detect Player")]
    public float detectRange = 5f;

    private Vector3 startPosition;
    private Vector3 originalScale;

    private Transform player;
    private Rigidbody2D rb;

    private bool movingRight = true;

    void Start()
    {
        startPosition = transform.position;
        originalScale = transform.localScale;

        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            Patrol();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance <= detectRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    // ==================== PATROL ====================

    void Patrol()
    {
        if (movingRight)
        {
            rb.linearVelocity = new Vector2(
                moveSpeed,
                rb.linearVelocity.y
            );

            if (transform.position.x >= startPosition.x + patrolDistance)
            {
                movingRight = false;
                Flip();
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(
                -moveSpeed,
                rb.linearVelocity.y
            );

            if (transform.position.x <= startPosition.x - patrolDistance)
            {
                movingRight = true;
                Flip();
            }
        }
    }

    // ==================== CHASE ====================

    void ChasePlayer()
    {
        float directionX = player.position.x - transform.position.x;

        if (directionX > 0)
        {
            rb.linearVelocity = new Vector2(
                moveSpeed,
                rb.linearVelocity.y
            );

            FaceRight();
        }
        else if (directionX < 0)
        {
            rb.linearVelocity = new Vector2(
                -moveSpeed,
                rb.linearVelocity.y
            );

            FaceLeft();
        }
        else
        {
            rb.linearVelocity = new Vector2(
                0,
                rb.linearVelocity.y
            );
        }
    }

    // ==================== FLIP ====================

    void FaceRight()
    {
        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x),
            originalScale.y,
            originalScale.z
        );
    }

    void FaceLeft()
    {
        transform.localScale = new Vector3(
            -Mathf.Abs(originalScale.x),
            originalScale.y,
            originalScale.z
        );
    }

    void Flip()
    {
        if (movingRight)
        {
            FaceRight();
        }
        else
        {
            FaceLeft();
        }
    }

    // ==================== DIE ====================

    public void Die()
    {
        Destroy(gameObject);
    }
}