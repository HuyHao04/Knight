using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ButtonManager : MonoBehaviour
{
    // ==================================================
    // MAIN MENU PANELS
    // ==================================================

    [Header("Main Menu Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject optionPanel;

    [Header("Level Navigation")]
    [SerializeField] private string nextLevelScene = "Level_3";


    // ==================================================
    // VOLUME UI
    // ==================================================

    [Header("Volume UI")]
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;


    // ==================================================
    // VOLUME DATA
    // ==================================================

    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private bool musicMuted = false;
    private bool sfxMuted = false;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        // AudioManager is the source of truth whenever it already exists.
        // This prevents the Options UI and real audio from getting out of sync.
        if (AudioManager.instance != null)
        {
            musicVolume = AudioManager.instance.MusicVolume;
            sfxVolume = AudioManager.instance.SfxVolume;
            musicMuted = AudioManager.instance.MusicMuted;
            sfxMuted = AudioManager.instance.SfxMuted;
        }
        else
        {
            musicVolume = PlayerPrefs.GetFloat(
                AudioManager.MusicVolumeKey,
                1f
            );
            sfxVolume = PlayerPrefs.GetFloat(
                AudioManager.SfxVolumeKey,
                1f
            );
            musicMuted = PlayerPrefs.GetInt(
                AudioManager.MusicMutedKey,
                0
            ) == 1;
            sfxMuted = PlayerPrefs.GetInt(
                AudioManager.SfxMutedKey,
                0
            ) == 1;
        }

        // Main-menu panels are not assigned on gameplay scenes.
        if (mainPanel != null || levelPanel != null || optionPanel != null)
        {
            ShowMainPanel();
        }

        // Cập nhật UI
        UpdateVolumeUI();

        // Áp dụng volume
        ApplyVolume();
    }


    // ==================================================
    // MAIN MENU
    // ==================================================

    public void play()
    {
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Level_1");
    }


    public void OpenLevelPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelPanel != null) levelPanel.SetActive(true);
        if (optionPanel != null) optionPanel.SetActive(false);
    }


    public void OpenOptionPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelPanel != null) levelPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(true);
    }


    public void BackToMainMenu()
    {
        ShowMainPanel();
    }


    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelPanel != null) levelPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
    }


    // ==================================================
    // LEVEL SELECT
    // ==================================================

    public void LoadLevel1()
    {
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Level_1");
    }


    public void LoadLevel2()
    {
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Level_2");
    }


    public void LoadLevel3()
    {
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Level_3");
    }


    public void LoadBoss()
    {
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Boss");
    }


    // ==================================================
    // OPTION - MUSIC
    // ==================================================

    public void MusicVolumeUp()
    {
        musicVolume += 0.1f;

        musicVolume = Mathf.Clamp01(musicVolume);

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    public void MusicVolumeDown()
    {
        musicVolume -= 0.1f;

        musicVolume = Mathf.Clamp01(musicVolume);

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // Compatible with Slider.onValueChanged(float), if sliders are added later.
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    public void ToggleMusic()
    {
        musicMuted = !musicMuted;

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // Compatible with Toggle.onValueChanged(bool), if toggles are added later.
    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // ==================================================
    // OPTION - SFX
    // ==================================================

    public void SFXVolumeUp()
    {
        sfxVolume += 0.1f;

        sfxVolume = Mathf.Clamp01(sfxVolume);

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    public void SFXVolumeDown()
    {
        sfxVolume -= 0.1f;

        sfxVolume = Mathf.Clamp01(sfxVolume);

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // Compatible with Slider.onValueChanged(float), if sliders are added later.
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    public void ToggleSFX()
    {
        sfxMuted = !sfxMuted;

        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // Compatible with Toggle.onValueChanged(bool), if toggles are added later.
    public void SetSFXMuted(bool muted)
    {
        sfxMuted = muted;
        SaveVolume();
        UpdateVolumeUI();
        ApplyVolume();
    }


    // ==================================================
    // AUDIO
    // ==================================================

    private void ApplyVolume()
    {
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.SetMusicVolume(musicVolume);
        AudioManager.instance.SetSFXVolume(sfxVolume);
        AudioManager.instance.SetMusicMuted(musicMuted);
        AudioManager.instance.SetSFXMuted(sfxMuted);
    }


    // ==================================================
    // UPDATE VOLUME TEXT
    // ==================================================

    private void UpdateVolumeUI()
    {
        if (musicVolumeText != null)
        {
            int value = Mathf.RoundToInt(musicVolume * 100f);

            if (musicMuted)
            {
                musicVolumeText.text = "MUTE";
            }
            else
            {
                musicVolumeText.text = value + "%";
            }
        }


        if (sfxVolumeText != null)
        {
            int value = Mathf.RoundToInt(sfxVolume * 100f);

            if (sfxMuted)
            {
                sfxVolumeText.text = "MUTE";
            }
            else
            {
                sfxVolumeText.text = value + "%";
            }
        }
    }


    // ==================================================
    // SAVE VOLUME
    // ==================================================

    private void SaveVolume()
    {
        PlayerPrefs.SetFloat(AudioManager.MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(AudioManager.SfxVolumeKey, sfxVolume);

        PlayerPrefs.SetInt(
            AudioManager.MusicMutedKey,
            musicMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            AudioManager.SfxMutedKey,
            sfxMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    // ==================================================
    // RESTART LEVEL
    // ==================================================

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        ScoreManager.StartNewRun();
        SceneManager.LoadScene("Level_1");
    }


    // ==================================================
    // BACK TO MAIN MENU
    // ==================================================

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void NextLevel()
    {
        ScoreManager.Instance.CommitCurrentLevelToRun();
        SceneManager.LoadScene(nextLevelScene);
    }
}
