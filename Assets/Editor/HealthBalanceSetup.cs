using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HealthBalanceSetup
{
    private const string MenuPath = "Tools/Health/Configure Health and Damage";
    private const string HeartSpritePath = "Assets/Sprites/heart pixel art 32x32.png";

    private static readonly string[] GameplayScenes =
    {
        "Assets/Scenes/Level_1.unity",
        "Assets/Scenes/Level_2.unity",
        "Assets/Scenes/Boss.unity"
    };

    [MenuItem(MenuPath)]
    public static void ConfigureHealthAndDamage()
    {
        Sprite heartSprite = AssetDatabase.LoadAllAssetsAtPath(HeartSpritePath)
            .OfType<Sprite>()
            .FirstOrDefault();

        if (heartSprite == null)
        {
            Debug.LogError("Health setup could not load heart sprite at " + HeartSpritePath);
            return;
        }

        ConfigureContactDamagePrefab("Assets/Prelabs/Slime.prefab", 1);
        ConfigureContactDamagePrefab("Assets/Prelabs/Skeleton_Warrior.prefab", 2);
        ConfigureContactDamagePrefab("Assets/Prelabs/Skeleton_Spearman.prefab", 2);
        ConfigureContactDamagePrefab("Assets/Prelabs/SkeletonArcher.prefab", 2);

        ConfigureIntegerFieldPrefab<BatController>("Assets/Prelabs/Bat.prefab", "damage", 1);
        ConfigureIntegerFieldPrefab<Arrow>("Assets/Prelabs/Arrow_0.prefab", "damage", 2);
        ConfigureIntegerFieldPrefab<NecromancerProjectile>(
            "Assets/Prelabs/Boss/NecromancerProjectile.prefab", "damage", 2);
        ConfigureIntegerFieldPrefab<GroundSpell>(
            "Assets/Prelabs/Boss/GroundSpell.prefab", "damage", 2);
        ConfigureIntegerFieldPrefab<LightningStrike>(
            "Assets/Prelabs/Boss/LightningStrike.prefab", "damage", 3);

        foreach (string scenePath in GameplayScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ConfigureScene(scene, heartSprite);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Health HUD and damage balance configured successfully.");
    }

    private static void ConfigureContactDamagePrefab(string prefabPath, int damage)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        SetContactDamage(root, damage);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureIntegerFieldPrefab<T>(string prefabPath, string fieldName, int value)
        where T : Component
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        T component = root.GetComponentInChildren<T>(true);

        if (component == null)
        {
            Debug.LogError(typeof(T).Name + " was not found in " + prefabPath);
        }
        else
        {
            SetIntegerField(component, fieldName, value);
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureScene(Scene scene, Sprite heartSprite)
    {
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            ConfigurePlayer(player, heartSprite);
        }

        foreach (EnemyController slime in Object.FindObjectsByType<EnemyController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SetContactDamage(slime.gameObject, 1);
        }

        foreach (SkeletonEnemy skeleton in Object.FindObjectsByType<SkeletonEnemy>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SetContactDamage(skeleton.gameObject, 2);
            SetIntegerField(skeleton, "attackDamage", 2);
        }

        foreach (SkeletonArcher archer in Object.FindObjectsByType<SkeletonArcher>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SetContactDamage(archer.gameObject, 2);
        }

        NecromancerBoss boss = Object.FindFirstObjectByType<NecromancerBoss>(FindObjectsInactive.Include);
        if (boss != null)
        {
            SetIntegerField(boss, "chargeDamage", 3);
        }
    }

    private static void ConfigurePlayer(PlayerController player, Sprite heartSprite)
    {
        SerializedObject serializedPlayer = new SerializedObject(player);
        serializedPlayer.FindProperty("maxHP").intValue = 10;

        PlayerHealthUI previousHealthUI = serializedPlayer.FindProperty("healthUI")
            .objectReferenceValue as PlayerHealthUI;
        if (previousHealthUI != null)
        {
            Object.DestroyImmediate(previousHealthUI.gameObject);
        }

        TextMeshProUGUIReference legacy = GetLegacyHealthText(serializedPlayer);
        if (legacy.Text == null)
        {
            Debug.LogError("Player health setup could not find the legacy HP text.", player);
            return;
        }

        Transform hudParent = legacy.Text.transform.parent;
        GameObject healthObject = new GameObject(
            "PlayerHealthUI",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(PlayerHealthUI));
        healthObject.layer = 5;
        healthObject.transform.SetParent(hudParent, false);

        RectTransform healthRect = healthObject.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 1f);
        healthRect.anchorMax = new Vector2(0f, 1f);
        healthRect.pivot = new Vector2(0f, 1f);
        healthRect.anchoredPosition = new Vector2(28f, -58f);
        healthRect.sizeDelta = new Vector2(270f, 28f);

        HorizontalLayoutGroup layout = healthObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image[] hearts = new Image[10];
        for (int i = 0; i < hearts.Length; i++)
        {
            GameObject heartObject = new GameObject(
                "Heart_" + (i + 1).ToString("D2"),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            heartObject.layer = 5;
            heartObject.transform.SetParent(healthObject.transform, false);

            RectTransform heartRect = heartObject.GetComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(24f, 24f);

            Image heart = heartObject.GetComponent<Image>();
            heart.sprite = heartSprite;
            heart.preserveAspect = true;
            heart.raycastTarget = false;
            hearts[i] = heart;

            LayoutElement element = heartObject.GetComponent<LayoutElement>();
            element.preferredWidth = 24f;
            element.preferredHeight = 24f;
        }

        PlayerHealthUI healthUI = healthObject.GetComponent<PlayerHealthUI>();
        SerializedObject serializedHealthUI = new SerializedObject(healthUI);
        SerializedProperty heartImages = serializedHealthUI.FindProperty("heartImages");
        heartImages.arraySize = hearts.Length;
        for (int i = 0; i < hearts.Length; i++)
        {
            heartImages.GetArrayElementAtIndex(i).objectReferenceValue = hearts[i];
        }

        serializedHealthUI.FindProperty("fullHeartSprite").objectReferenceValue = heartSprite;
        serializedHealthUI.FindProperty("emptyHeartSprite").objectReferenceValue = null;
        serializedHealthUI.FindProperty("emptyHeartAlpha").floatValue = 0.25f;
        serializedHealthUI.ApplyModifiedPropertiesWithoutUndo();

        legacy.Text.gameObject.SetActive(false);
        Image legacyIcon = hudParent.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(image => image.name == "HPIcon");
        if (legacyIcon == null)
        {
            legacyIcon = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(image => image.name == "HPIcon");
        }

        if (legacyIcon != null)
        {
            legacyIcon.gameObject.SetActive(false);
        }

        serializedPlayer.FindProperty("healthUI").objectReferenceValue = healthUI;
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(healthUI);
    }

    private static TextMeshProUGUIReference GetLegacyHealthText(SerializedObject serializedPlayer)
    {
        SerializedProperty property = serializedPlayer.FindProperty("legacyHPText");
        return new TextMeshProUGUIReference(property?.objectReferenceValue as TMPro.TextMeshProUGUI);
    }

    private static void SetContactDamage(GameObject target, int damage)
    {
        ContactDamage contactDamage = target.GetComponent<ContactDamage>();
        if (contactDamage == null)
        {
            contactDamage = target.AddComponent<ContactDamage>();
        }

        SetIntegerField(contactDamage, "damage", damage);
    }

    private static void SetIntegerField(Component component, string fieldName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError(component.GetType().Name + " has no serialized field named " + fieldName, component);
            return;
        }

        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private readonly struct TextMeshProUGUIReference
    {
        public readonly TMPro.TextMeshProUGUI Text;

        public TextMeshProUGUIReference(TMPro.TextMeshProUGUI text)
        {
            Text = text;
        }
    }
}
