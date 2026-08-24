using UnityEngine;

public class LightningDamage : MonoBehaviour
{
    private LightningStrike lightningStrike;

    private void Start()
    {
        lightningStrike = GetComponentInParent<LightningStrike>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (lightningStrike == null)
            return;

        if (!lightningStrike.CanDamage())
            return;

        PlayerController player =
            collision.GetComponent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage(lightningStrike.GetDamage());
        }
    }
}