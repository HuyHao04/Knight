using TMPro;
using UnityEngine;

public sealed class LevelCountdownTimer : MonoBehaviour
{
    [Header("Countdown")]
    [SerializeField, Min(1f)] private float durationSeconds = 300f;
    [SerializeField] private PlayerController player;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Warning Colors")]
    [SerializeField, Min(0f)] private float warningThreshold = 60f;
    [SerializeField, Min(0f)] private float criticalThreshold = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.2f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.25f, 0.2f);

    private float remainingSeconds;
    private int displayedSecond = -1;
    private bool expired;

    public float RemainingSeconds => remainingSeconds;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TextMeshProUGUI>();
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>(
                FindObjectsInactive.Include
            );
        }

        remainingSeconds = Mathf.Max(1f, durationSeconds);
        RefreshDisplay(true);
    }

    private void Update()
    {
        if (expired)
        {
            return;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>(
                FindObjectsInactive.Include
            );

            if (player == null)
            {
                return;
            }
        }

        if (player.IsLevelCompleted || player.IsGameOver)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        RefreshDisplay(false);

        if (remainingSeconds <= 0f)
        {
            expired = true;
            player.TriggerGameOver();
        }
    }

    private void RefreshDisplay(bool force)
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        if (!force && totalSeconds == displayedSecond)
        {
            return;
        }

        displayedSecond = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (remainingSeconds <= criticalThreshold)
        {
            timerText.color = criticalColor;
        }
        else if (remainingSeconds <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }
}
