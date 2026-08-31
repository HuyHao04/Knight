using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationScene;

    [Header("Sequence Timing")]
    [SerializeField, Min(0.05f)] private float moveToCenterDuration = 0.4f;
    [SerializeField, Min(0.05f)] private float playerFadeDuration = 0.3f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.35f;
    [SerializeField, Range(1f, 1.2f)] private float pulseScale = 1.08f;

    [Header("Optional Effects")]
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private PortalPromptUI promptUI;

    private PlayerController playerInRange;
    private Vector3 idleScale;
    private int playerOverlapCount;
    private bool isTransitioning;

    private void Awake()
    {
        idleScale = transform.localScale;

        Collider2D portalCollider = GetComponent<Collider2D>();
        portalCollider.isTrigger = true;

        if (promptUI == null)
        {
            promptUI = FindFirstObjectByType<PortalPromptUI>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
        if (!isTransitioning && playerInRange != null && InteractionInput.TryConsumeInteract())
        {
            StartCoroutine(EnterPortalRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || isTransitioning)
        {
            return;
        }

        if (playerInRange == null)
        {
            playerInRange = player;
        }

        if (player != playerInRange)
        {
            return;
        }

        playerOverlapCount++;
        promptUI?.Show(this, "[E] Enter");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != playerInRange || isTransitioning)
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount == 0)
        {
            promptUI?.Hide(this);
            playerInRange = null;
        }
    }

    private IEnumerator EnterPortalRoutine()
    {
        if (isTransitioning || playerInRange == null || !CanLoadDestination())
        {
            yield break;
        }

        isTransitioning = true;
        promptUI?.Hide(this);
        playerInRange.SetControlEnabled(false);

        if (activationParticles != null)
        {
            activationParticles.Play();
        }

        StartCoroutine(PulseRoutine());
        yield return playerInRange.MoveToPortalCenter(transform.position.x, moveToCenterDuration);
        yield return playerInRange.FadeOutForPortal(playerFadeDuration);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayPortal();
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager == null)
        {
            Debug.LogError("Portal requires a SceneTransitionManager in the scene.", this);
            isTransitioning = false;
            playerInRange.SetControlEnabled(true);
            yield break;
        }

        bool transitionStarted = string.IsNullOrWhiteSpace(destinationScene)
            ? transitionManager.TransitionToScene(SceneManager.GetActiveScene().buildIndex + 1)
            : transitionManager.TransitionToScene(destinationScene);

        if (!transitionStarted)
        {
            Debug.LogError("Portal could not start transition to '" + destinationScene + "'.", this);
            isTransitioning = false;
            playerInRange.SetControlEnabled(true);
            yield break;
        }

        // Only completed levels enter the campaign total. A death/restart reloads
        // the scene without committing, preventing recollected items from counting twice.
        ScoreManager.Instance.CommitCurrentLevelToRun();
    }

    private bool CanLoadDestination()
    {
        if (!string.IsNullOrWhiteSpace(destinationScene))
        {
            if (Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                return true;
            }

            Debug.LogError("Portal destination scene is not enabled in Build Settings: " + destinationScene, this);
            return false;
        }

        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextBuildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            return true;
        }

        Debug.LogError("Portal has no valid destination scene.", this);
        return false;
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, pulseDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(idleScale, idleScale * pulseScale, pulse);
            yield return null;
        }

        transform.localScale = idleScale;
    }
}
