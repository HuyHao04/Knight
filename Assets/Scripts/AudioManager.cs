using UnityEngine;

public class AudioManager : MonoBehaviour
{
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

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        musicSource.clip = bgmClip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayJump()
    {
        PlaySFX(jumpClip);
    }

    public void PlayCoin()
    {
        PlaySFX(coinClip);
    }

    public void PlayAttack()
    {
        PlaySFX(attackClip);
    }

    public void PlayHitEnemy()
    {
        PlaySFX(hitEnemyClip);
    }

    public void PlayHurt()
    {
        PlaySFX(hurtClip);
    }

    public void PlayGameOver()
    {
        musicSource.Stop();
        sfxSource.PlayOneShot(gameOverClip);
    }

    public void PlayVictory()
    {
        musicSource.Stop();
        sfxSource.PlayOneShot(victoryClip);
    }
    public void SetMusicVolume(float volume)
{
    musicSource.volume = volume;
}

public void SetSFXVolume(float volume)
{
    sfxSource.volume = volume;
}
}