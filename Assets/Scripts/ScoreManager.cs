using TMPro;
using UnityEngine;

/// <summary>
/// Owns score state for the current level and the accumulated campaign run.
/// The scene object remains level-local; only committed totals survive scene changes.
/// </summary>
public sealed class ScoreManager : MonoBehaviour
{
    private const int CoinScoreValue = 100;

    private static ScoreManager instance;
    private static int completedLevelCoins;
    private static int completedLevelEnemyKills;
    private static int completedLevelScore;

    [SerializeField] private TextMeshProUGUI scoreText;

    private bool currentLevelCommitted;

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

    public int RunCoinCount => completedLevelCoins + CoinCount;
    public int RunEnemyKillCount => completedLevelEnemyKills + EnemyKillCount;
    public int RunTotalScore => completedLevelScore + TotalScore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
        completedLevelCoins = 0;
        completedLevelEnemyKills = 0;
        completedLevelScore = 0;
    }

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
        currentLevelCommitted = false;
        RefreshHud();
    }

    /// <summary>
    /// Stores this level's result before a portal loads the next scene.
    /// Safe to call more than once during the same transition.
    /// </summary>
    public void CommitCurrentLevelToRun()
    {
        if (currentLevelCommitted)
        {
            return;
        }

        completedLevelCoins += CoinCount;
        completedLevelEnemyKills += EnemyKillCount;
        completedLevelScore += TotalScore;
        currentLevelCommitted = true;
    }

    /// <summary>
    /// Starts a clean campaign when Play or a level-select button is used.
    /// </summary>
    public static void StartNewRun()
    {
        completedLevelCoins = 0;
        completedLevelEnemyKills = 0;
        completedLevelScore = 0;

        if (instance != null)
        {
            instance.ResetForCurrentLevel();
        }
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
            // Keep the HUD continuous across Level 1, Level 2, Level 3 and Boss.
            // TotalScore is only this scene; RunTotalScore also includes levels
            // already committed when the player entered their exit portals.
            scoreText.text = "SCORE " + RunTotalScore.ToString("D6");
        }
    }
}
