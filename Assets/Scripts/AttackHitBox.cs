using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Enemy thường
        if (collision.CompareTag("enemy"))
        {
            player.EnemyKilled();
            Destroy(collision.gameObject);
            return;
        }

        // Boss
        if (collision.CompareTag("Boss"))
        {
            NecromancerBoss boss =
                collision.GetComponent<NecromancerBoss>();

            if (boss != null)
            {
                boss.TakeDamage(10);
            }
        }
    }
}