using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 2f;

    [Header("Limit")]
    [SerializeField] private float maxFallDistance = 5f;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private bool playerOnPlatform = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
    }

    private void FixedUpdate()
    {
        if (!playerOnPlatform)
            return;

        float maxY = startPosition.y - maxFallDistance;

        Vector2 newPosition = rb.position;

        newPosition.y -= fallSpeed * Time.fixedDeltaTime;

        if (newPosition.y < maxY)
        {
            newPosition.y = maxY;
        }

        rb.MovePosition(newPosition);
    }

    public void PlayerEnter()
    {
        playerOnPlatform = true;
    }

    public void PlayerExit()
    {
        playerOnPlatform = false;
    }
}