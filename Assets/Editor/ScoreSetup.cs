using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the reusable score rewards and adds the Total Score victory label to every
/// gameplay scene. Run from the menu whenever a new gameplay scene is introduced.
/// </summary>
public static class ScoreSetup
{
    private const string MenuPath = "Tools/Score/Configure Score System";

    [MenuItem(MenuPath)]
    public static void ConfigureScoreSystem()
    {
        ConfigurePrefab("Assets/Prelabs/Slime.prefab", 200);
        ConfigurePrefab("Assets/Prelabs/Bat.prefab", 200);
        ConfigurePrefab("Assets/Prelabs/SkeletonArcher.prefab", 300);
        ConfigurePrefab("Assets/Prelabs/Skeleton_Warrior.prefab", 300);
        ConfigurePrefab("Assets/Prelabs/Skeleton_Spearman.prefab", 300);

        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly);
        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ConfigureScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Score system configured for prefabs and gameplay scenes.");
    }

    private static void ConfigurePrefab(string prefabPath, int scoreValue)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        ConfigureReward(root, scoreValue);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureScene(Scene scene)
    {
        NecromancerBoss boss = Object.FindFirstObjectByType<NecromancerBoss>(FindObjectsInactive.Include);
        if (boss != null)
        {
            ConfigureReward(boss.gameObject, 5000);
        }

        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            ConfigureVictoryUi(player);
        }
    }

    private static void ConfigureReward(GameObject target, int scoreValue)
    {
        ScoreReward reward = target.GetComponent<ScoreReward>();
        if (reward == null)
        {
            reward = target.AddComponent<ScoreReward>();
        }

        SerializedObject serializedReward = new SerializedObject(reward);
        serializedReward.FindProperty("scoreValue").intValue = scoreValue;
        serializedReward.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureVictoryUi(PlayerController player)
    {
        SerializedObject serializedPlayer = new SerializedObject(player);
        TextMeshProUGUI victoryHP = serializedPlayer.FindProperty("victoryHP").objectReferenceValue as TextMeshProUGUI;
        TextMeshProUGUI victoryCoins = serializedPlayer.FindProperty("victoryScore").objectReferenceValue as TextMeshProUGUI;
        TextMeshProUGUI victoryKills = serializedPlayer.FindProperty("victoryKill").objectReferenceValue as TextMeshProUGUI;

        if (victoryHP == null || victoryCoins == null || victoryKills == null)
        {
            Debug.LogWarning("Score setup skipped a PlayerController with incomplete Victory UI.", player);
            return;
        }

        Transform victoryPanel = victoryCoins.transform.parent;
        TextMeshProUGUI total = victoryPanel.Find("VictoryTotalScore")?.GetComponent<TextMeshProUGUI>();
        if (total == null)
        {
            GameObject totalObject = Object.Instantiate(victoryCoins.gameObject, victoryPanel);
            totalObject.name = "VictoryTotalScore";
            total = totalObject.GetComponent<TextMeshProUGUI>();
        }

        SetVerticalPosition(victoryHP.rectTransform, -78f);
        SetVerticalPosition(victoryCoins.rectTransform, -118f);
        SetVerticalPosition(victoryKills.rectTransform, -158f);
        SetVerticalPosition(total.rectTransform, -198f);
        total.text = "Total Score: 000000";

        foreach (RectTransform child in victoryPanel.GetComponentsInChildren<RectTransform>(true))
        {
            if (child.parent != victoryPanel)
            {
                continue;
            }

            if (child.name.Contains("Back") || child.name.Contains("Again"))
            {
                SetVerticalPosition(child, -252f);
            }
            else if (child.name.Contains("Next"))
            {
                SetVerticalPosition(child, -302f);
            }
        }

        serializedPlayer.FindProperty("victoryTotalScore").objectReferenceValue = total;
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player);
    }

    private static void SetVerticalPosition(RectTransform rectTransform, float y)
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.y = y;
        rectTransform.anchoredPosition = position;
    }
}
