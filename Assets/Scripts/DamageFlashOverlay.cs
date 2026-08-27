using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DamageFlashOverlay : MonoBehaviour
{
    private const string OverlayName = "DamageFlashOverlay";
    private const int OverlaySortingOrder = 32700;

    [Header("Flash Timing")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.04f;
    [SerializeField, Min(0f)] private float holdDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.18f;

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.72f;
    [SerializeField, Min(1f)] private float borderThickness = 44f;
    [SerializeField] private Color borderColor = new Color(0.9f, 0.025f, 0.025f, 1f);

    private static DamageFlashOverlay instance;
    private CanvasGroup canvasGroup;
    private Coroutine flashRoutine;

    public CanvasGroup OverlayGroup => canvasGroup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        instance = Object.FindFirstObjectByType<DamageFlashOverlay>(FindObjectsInactive.Include);
        if (instance != null)
        {
            instance.EnsureVisuals();
            return;
        }

        GameObject overlay = new GameObject(
            OverlayName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup));
        instance = overlay.AddComponent<DamageFlashOverlay>();
        instance.EnsureVisuals();
    }

    public static void FlashDamage()
    {
        EnsureExists();
        instance.PlayFlash();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureVisuals();
    }

    private void EnsureVisuals()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = true;

        if (transform.childCount == 0)
        {
            CreateEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, borderThickness));
            CreateEdge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, borderThickness));
            CreateEdge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(borderThickness, 0f));
            CreateEdge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(borderThickness, 0f));
        }
    }

    private void CreateEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject edgeObject = new GameObject(edgeName, typeof(RectTransform), typeof(Image));
        edgeObject.transform.SetParent(transform, false);

        RectTransform rect = edgeObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = (anchorMin + anchorMax) * 0.5f;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        Image image = edgeObject.GetComponent<Image>();
        image.color = borderColor;
        image.raycastTarget = false;
    }

    private void PlayFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, peakAlpha, SafeProgress(elapsed, fadeInDuration));
            yield return null;
        }

        canvasGroup.alpha = peakAlpha;
        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(peakAlpha, 0f, SafeProgress(elapsed, fadeOutDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        flashRoutine = null;
    }

    private static float SafeProgress(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }
}
