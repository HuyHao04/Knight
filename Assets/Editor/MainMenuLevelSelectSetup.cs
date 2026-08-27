using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuLevelSelectSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string IconSheetPath = "Assets/Sprites/UI/Green Buttons Icons.png";

    private static readonly Vector2[] ButtonPositions =
    {
        new Vector2(-429f, 130f),
        new Vector2(-297f, 130f),
        new Vector2(-164f, 130f),
        new Vector2(-31f, 130f)
    };

    [MenuItem("Tools/Main Menu/Configure Four Level Buttons")]
    public static void ConfigureFourLevelButtons()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ButtonManager manager = UnityEngine.Object.FindFirstObjectByType<ButtonManager>(
            FindObjectsInactive.Include);
        Require(manager != null, "MainMenu is missing ButtonManager.");

        GameObject levelPanel = FindSceneObject(scene, "LevelPanel");
        Require(levelPanel != null, "MainMenu is missing LevelPanel.");

        Button level1 = FindDirectButton(levelPanel.transform, "Level1Button");
        Button level2 = FindDirectButton(levelPanel.transform, "Level2Button");
        Button level3 = FindDirectButton(levelPanel.transform, "Level3Button");
        Require(level1 != null && level2 != null && level3 != null,
            "LevelPanel must contain Level1Button, Level2Button and Level3Button.");

        Button boss = FindDirectButton(levelPanel.transform, "BossButton");
        if (boss == null)
        {
            GameObject bossObject = UnityEngine.Object.Instantiate(level3.gameObject);
            bossObject.name = "BossButton";
            bossObject.transform.SetParent(levelPanel.transform, false);
            boss = bossObject.GetComponent<Button>();
        }

        ConfigureButton(level1, ButtonPositions[0], LoadSprite("Green Buttons Icons_25"));
        ConfigureButton(level2, ButtonPositions[1], LoadSprite("Green Buttons Icons_54"));
        ConfigureButton(level3, ButtonPositions[2], LoadSprite("Green Buttons Icons_55"));
        ConfigureButton(boss, ButtonPositions[3], LoadSprite("Green Buttons Icons_84"));

        ReplaceClick(level1, manager.LoadLevel1);
        ReplaceClick(level2, manager.LoadLevel2);
        ReplaceClick(level3, manager.LoadLevel3);
        ReplaceClick(boss, manager.LoadBoss);

        level1.transform.SetSiblingIndex(0);
        level2.transform.SetSiblingIndex(1);
        level3.transform.SetSiblingIndex(2);
        boss.transform.SetSiblingIndex(3);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ValidateSavedMenu();

        Debug.Log(
            "MAIN MENU LEVEL SELECT READY: Level 1, Level 2, Level 3 and Boss buttons configured.");
    }

    public static void ConfigureAndValidate()
    {
        ConfigureFourLevelButtons();
    }

    private static void ConfigureButton(Button button, Vector2 position, Sprite sprite)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        Image image = button.GetComponent<Image>();
        Require(rect != null && image != null, button.name + " is missing RectTransform or Image.");

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(60f, 60f);
        image.sprite = sprite;
        image.preserveAspect = false;

        EditorUtility.SetDirty(rect);
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(button);
    }

    private static void ReplaceClick(Button button, UnityEngine.Events.UnityAction action)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static Sprite LoadSprite(string spriteName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(IconSheetPath)
            .OfType<Sprite>()
            .FirstOrDefault(candidate => candidate.name == spriteName);
        Require(sprite != null, "Missing UI sprite: " + spriteName);
        return sprite;
    }

    private static Button FindDirectButton(Transform parent, string objectName)
    {
        Transform child = parent.Cast<Transform>()
            .FirstOrDefault(candidate => candidate.name == objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(candidate => candidate.name == objectName)
            ?.gameObject;
    }

    private static void ValidateSavedMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject levelPanel = FindSceneObject(scene, "LevelPanel");
        Require(levelPanel != null, "Saved MainMenu is missing LevelPanel.");

        string[] names = { "Level1Button", "Level2Button", "Level3Button", "BossButton" };
        string[] methods = { "LoadLevel1", "LoadLevel2", "LoadLevel3", "LoadBoss" };
        string[] sprites =
        {
            "Green Buttons Icons_25",
            "Green Buttons Icons_54",
            "Green Buttons Icons_55",
            "Green Buttons Icons_84"
        };

        for (int i = 0; i < names.Length; i++)
        {
            Button button = FindDirectButton(levelPanel.transform, names[i]);
            Require(button != null, "Saved menu is missing " + names[i] + ".");
            Require(button.onClick.GetPersistentEventCount() == 1,
                names[i] + " must have exactly one click action.");
            Require(button.onClick.GetPersistentMethodName(0) == methods[i],
                names[i] + " is not connected to " + methods[i] + ".");
            Require(button.GetComponent<Image>().sprite.name == sprites[i],
                names[i] + " is using the wrong sprite.");
            Require(button.GetComponent<RectTransform>().anchoredPosition == ButtonPositions[i],
                names[i] + " has the wrong position.");
        }

        Require(levelPanel.transform.Cast<Transform>()
                .Count(child => names.Contains(child.name)) == 4,
            "LevelPanel must contain exactly four managed level buttons.");

        Debug.Log("MAIN MENU LEVEL SELECT VALIDATION PASSED.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
