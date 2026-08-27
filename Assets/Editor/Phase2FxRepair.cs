using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the world-space visual prefabs used by NecromancerBoss Phase 2 and
/// wires them to the existing Boss scene. This is an editor-only repair tool;
/// it is excluded from game builds.
/// </summary>
public static class Phase2FxRepair
{
    private const string AuraSpritePath = "Assets/Sprites/Effect/Aura38.png";
    private const string FlashPrefabPath = "Assets/Prelabs/PhaseFlash.prefab";
    private const string AuraPrefabPath = "Assets/Prelabs/PhaseAura.prefab";
    private const string FlashClipPath = "Assets/Animation/PhaseFlash.anim";
    private const string AuraClipPath = "Assets/Animation/PhaseAura.anim";
    private const string FlashControllerPath = "Assets/Animation/PhaseFlash.controller";
    private const string AuraControllerPath = "Assets/Animation/PhaseAura.controller";
    private const string BossScenePath = "Assets/Scenes/Boss.unity";

    [MenuItem("Tools/Necromancer/Repair Phase 2 Effects")]
    public static void RepairAndValidate()
    {
        Sprite[] auraFrames = LoadAuraFrames();
        if (auraFrames.Length == 0)
        {
            Debug.LogError("Phase 2 repair failed: Aura38 has no sliced sprites.");
            return;
        }

        EnsureFolder("Assets/Animation");
        EnsureFolder("Assets/Prelabs");

        AnimationClip flashClip = CreateSpriteClip(
            FlashClipPath,
            auraFrames.Take(12).ToArray(),
            false
        );
        AnimationClip auraClip = CreateSpriteClip(
            AuraClipPath,
            auraFrames,
            true
        );

        RuntimeAnimatorController flashController = CreateController(
            FlashControllerPath,
            "PhaseFlash",
            flashClip
        );
        RuntimeAnimatorController auraController = CreateController(
            AuraControllerPath,
            "PhaseAura",
            auraClip
        );

        GameObject phaseFlashPrefab = CreateEffectPrefab(
            FlashPrefabPath,
            "PhaseFlash",
            auraFrames[0],
            flashController,
            new Vector3(1.75f, 1.75f, 1f),
            10
        );
        GameObject phaseAuraPrefab = CreateEffectPrefab(
            AuraPrefabPath,
            "PhaseAura",
            auraFrames[0],
            auraController,
            new Vector3(1.6f, 1.6f, 1f),
            -1
        );

        WireBossScene(phaseFlashPrefab, phaseAuraPrefab);
        Validate(phaseFlashPrefab, phaseAuraPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Phase 2 visual repair completed successfully.");
    }

    [MenuItem("Tools/Necromancer/Validate Phase 2 Effects")]
    public static void ValidateMenuItem()
    {
        Validate(
            AssetDatabase.LoadAssetAtPath<GameObject>(FlashPrefabPath),
            AssetDatabase.LoadAssetAtPath<GameObject>(AuraPrefabPath)
        );
    }

    private static Sprite[] LoadAuraFrames()
    {
        return AssetDatabase.LoadAllAssetsAtPath(AuraSpritePath)
            .OfType<Sprite>()
            .OrderBy(SpriteFrameNumber)
            .ToArray();
    }

    private static int SpriteFrameNumber(Sprite sprite)
    {
        int underscoreIndex = sprite.name.LastIndexOf('_');
        int frameNumber;
        return underscoreIndex >= 0 && int.TryParse(
            sprite.name.Substring(underscoreIndex + 1),
            out frameNumber
        ) ? frameNumber : 0;
    }

    private static AnimationClip CreateSpriteClip(
        string assetPath,
        Sprite[] frames,
        bool loop)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AnimationClip clip = new AnimationClip
        {
            frameRate = 12f
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        ObjectReferenceKeyframe[] keyframes = frames
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        SerializedObject serializedClip = new SerializedObject(clip);
        serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    private static RuntimeAnimatorController CreateController(
        string assetPath,
        string stateName,
        AnimationClip clip)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateEffectPrefab(
        string assetPath,
        string objectName,
        Sprite defaultSprite,
        RuntimeAnimatorController controller,
        Vector3 scale,
        int sortingOrder)
    {
        GameObject root = new GameObject(objectName);
        root.transform.localScale = scale;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = defaultSprite;
        renderer.color = Color.white;
        renderer.sortingLayerName = "Enemy";
        renderer.sortingOrder = sortingOrder;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void WireBossScene(
        GameObject phaseFlashPrefab,
        GameObject phaseAuraPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(
            BossScenePath,
            OpenSceneMode.Single
        );
        NecromancerBoss boss = UnityEngine.Object.FindAnyObjectByType<NecromancerBoss>(
            FindObjectsInactive.Include
        );

        if (boss == null)
        {
            throw new InvalidOperationException(
                "NecromancerBoss was not found in Assets/Scenes/Boss.unity."
            );
        }

        SerializedObject serializedBoss = new SerializedObject(boss);
        serializedBoss.FindProperty("phaseFlashPrefab").objectReferenceValue = phaseFlashPrefab;
        serializedBoss.FindProperty("phaseAuraPrefab").objectReferenceValue = phaseAuraPrefab;
        serializedBoss.FindProperty("phaseFlashOffset").vector3Value = Vector3.zero;
        serializedBoss.FindProperty("phaseAuraOffset").vector3Value = Vector3.zero;
        serializedBoss.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Validate(
        GameObject phaseFlashPrefab,
        GameObject phaseAuraPrefab)
    {
        ValidateVisualPrefab(phaseFlashPrefab, "PhaseFlash", 10, false);
        ValidateVisualPrefab(phaseAuraPrefab, "PhaseAura", -1, true);
    }

    private static void ValidateVisualPrefab(
        GameObject prefab,
        string name,
        int expectedSortingOrder,
        bool shouldLoop)
    {
        if (prefab == null)
        {
            throw new InvalidOperationException(name + " prefab is missing.");
        }

        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        Animator animator = prefab.GetComponent<Animator>();
        if (renderer == null || renderer.sprite == null)
        {
            throw new InvalidOperationException(name + " has no visible SpriteRenderer sprite.");
        }

        if (renderer.color.a <= 0f || renderer.sortingLayerName != "Enemy" ||
            renderer.sortingOrder != expectedSortingOrder)
        {
            throw new InvalidOperationException(name + " has invalid visual sorting or alpha settings.");
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            throw new InvalidOperationException(name + " Animator Controller is missing.");
        }

        AnimationClip clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault();
        SerializedObject serializedClip = new SerializedObject(clip);
        bool loops = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue;
        if (loops != shouldLoop)
        {
            throw new InvalidOperationException(name + " loop setting is incorrect.");
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            string parent = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(assetPath);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
