using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossIntroSetup
{
    private const string MenuPath = "Tools/Boss/Configure Malakor Intro";
    private const string BossScenePath = "Assets/Scenes/Boss.unity";

    [MenuItem(MenuPath)]
    public static void ConfigureMalakorIntro()
    {
        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);

        NecromancerBoss boss = Object.FindFirstObjectByType<NecromancerBoss>(FindObjectsInactive.Include);
        DialogueManager dialogueManager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (boss == null || dialogueManager == null || player == null)
        {
            Debug.LogError("Malakor intro setup requires NecromancerBoss, DialogueManager and PlayerController in Boss scene.");
            return;
        }

        BossIntroTrigger trigger = Object.FindFirstObjectByType<BossIntroTrigger>(FindObjectsInactive.Include);
        if (trigger == null)
        {
            GameObject triggerObject = new GameObject("BossIntroTrigger");
            BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            trigger = triggerObject.AddComponent<BossIntroTrigger>();
        }

        float approachDirection = Mathf.Sign(player.transform.position.x - boss.transform.position.x);
        if (Mathf.Approximately(approachDirection, 0f))
        {
            approachDirection = -1f;
        }

        trigger.transform.position = new Vector3(
            boss.transform.position.x + approachDirection * 5f,
            player.transform.position.y + 2.5f,
            0f);

        BoxCollider2D triggerCollider = trigger.GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.offset = Vector2.zero;
        triggerCollider.size = new Vector2(3f, 6f);

        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("dialogueManager").objectReferenceValue = dialogueManager;
        serializedTrigger.FindProperty("necromancerBoss").objectReferenceValue = boss;
        serializedTrigger.FindProperty("combatStartDelay").floatValue = 0.35f;
        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedBoss = new SerializedObject(boss);
        BossHealthBar healthBar = serializedBoss.FindProperty("bossHealthBar")
            .objectReferenceValue as BossHealthBar;
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            EditorUtility.SetDirty(healthBar.gameObject);
        }

        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(triggerCollider);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Malakor intro configured. Player=" + player.transform.position
            + ", Boss=" + boss.transform.position
            + ", Trigger=" + trigger.transform.position);
    }
}
