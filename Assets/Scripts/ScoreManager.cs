using TMPro;
using UnityEngine;

/// <summary>
/// Owns all score-related state for the currently loaded level.
/// This object is deliberately not persistent, so a fresh load/restart starts at zero.
/// </summary>
public sealed class ScoreManager : MonoBehaviour
{
    private const int CoinScoreValue = 100;

    private static ScoreManager instance;

    [SerializeField] private TextMeshProUGUI scoreText;

    public static ScoreManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<ScoreManager>();
            if (instance != null)
            {
                return instance;
            }

            GameObject managerObject = new GameObject("ScoreManager");
            instance = managerObject.AddComponent<ScoreManager>();
            return instance;
        }
    }

    public int CoinCount { get; private set; }
    public int EnemyKillCount { get; private set; }
    public int TotalScore { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResetForCurrentLevel();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void BindScoreText(TextMeshProUGUI hudScoreText)
    {
        scoreText = hudScoreText;
        RefreshHud();
    }

    public void AddCoin()
    {
        CoinCount++;
        AddScore(CoinScoreValue);
    }

    public void AddEnemyDefeat(int reward)
    {
        EnemyKillCount++;
        AddScore(Mathf.Max(0, reward));
    }

    public void ResetForCurrentLevel()
    {
        CoinCount = 0;
        EnemyKillCount = 0;
        TotalScore = 0;
        RefreshHud();
    }

    private void AddScore(int amount)
    {
        TotalScore += amount;
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE " + TotalScore.ToString("D6");
        }
    }
}
