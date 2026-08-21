using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    private PlayerController player;

    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("enemy"))
            return;

        if (player != null)
        {
            player.EnemyKilled();
        }

        SkeletonArcher skeleton =
            collision.GetComponentInParent<SkeletonArcher>();

        if (skeleton != null)
        {
            skeleton.Die();
            return;
        }

        EnemyController enemy =
            collision.GetComponentInParent<EnemyController>();

        if (enemy != null)
        {
            enemy.Die();
            return;
        }

        Destroy(collision.gameObject);
    }
}