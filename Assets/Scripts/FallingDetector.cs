using UnityEngine;

public class FallingPlatformDetector : MonoBehaviour
{
    private FallingPlatform platform;

    private void Start()
    {
        platform = GetComponentInParent<FallingPlatform>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            platform.PlayerEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            platform.PlayerExit();
        }
    }
}