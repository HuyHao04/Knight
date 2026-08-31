using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1TutorialSetup
{
    private const string ScenePath = "Assets/Scenes/Level_1.unity";
    private const string CoinPrefabPath = "Assets/Prelabs/Coin.prefab";
    private const string SlimePrefabPath = "Assets/Prelabs/Slime.prefab";

    [MenuItem("Tools/Level 1/Setup Tutorial")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject tutorialRoot = GameObject.Find("Level1Tutorial");
        if (tutorialRoot == null)
        {
            tutorialRoot = new GameObject("Level1Tutorial");
        }

        LevelOneTutorial tutorial = tutorialRoot.GetComponent<LevelOneTutorial>();
        if (tutorial == null)
        {
            tutorial = tutorialRoot.AddComponent<LevelOneTutorial>();
        }

        Transform content = tutorialRoot.transform.Find("TutorialContent");
        if (content == null)
        {
            GameObject contentObject = new GameObject("TutorialContent");
            contentObject.transform.SetParent(tutorialRoot.transform, false);
            content = contentObject.transform;
        }

        ClearChildren(content);

        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        CreatePrefabInstance(coinPrefab, content, "TutorialCoin_1", new Vector3(-68.2f, -3.35f, -3f));
        CreatePrefabInstance(coinPrefab, content, "TutorialCoin_2", new Vector3(-66.9f, -2.55f, -3f));
        CreatePrefabInstance(coinPrefab, content, "TutorialCoin_3", new Vector3(-65.6f, -3.35f, -3f));

        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath);
        GameObject tutorialSlime = CreatePrefabInstance(
            slimePrefab,
            content,
            "TutorialSlime",
            new Vector3(-54.5f, -3.59f, -3f));

        if (tutorialSlime != null
            && tutorialSlime.TryGetComponent(out EnemyController slimeController))
        {
            slimeController.moveSpeed = 1.4f;
            slimeController.patrolDistance = 2.5f;
            slimeController.detectRange = 4f;
        }

        SerializedObject tutorialSerialized = new SerializedObject(tutorial);
        tutorialSerialized.FindProperty("tutorialEnemy").objectReferenceValue =
            tutorialSlime != null ? tutorialSlime.transform : null;
        tutorialSerialized.ApplyModifiedPropertiesWithoutUndo();

        NPCDialogue[] npcDialogues = Object.FindObjectsByType<NPCDialogue>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (NPCDialogue npcDialogue in npcDialogues)
        {
            SerializedObject npcSerialized = new SerializedObject(npcDialogue);
            npcSerialized.FindProperty("requireInteractKey").boolValue = true;
            npcSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL 1 TUTORIAL SETUP COMPLETE.");
    }

    private static GameObject CreatePrefabInstance(
        GameObject prefab,
        Transform parent,
        string objectName,
        Vector3 worldPosition)
    {
        if (prefab == null)
        {
            Debug.LogError("Tutorial prefab is missing: " + objectName);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        instance.name = objectName;
        instance.transform.position = worldPosition;
        return instance;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
