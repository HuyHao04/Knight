using UnityEngine;

public class HorizontalMovingPlatforms : MonoBehaviour
{
    [Header("Platforms")]
    [SerializeField] private Rigidbody2D platformC;
    [SerializeField] private Rigidbody2D platformD;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 4f;
    [SerializeField] private float speed = 1f;

    private Vector2 startC;
    private Vector2 startD;

    private float time;

    private void Start()
    {
        startC = platformC.position;
        startD = platformD.position;
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime * speed;

        float movement = Mathf.Sin(time);

        Vector2 newPositionC =
            startC + Vector2.right * movement * moveDistance;

        Vector2 newPositionD =
            startD - Vector2.right * movement * moveDistance;

        platformC.MovePosition(newPositionC);
        platformD.MovePosition(newPositionD);
    }
}