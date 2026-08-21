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
        // Load volume đã lưu
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        // Hiển thị Main Panel
        ShowMainPanel();

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
        SceneManager.LoadScene("Level_2");
    }


    public void OpenLevelPanel()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        optionPanel.SetActive(false);
    }


    public void OpenOptionPanel()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(false);
        optionPanel.SetActive(true);
    }


    public void BackToMainMenu()
    {
        ShowMainPanel();
    }


    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        optionPanel.SetActive(false);
    }


    // ==================================================
    // LEVEL SELECT
    // ==================================================

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level_1");
    }


    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level_2");
    }


    public void LoadLevel3()
    {
        SceneManager.LoadScene("Level_3");
    }


    // ==================================================
    // OPTION - MUSIC
    // ==================================================

    public void MusicVolumeUp()
    {
        musicVolume += 0.1f;

        musicVolume = Mathf.Clamp01(musicVolume);

        musicMuted = false;

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


    public void ToggleMusic()
    {
        musicMuted = !musicMuted;

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

        sfxMuted = false;

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


    public void ToggleSFX()
    {
        sfxMuted = !sfxMuted;

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

        AudioManager.instance.SetMusicVolume(
            musicMuted ? 0f : musicVolume
        );

        AudioManager.instance.SetSFXVolume(
            sfxMuted ? 0f : sfxVolume
        );
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
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        PlayerPrefs.SetInt(
            "MusicMuted",
            musicMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            "SFXMuted",
            sfxMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    // ==================================================
    // RESTART LEVEL
    // ==================================================

    public void PlayAgain()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    // ==================================================
    // BACK TO MAIN MENU
    // ==================================================

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }
}