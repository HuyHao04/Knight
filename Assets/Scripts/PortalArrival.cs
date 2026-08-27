using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PortalArrival : MonoBehaviour
{
    [Header("Arrival Path")]
    [SerializeField] private Transform exitPoint;

    [Header("Sequence Timing")]
    [SerializeField, Min(0.05f)] private float playerFadeDuration = 0.25f;
    [SerializeField, Min(0.05f)] private float walkOutDuration = 0.8f;
    [SerializeField, Min(0.05f)] private float portalFadeDuration = 0.6f;

    [Header("Optional Effects")]
    [SerializeField] private ParticleSystem activationParticles;

    private bool isPrepared;
    private bool hasPlayed;
    private SpriteRenderer[] portalRenderers;
    private Color[] portalBaseColors;
    private Animator portalAnimator;

    public bool HasPlayed => hasPlayed;

    private void Awake()
    {
        portalRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        portalBaseColors = new Color[portalRenderers.Length];

        for (int i = 0; i < portalRenderers.Length; i++)
        {
            portalBaseColors[i] = portalRenderers[i].color;
        }

        portalAnimator = GetComponentInChildren<Animator>(true);
    }

    private IEnumerator Start()
    {
        // A persistent SceneTransitionManager owns arrivals after portal travel.
        // The delayed fallback covers direct scene loads, menu loads and restarts.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        player?.SetControlEnabled(false);

        yield return null;

        // A death respawn reloads the scene to restore coins and enemies, but it
        // must not replay the level entrance portal or move Player away from the
        // checkpoint restored by CheckpointManager.
        if (CheckpointManager.Instance.PlayerSpawnRestoredFromReload)
        {
            HidePortalImmediately();
            player?.SetControlEnabled(true);
            yield break;
        }

        if (hasPlayed || (SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.IsTransitioning))
        {
            yield break;
        }

        if (player != null)
        {
            yield return PlayArrival(player);
        }
    }

    public void Prepare(PlayerController player)
    {
        if (player == null || isPrepared || hasPlayed)
        {
            return;
        }

        isPrepared = true;
        player.SetControlEnabled(false);
        player.PrepareForPortalArrival(transform.position.x);
    }

    public IEnumerator PlayArrival(PlayerController player)
    {
        if (player == null || hasPlayed)
        {
            yield break;
        }

        Prepare(player);

        if (activationParticles != null)
        {
            activationParticles.Play();
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayPortal();
        }

        yield return player.FadeInFromPortal(playerFadeDuration);

        float destinationX = exitPoint != null
            ? exitPoint.position.x
            : transform.position.x + Mathf.Sign(player.transform.localScale.x) * 1.75f;

        yield return player.MoveFromPortal(destinationX, walkOutDuration);

        hasPlayed = true;
        player.SetControlEnabled(true);

        yield return FadePortalOut();
    }

    private IEnumerator FadePortalOut()
    {
        float safeDuration = Mathf.Max(0.05f, portalFadeDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float opacity = 1f - Mathf.Clamp01(elapsed / safeDuration);

            for (int i = 0; i < portalRenderers.Length; i++)
            {
                Color color = portalBaseColors[i];
                color.a *= opacity;
                portalRenderers[i].color = color;
            }

            yield return null;
        }

        for (int i = 0; i < portalRenderers.Length; i++)
        {
            portalRenderers[i].enabled = false;
        }

        if (portalAnimator != null)
        {
            portalAnimator.enabled = false;
        }
    }

    private void HidePortalImmediately()
    {
        for (int i = 0; i < portalRenderers.Length; i++)
        {
            portalRenderers[i].enabled = false;
        }

        if (portalAnimator != null)
        {
            portalAnimator.enabled = false;
        }
    }
}
