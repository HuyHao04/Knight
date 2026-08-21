using UnityEngine;

public class VerticalMovingPlatforms : MonoBehaviour
{
    [Header("Platforms")]
    [SerializeField] private Rigidbody2D platformA;
    [SerializeField] private Rigidbody2D platformB;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float speed = 1f;

    private Vector2 startA;
    private Vector2 startB;

    private float time;

    private void Start()
    {
        startA = platformA.position;
        startB = platformB.position;
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime * speed;

        float movement = Mathf.Sin(time);

        Vector2 newPositionA =
            startA + Vector2.up * movement * moveDistance;

        Vector2 newPositionB =
            startB - Vector2.up * movement * moveDistance;

        platformA.MovePosition(newPositionA);
        platformB.MovePosition(newPositionB);
    }
}