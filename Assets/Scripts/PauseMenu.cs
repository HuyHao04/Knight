using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Mobile")]
    [SerializeField] private GameObject mobileControls;

    private bool isPaused = false;

    private void Start()
    {
        Time.timeScale = 1f;

        pausePanel.SetActive(false);

        // Nếu đang build PC thì ẩn Mobile Controls
#if UNITY_STANDALONE || UNITY_EDITOR
        if (mobileControls != null)
        {
            mobileControls.SetActive(false);
        }
#endif

        // Nếu build Mobile thì hiện Mobile Controls
#if UNITY_ANDROID || UNITY_IOS
        if (mobileControls != null)
        {
            mobileControls.SetActive(true);
        }
#endif
    }

    private void Update()
    {
        // ESC dùng cho PC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // =========================
    // TOGGLE PAUSE
    // =========================

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    // =========================
    // PAUSE
    // =========================

    public void Pause()
    {
        isPaused = true;

        pausePanel.SetActive(true);

        Time.timeScale = 0f;
    }

    // =========================
    // RESUME
    // =========================

    public void Resume()
    {
        isPaused = false;

        pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    // =========================
    // RESTART
    // =========================

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // =========================
    // MAIN MENU
    // =========================

    public void MainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}