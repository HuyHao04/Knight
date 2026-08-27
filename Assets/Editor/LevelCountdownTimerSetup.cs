using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelCountdownTimerSetup
{
    private const string MenuPath = "Tools/Gameplay/Configure 300 Second Timers";
    private const float DurationSeconds = 300f;

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level_1.unity",
        "Assets/Scenes/Level_2.unity",
        "Assets/Scenes/Level_3.unity",
        "Assets/Scenes/Boss.unity"
    };

    [MenuItem(MenuPath)]
    public static void ConfigureTimers()
    {
        foreach (string scenePath in ScenePaths)
        {
            ConfigureScene(scenePath);
        }

        AssetDatabase.SaveAssets();
        ValidateTimers();
        Debug.Log("LEVEL TIMER SETUP COMPLETE: Level_1, Level_2, Level_3 and Boss use 300 seconds.");
    }

    public static void ValidateTimers()
    {
        foreach (string scenePath in ScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(
                FindObjectsInactive.Include);
            PauseMenu pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenu>(
                FindObjectsInactive.Include);
            LevelCountdownTimer[] timers = UnityEngine.Object.FindObjectsByType<LevelCountdownTimer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Require(player != null, scenePath + " is missing PlayerController.");
            Require(pauseMenu != null, scenePath + " is missing PauseMenu.");
            Require(timers.Length == 1, scenePath + " must contain exactly one level timer.");

            SerializedObject playerData = new SerializedObject(player);
            SerializedObject timerData = new SerializedObject(timers[0]);
            Require(
                playerData.FindProperty("GameOverPanel").objectReferenceValue != null,
                scenePath + " PlayerController is missing GameOverPanel.");
            Require(
                Mathf.Approximately(
                    timerData.FindProperty("durationSeconds").floatValue,
                    DurationSeconds),
                scenePath + " timer duration must be 300 seconds.");
            Require(
                timerData.FindProperty("player").objectReferenceValue == player,
                scenePath + " timer is not linked to PlayerController.");
            Require(
                timerData.FindProperty("timerText").objectReferenceValue != null,
                scenePath + " timer text is not assigned.");
            Require(
                timers[0].GetComponentInParent<Canvas>() != null,
                scenePath + " timer is not under a Canvas.");
        }

        Debug.Log("LEVEL TIMER VALIDATION PASSED: all gameplay scenes have one 05:00 timer.");
    }

    internal static void ConfigureScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(
            FindObjectsInactive.Include);

        if (player == null)
        {
            throw new InvalidOperationException(scenePath + " is missing PlayerController.");
        }

        Canvas hudCanvas = FindHudCanvas(player);
        if (hudCanvas == null)
        {
            throw new InvalidOperationException(scenePath + " is missing a HUD Canvas.");
        }

        LevelCountdownTimer timer = UnityEngine.Object.FindAnyObjectByType<LevelCountdownTimer>(
            FindObjectsInactive.Include);
        TextMeshProUGUI timerText;

        if (timer == null)
        {
            GameObject timerObject = new GameObject(
                "LevelTimer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(LevelCountdownTimer));
            timerObject.layer = 5;
            timerObject.transform.SetParent(hudCanvas.transform, false);
            timer = timerObject.GetComponent<LevelCountdownTimer>();
            timerText = timerObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            timer.transform.SetParent(hudCanvas.transform, false);
            timerText = timer.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = timer.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(240f, 58f);
        rect.localScale = Vector3.one;

        TextMeshProUGUI scoreText = GetPlayerScoreText(player);
        if (scoreText != null && scoreText.font != null)
        {
            timerText.font = scoreText.font;
        }

        timerText.text = "05:00";
        timerText.fontSize = 36f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.raycastTarget = false;
        timerText.enableWordWrapping = false;

        SerializedObject timerData = new SerializedObject(timer);
        timerData.FindProperty("durationSeconds").floatValue = DurationSeconds;
        timerData.FindProperty("player").objectReferenceValue = player;
        timerData.FindProperty("timerText").objectReferenceValue = timerText;
        timerData.FindProperty("warningThreshold").floatValue = 60f;
        timerData.FindProperty("criticalThreshold").floatValue = 10f;
        timerData.ApplyModifiedPropertiesWithoutUndo();

        timer.transform.SetAsLastSibling();
        EditorUtility.SetDirty(timer);
        EditorUtility.SetDirty(timerText);
        EditorSceneManager.SaveScene(scene);
    }

    private static Canvas FindHudCanvas(PlayerController player)
    {
        TextMeshProUGUI scoreText = GetPlayerScoreText(player);
        if (scoreText != null)
        {
            Canvas scoreCanvas = scoreText.GetComponentInParent<Canvas>();
            if (scoreCanvas != null)
            {
                return scoreCanvas;
            }
        }

        return UnityEngine.Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
    }

    private static TextMeshProUGUI GetPlayerScoreText(PlayerController player)
    {
        SerializedObject playerData = new SerializedObject(player);
        return playerData.FindProperty("score").objectReferenceValue as TextMeshProUGUI;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
