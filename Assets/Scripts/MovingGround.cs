using UnityEngine;

public class MovingGround : MonoBehaviour
{
    public enum MoveDirection
    {
        Down,
        Up
    }

    [Header("Movement")]
    [SerializeField] private MoveDirection direction = MoveDirection.Down;
    [SerializeField] private float speed = 3f;

    [Header("Position")]
    [SerializeField] private float topY = 8f;
    [SerializeField] private float bottomY = -6f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 position = rb.position;

        if (direction == MoveDirection.Down)
        {
            position.y -= speed * Time.fixedDeltaTime;

            // Xuống dưới → xuất hiện lại trên
            if (position.y <= bottomY)
            {
                position.y = topY;
            }
        }
        else
        {
            position.y += speed * Time.fixedDeltaTime;

            // Lên trên → xuất hiện lại dưới
            if (position.y >= topY)
            {
                position.y = bottomY;
            }
        }

        rb.MovePosition(position);
    }
}