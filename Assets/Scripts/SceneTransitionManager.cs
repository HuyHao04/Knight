using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.5f;

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetFadeImmediately(0f, false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TransitionToScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrWhiteSpace(sceneName)
            || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return false;
        }

        StartCoroutine(TransitionRoutine(() => SceneManager.LoadSceneAsync(sceneName)));
        return true;
    }

    public bool TransitionToScene(int buildIndex)
    {
        if (isTransitioning || buildIndex < 0
            || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return false;
        }

        StartCoroutine(TransitionRoutine(() => SceneManager.LoadSceneAsync(buildIndex)));
        return true;
    }

    private IEnumerator TransitionRoutine(System.Func<AsyncOperation> loadScene)
    {
        isTransitioning = true;

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        AsyncOperation loadOperation = loadScene();
        if (loadOperation == null)
        {
            SetFadeImmediately(0f, false);
            isTransitioning = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        PlayerController destinationPlayer = FindFirstObjectByType<PlayerController>();
        if (destinationPlayer != null)
        {
            destinationPlayer.SetControlEnabled(false);
        }

        PortalArrival arrival = FindFirstObjectByType<PortalArrival>(FindObjectsInactive.Include);

        // Let PlayerController.Start register the intended checkpoint before the
        // arrival sequence temporarily moves the player into the portal.
        yield return null;

        if (destinationPlayer != null && arrival != null)
        {
            arrival.Prepare(destinationPlayer);
        }

        if (fadeGroup != null)
        {
            yield return Fade(1f, 0f);
            fadeGroup.blocksRaycasts = false;
        }

        if (destinationPlayer != null && arrival != null)
        {
            yield return arrival.PlayArrival(destinationPlayer);
        }
        else if (destinationPlayer != null)
        {
            destinationPlayer.SetControlEnabled(true);
        }

        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        fadeGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        fadeGroup.alpha = to;
    }

    private void SetFadeImmediately(float alpha, bool blockRaycasts)
    {
        if (fadeGroup == null)
        {
            return;
        }

        fadeGroup.alpha = alpha;
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = blockRaycasts;
    }
}
