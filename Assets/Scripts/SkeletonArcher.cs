using UnityEngine;

public class SkeletonArcher : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Detect Player")]
    [SerializeField] private float detectRange = 8f;

    private Transform player;
    private Animator animator;

    private float attackTimer;

    void Start()
    {
        animator = GetComponent<Animator>();

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        attackTimer = attackCooldown;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance =
            Vector2.Distance(transform.position, player.position);

        if (distance <= detectRange)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
    }

    void Attack()
    {
        animator.SetTrigger("Attack");

        FacePlayer();
    }

    void FacePlayer()
    {
        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    // Animation Event gọi function này
    public void ShootArrow()
    {
        if (arrowPrefab == null || shootPoint == null)
            return;

        GameObject arrow =
            Instantiate(
                arrowPrefab,
                shootPoint.position,
                Quaternion.identity
            );

        Arrow arrowScript =
            arrow.GetComponent<Arrow>();

        if (arrowScript != null)
        {
            Vector2 direction =
                (player.position - shootPoint.position).normalized;

            arrowScript.SetDirection(direction);
        }
    }
    public void Die()
{
    Destroy(gameObject);
}
}