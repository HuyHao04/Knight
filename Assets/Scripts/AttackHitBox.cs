using UnityEngine;
using System.Collections.Generic;

public class AttackHitBox : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }
    }

    public void BeginAttack()
    {
        hitTargets.Clear();
    }

    public void EndAttack()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (player == null)
        {
            return;
        }

        // Boss is checked before normal enemies so it always uses its own health system.
        NecromancerBoss boss = collision.GetComponentInParent<NecromancerBoss>();
        if (boss != null)
        {
            if (hitTargets.Add(boss.gameObject))
            {
                boss.TakeDamage(10);
            }

            return;
        }

        // Each normal enemy can only be hit once during the current attack window.
        ScoreReward reward = collision.GetComponentInParent<ScoreReward>();
        if (collision.CompareTag("enemy") || reward != null)
        {
            GameObject enemyRoot = collision.attachedRigidbody != null
                ? collision.attachedRigidbody.gameObject
                : collision.gameObject;

            if (!hitTargets.Add(enemyRoot))
            {
                return;
            }

            reward = enemyRoot.GetComponent<ScoreReward>();
            if (reward == null)
            {
                Debug.LogWarning("Enemy '" + enemyRoot.name + "' is missing ScoreReward and awarded no score.");
            }
            else
            {
                reward.TryAwardDefeat();
            }

            Destroy(enemyRoot);
        }
    }
}
