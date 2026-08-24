using System.Collections;
using UnityEngine;

public class GroundSpell : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject warningObject;
    [SerializeField] private GameObject attackObject;

    [Header("Timing")]
    [SerializeField] private float warningTime = 1f;
    [SerializeField] private float attackDuration = 0.5f;

    [Header("Damage")]
    [SerializeField] private int damage = 3;

    private Collider2D damageCollider;

    private bool canDamage = false;
    private bool hasHitPlayer = false;

    private void Awake()
    {
        damageCollider = GetComponent<Collider2D>();

        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }
    }

    private void Start()
    {
        if (warningObject != null)
        {
            warningObject.SetActive(true);
        }

        if (attackObject != null)
        {
            attackObject.SetActive(false);
        }

        StartCoroutine(GroundAttackRoutine());
    }

    private IEnumerator GroundAttackRoutine()
    {
        // =========================
        // WARNING
        // =========================

        yield return new WaitForSeconds(warningTime);

        if (warningObject != null)
        {
            warningObject.SetActive(false);
        }

        // =========================
        // ATTACK
        // =========================

        if (attackObject != null)
        {
            attackObject.SetActive(true);
        }

        canDamage = true;

        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        // Thời gian spell gây damage
        yield return new WaitForSeconds(attackDuration);

        // =========================
        // END ATTACK
        // =========================

        canDamage = false;

        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DamagePlayer(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        DamagePlayer(collision);
    }

    private void DamagePlayer(Collider2D collision)
    {
        if (!canDamage)
            return;

        if (hasHitPlayer)
            return;

        if (!collision.CompareTag("Player"))
            return;

        PlayerController player =
            collision.GetComponent<PlayerController>();

        if (player != null)
        {
            hasHitPlayer = true;

            player.TakeDamage(damage);
        }
    }
}