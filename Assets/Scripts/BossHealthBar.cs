using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    public void SetHealth(int currentHP, int maxHP)
    {
        // Cập nhật thanh máu
        if (fillImage != null)
        {
            float healthPercent = (float)currentHP / maxHP;
            fillImage.fillAmount = healthPercent;
        }

        // Cập nhật số HP
        if (healthText != null)
        {
            healthText.text = currentHP + " / " + maxHP;
        }
        else
        {
            Debug.LogWarning("HealthText chưa được gán vào BossHealthBar!");
        }
    }
}