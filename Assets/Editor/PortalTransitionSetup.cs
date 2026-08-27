using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PortalTransitionSetup
{
    private const string MenuPath = "Tools/Portal/Configure Portal Transition";
    private const string ValidateMenuPath = "Tools/Portal/Validate Portal Transition";
    private const string PortalPrefabPath = "Assets/Prelabs/Portal.prefab";

    private readonly struct SceneDestination
    {
        public readonly string ScenePath;
        public readonly string Destination;
        public readonly float ArrivalDirection;
        public readonly bool HasEntrancePortal;

        public SceneDestination(
            string scenePath,
            string destination,
            float arrivalDirection,
            bool hasEntrancePortal)
        {
            ScenePath = scenePath;
            Destination = destination;
            ArrivalDirection = Mathf.Sign(arrivalDirection);
            HasEntrancePortal = hasEntrancePortal;
        }
    }

    private static readonly SceneDestination[] GameplayScenes =
    {
        new SceneDestination("Assets/Scenes/Level_1.unity", "Level_2", 0f, false),
        new SceneDestination("Assets/Scenes/Level_2.unity", "Level_3", 1f, true),
        new SceneDestination("Assets/Scenes/Level_3.unity", "Boss", 1f, true),
        new SceneDestination("Assets/Scenes/Boss.unity", string.Empty, 1f, true)
    };

    [MenuItem(MenuPath)]
    public static void ConfigurePortalTransition()
    {
        ConfigurePortalPrefab();

        foreach (SceneDestination entry in GameplayScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
            ConfigureScene(
                scene,
                entry.Destination,
                entry.ArrivalDirection,
                entry.HasEntrancePortal);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Portal entry, arrival sequence and persistent screen fade configured successfully.");
    }

    [MenuItem(ValidateMenuPath)]
    public static void ValidatePortalTransition()
    {
        foreach (SceneDestination entry in GameplayScenes)
        {
            EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);

            PortalArrival[] arrivals = Object.FindObjectsByType<PortalArrival>(
                FindObjectsInactive.Include);

            if (!entry.HasEntrancePortal)
            {
                Require(arrivals.Length == 0,
                    entry.ScenePath + " must not contain an EntrancePortal.");
                continue;
            }

            Require(arrivals.Length == 1,
                entry.ScenePath + " must contain exactly one EntrancePortal.");

            PortalArrival arrival = arrivals[0];
            Require(arrival.gameObject.name == "EntrancePortal",
                entry.ScenePath + " arrival object must be named EntrancePortal.");

            Portal interactivePortal = arrival.GetComponent<Portal>();
            Require(interactivePortal == null || !interactivePortal.enabled,
                entry.ScenePath + " EntrancePortal must not accept E interaction.");

            Collider2D portalCollider = arrival.GetComponent<Collider2D>();
            Require(portalCollider == null || !portalCollider.enabled,
                entry.ScenePath + " EntrancePortal collider must be disabled.");

            SerializedObject serializedArrival = new SerializedObject(arrival);
            Transform exitPoint = serializedArrival.FindProperty("exitPoint").objectReferenceValue
                as Transform;
            Require(exitPoint != null,
                entry.ScenePath + " EntrancePortal requires an ExitPoint.");

            float actualDirection = Mathf.Sign(
                exitPoint.position.x - arrival.transform.position.x);
            Require(Mathf.Approximately(actualDirection, entry.ArrivalDirection),
                entry.ScenePath + " EntrancePortal faces the wrong travel direction.");
            Require(Mathf.Abs(exitPoint.position.x - arrival.transform.position.x) >= 1.5f,
                entry.ScenePath + " EntrancePortal walk-out distance is too short.");

            Require(Object.FindAnyObjectByType<SceneTransitionManager>(
                FindObjectsInactive.Include) != null,
                entry.ScenePath + " requires a SceneTransitionManager.");
        }

        Debug.Log("PORTAL ARRIVAL VALIDATION PASSED: Level_1 has no entrance; Level_2, Level_3 and Boss do.");
    }

    internal static void ConfigurePortalPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PortalPrefabPath);

        Collider2D portalCollider = root.GetComponent<Collider2D>();
        if (portalCollider == null)
        {
            portalCollider = root.AddComponent<BoxCollider2D>();
        }

        portalCollider.isTrigger = true;
        root.tag = "gate";

        Portal portal = root.GetComponent<Portal>();
        if (portal == null)
        {
            portal = root.AddComponent<Portal>();
        }

        SerializedObject serializedPortal = new SerializedObject(portal);
        serializedPortal.FindProperty("moveToCenterDuration").floatValue = 0.4f;
        serializedPortal.FindProperty("playerFadeDuration").floatValue = 0.3f;
        serializedPortal.FindProperty("pulseDuration").floatValue = 0.35f;
        serializedPortal.FindProperty("pulseScale").floatValue = 1.08f;
        serializedPortal.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    internal static void ConfigureScene(
        Scene scene,
        string destination,
        float arrivalDirection,
        bool hasEntrancePortal)
    {
        ConfigureTransitionManager();

        if (hasEntrancePortal)
        {
            ConfigureArrivalPortal(scene, arrivalDirection);
        }
        else
        {
            RemoveArrivalPortals();
        }

        Portal[] portals = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include)
            .Where(portal => portal.enabled && portal.GetComponent<PortalArrival>() == null)
            .ToArray();

        if (portals.Length == 0)
        {
            PortalPromptUI unusedPrompt = Object.FindAnyObjectByType<PortalPromptUI>(
                FindObjectsInactive.Include);
            if (unusedPrompt != null)
            {
                Object.DestroyImmediate(unusedPrompt.gameObject);
            }

            return;
        }

        Canvas gameplayCanvas = FindGameplayCanvas();
        if (gameplayCanvas == null)
        {
            Debug.LogError("Portal setup could not find a screen-space Canvas in " + scene.path);
            return;
        }

        PortalPromptUI prompt = ConfigurePrompt(gameplayCanvas);

        foreach (Portal portal in portals)
        {
            SerializedObject serializedPortal = new SerializedObject(portal);
            serializedPortal.FindProperty("destinationScene").stringValue = destination;
            serializedPortal.FindProperty("promptUI").objectReferenceValue = prompt;
            serializedPortal.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(portal);
        }
    }

    private static void RemoveArrivalPortals()
    {
        PortalArrival[] arrivals = Object.FindObjectsByType<PortalArrival>(
            FindObjectsInactive.Include);

        foreach (PortalArrival arrival in arrivals)
        {
            Object.DestroyImmediate(arrival.gameObject);
        }
    }

    private static void ConfigureArrivalPortal(Scene scene, float direction)
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>(
            FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("Portal setup could not find the player in " + scene.path);
            return;
        }

        PortalArrival arrival = Object.FindAnyObjectByType<PortalArrival>(
            FindObjectsInactive.Include);

        if (arrival == null)
        {
            GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (portalPrefab == null)
            {
                Debug.LogError("Portal prefab is missing at " + PortalPrefabPath);
                return;
            }

            GameObject entranceObject = (GameObject)PrefabUtility.InstantiatePrefab(
                portalPrefab,
                scene);
            entranceObject.name = "EntrancePortal";
            entranceObject.tag = "Untagged";
            arrival = entranceObject.AddComponent<PortalArrival>();
        }

        GameObject entrance = arrival.gameObject;
        Portal interactivePortal = entrance.GetComponent<Portal>();
        if (interactivePortal != null)
        {
            interactivePortal.enabled = false;
        }

        Collider2D portalCollider = entrance.GetComponent<Collider2D>();
        if (portalCollider != null)
        {
            portalCollider.enabled = false;
        }

        float safeDirection = Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction);
        Vector3 spawnPosition = player.transform.position;
        entrance.transform.position = new Vector3(
            spawnPosition.x - safeDirection * 1.75f,
            spawnPosition.y + 0.9f,
            0f);

        Transform exitPoint = entrance.transform.Find("ExitPoint");
        if (exitPoint == null)
        {
            GameObject exitPointObject = new GameObject("ExitPoint");
            exitPoint = exitPointObject.transform;
            exitPoint.SetParent(entrance.transform, true);
        }

        exitPoint.position = spawnPosition;

        SerializedObject serializedArrival = new SerializedObject(arrival);
        serializedArrival.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        serializedArrival.FindProperty("playerFadeDuration").floatValue = 0.25f;
        serializedArrival.FindProperty("walkOutDuration").floatValue = 0.8f;
        serializedArrival.FindProperty("portalFadeDuration").floatValue = 0.6f;
        serializedArrival.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(arrival);
        EditorUtility.SetDirty(entrance);
    }

    private static Canvas FindGameplayCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);

        return canvases.FirstOrDefault(canvas =>
            canvas.renderMode != RenderMode.WorldSpace
            && canvas.GetComponentsInChildren<PlayerHealthUI>(true).Length > 0)
            ?? canvases.FirstOrDefault(canvas => canvas.renderMode != RenderMode.WorldSpace);
    }

    private static PortalPromptUI ConfigurePrompt(Canvas canvas)
    {
        PortalPromptUI prompt = Object.FindAnyObjectByType<PortalPromptUI>(FindObjectsInactive.Include);
        if (prompt == null)
        {
            GameObject promptObject = new GameObject(
                "PortalPrompt",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(PortalPromptUI));
            promptObject.layer = 5;
            promptObject.transform.SetParent(canvas.transform, false);
            prompt = promptObject.GetComponent<PortalPromptUI>();
        }

        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 72f);
        promptRect.sizeDelta = new Vector2(320f, 56f);

        CanvasGroup promptGroup = prompt.GetComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;

        TextMeshProUGUI promptText = prompt.GetComponentInChildren<TextMeshProUGUI>(true);
        if (promptText == null)
        {
            TextMeshProUGUI template = canvas.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "Score")
                ?? canvas.GetComponentInChildren<TextMeshProUGUI>(true);

            if (template != null)
            {
                GameObject textObject = Object.Instantiate(template.gameObject, prompt.transform);
                textObject.name = "PortalPromptText";
                promptText = textObject.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject textObject = new GameObject(
                    "PortalPromptText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                textObject.layer = 5;
                textObject.transform.SetParent(prompt.transform, false);
                promptText = textObject.GetComponent<TextMeshProUGUI>();
            }
        }

        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        promptText.text = "[E] Enter";
        promptText.fontSize = 30f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        SerializedObject serializedPrompt = new SerializedObject(prompt);
        serializedPrompt.FindProperty("promptGroup").objectReferenceValue = promptGroup;
        serializedPrompt.FindProperty("promptText").objectReferenceValue = promptText;
        serializedPrompt.FindProperty("message").stringValue = "[E] Enter";
        serializedPrompt.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prompt);
        return prompt;
    }

    private static void ConfigureTransitionManager()
    {
        SceneTransitionManager manager = Object.FindAnyObjectByType<SceneTransitionManager>(
            FindObjectsInactive.Include);

        if (manager == null)
        {
            GameObject managerObject = new GameObject("SceneTransitionManager");
            manager = managerObject.AddComponent<SceneTransitionManager>();
        }

        CanvasGroup fadeGroup = manager.GetComponentInChildren<CanvasGroup>(true);
        if (fadeGroup == null)
        {
            GameObject canvasObject = new GameObject(
                "TransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            canvasObject.transform.SetParent(manager.transform, false);

            Canvas transitionCanvas = canvasObject.GetComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 32760;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject fadeObject = new GameObject(
                "FadeImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            fadeObject.layer = 5;
            fadeObject.transform.SetParent(canvasObject.transform, false);

            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.pivot = new Vector2(0.5f, 0.5f);
            fadeRect.anchoredPosition = Vector2.zero;
            fadeRect.sizeDelta = Vector2.zero;

            Image fadeImage = fadeObject.GetComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;

            fadeGroup = fadeObject.GetComponent<CanvasGroup>();
        }

        fadeGroup.alpha = 0f;
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = false;

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("fadeGroup").objectReferenceValue = fadeGroup;
        serializedManager.FindProperty("fadeDuration").floatValue = 0.5f;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
