using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField, Range(0f, 1f)] private float emptyHeartAlpha = 0.25f;

    public void SetHealth(int currentHealth, int maxHealth)
    {
        int visibleHearts = Mathf.Clamp(maxHealth, 0, heartImages?.Length ?? 0);
        int fullHearts = Mathf.Clamp(currentHealth, 0, visibleHearts);

        for (int i = 0; i < (heartImages?.Length ?? 0); i++)
        {
            Image heart = heartImages[i];
            if (heart == null)
            {
                continue;
            }

            bool shouldBeVisible = i < visibleHearts;
            bool isFull = i < fullHearts;
            heart.enabled = shouldBeVisible;

            if (!shouldBeVisible)
            {
                continue;
            }

            heart.sprite = isFull || emptyHeartSprite == null
                ? fullHeartSprite
                : emptyHeartSprite;
            heart.color = new Color(1f, 1f, 1f, isFull ? 1f : emptyHeartAlpha);
        }
    }
}
