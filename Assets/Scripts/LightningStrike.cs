using UnityEngine;
using System.Collections;

public class LightningStrike : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject warningObject;
    [SerializeField] private GameObject lightningObject;

    [Header("Timing")]
    [SerializeField] private float warningTime = 0.8f;
    [SerializeField] private float lightningDuration = 0.4f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int damage = 3;

    private bool canDamage = false;
    private bool hasHitPlayer = false;

    private void Start()
    {
        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        // =========================
        // WARNING
        // =========================

        warningObject.SetActive(true);
        lightningObject.SetActive(false);

        yield return new WaitForSeconds(warningTime);

        // =========================
        // LIGHTNING
        // =========================

        warningObject.SetActive(false);
        lightningObject.SetActive(true);

        canDamage = true;

        yield return new WaitForSeconds(lightningDuration);

        // =========================
        // END
        // =========================

        canDamage = false;

        Destroy(gameObject);
    }

    public void TryDamage(PlayerController player)
    {
        if (!canDamage || hasHitPlayer || player == null)
        {
            return;
        }

        hasHitPlayer = true;
        player.TakeDamage(damage);
    }
}
