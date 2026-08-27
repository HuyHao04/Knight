using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;

    public void ApplyTo(PlayerController player)
    {
        if (player != null && damage > 0)
        {
            player.TakeDamage(damage);
        }
    }
}
