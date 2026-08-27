using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public const string MusicVolumeKey = "MusicVolume";
    public const string SfxVolumeKey = "SFXVolume";
    public const string MusicMutedKey = "MusicMuted";
    public const string SfxMutedKey = "SFXMuted";

    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip victoryClip;

    [Header("SFX")]
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip hitEnemyClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip portalClip;

    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool MusicMuted { get; private set; }
    public bool SfxMuted { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("Duplicate AudioManager destroyed.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("AudioManager initialized.");
    }

    private void Start()
    {
        // The sceneLoaded callback normally selects the clip first. This is a
        // fallback for unusual startup order when the source is still silent.
        if (musicSource == null || !musicSource.isPlaying)
        {
            PlayMusicForScene(SceneManager.GetActiveScene(), false);
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A new level resumes either its override or the shared level BGM after
        // GameOver/Victory. Only scenes with SceneMusicOverride replace bgmClip.
        PlayMusicForScene(scene, true);
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        ApplyMusicSettings();
        SaveSettings();
        Debug.Log("SetMusicVolume: " + MusicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        ApplySfxSettings();
        SaveSettings();
        Debug.Log("SetSFXVolume: " + SfxVolume);
    }

    public void SetMusicMuted(bool muted)
    {
        MusicMuted = muted;
        ApplyMusicSettings();
        SaveSettings();
        Debug.Log("MusicMuted: " + MusicMuted);
    }

    public void SetSFXMuted(bool muted)
    {
        SfxMuted = muted;
        ApplySfxSettings();
        SaveSettings();
        Debug.Log("SFXMuted: " + SfxMuted);
    }

    private void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;

        Debug.Log(
            "Loaded audio settings - Music: " + MusicVolume +
            ", SFX: " + SfxVolume +
            ", MusicMuted: " + MusicMuted +
            ", SFXMuted: " + SfxMuted
        );
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.SetInt(MusicMutedKey, MusicMuted ? 1 : 0);
        PlayerPrefs.SetInt(SfxMutedKey, SfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        ApplyMusicSettings();
        ApplySfxSettings();
    }

    private void ApplyMusicSettings()
    {
        if (musicSource == null)
            return;

        musicSource.volume = MusicVolume;
        musicSource.mute = MusicMuted;
    }

    private void ApplySfxSettings()
    {
        if (sfxSource == null)
            return;

        sfxSource.volume = SfxVolume;
        sfxSource.mute = SfxMuted;
    }

    private void PlayBackgroundMusic()
    {
        PlayMusicClip(bgmClip, false);
    }

    public void PlaySceneMusic(AudioClip clip, bool restart = true)
    {
        PlayMusicClip(clip != null ? clip : bgmClip, restart);
    }

    private void PlayMusicForScene(Scene scene, bool allowRestart)
    {
        SceneMusicOverride sceneOverride = null;

        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                sceneOverride = rootObject.GetComponentInChildren<SceneMusicOverride>(true);

                if (sceneOverride != null)
                {
                    break;
                }
            }
        }

        if (sceneOverride != null && sceneOverride.MusicClip != null)
        {
            PlayMusicClip(
                sceneOverride.MusicClip,
                allowRestart && sceneOverride.RestartOnSceneLoad
            );
            return;
        }

        PlayBackgroundMusic();
    }

    private void PlayMusicClip(AudioClip clip, bool restart)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        bool clipChanged = musicSource.clip != clip;

        if (clipChanged || restart)
        {
            musicSource.Stop();
            musicSource.clip = clip;
        }

        musicSource.loop = true;
        ApplyMusicSettings();

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        ApplySfxSettings();
        sfxSource.PlayOneShot(clip);
    }

    public void PlayJump() => PlaySFX(jumpClip);
    public void PlayCoin() => PlaySFX(coinClip);
    public void PlayAttack() => PlaySFX(attackClip);
    public void PlayHitEnemy() => PlaySFX(hitEnemyClip);
    public void PlayHurt() => PlaySFX(hurtClip);
    public void PlayPortal() => PlaySFX(portalClip);

    // Reuses the existing death/game-over clip without stopping the level music.
    public void PlayDeath() => PlaySFX(gameOverClip);

    public void PlayGameOver()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        PlaySFX(gameOverClip);
    }

    public void PlayVictory()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        PlaySFX(victoryClip);
    }
}
