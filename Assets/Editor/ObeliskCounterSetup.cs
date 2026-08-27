using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ObeliskCounterSetup
{
    private const string MenuPath = "Tools/Boss/Configure Two Obelisk Counter";
    private const string BossScenePath = "Assets/Scenes/Boss.unity";
    private const string ObeliskSheetPath = "Assets/Sprites/Effect/Obelisk/Obelisk.png";
    private const string ObeliskEffectsPath = "Assets/Sprites/Effect/Obelisk/Obelisk_effects.png";
    private const string BeamFramesFolder = "Assets/Sprites/Effect/Obelisk/BeamFrames";
    private const string AnimationFolder = "Assets/Animation/ObeliskCounter";
    private const string PrefabFolder = "Assets/Prelabs/Boss";
    private const string ObeliskControllerPath = AnimationFolder + "/Obelisk.controller";
    private const string BeamControllerPath = AnimationFolder + "/EnergyBeam.controller";
    private const string ObeliskPrefabPath = PrefabFolder + "/Obelisk.prefab";
    private const string BeamPrefabPath = PrefabFolder + "/EnergyBeam.prefab";

    [MenuItem("Tools/Boss/Repair Obelisk Interaction Prompt")]
    public static void RepairInteractionPrompt()
    {
        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        ObeliskManager manager = UnityEngine.Object.FindAnyObjectByType<ObeliskManager>(
            FindObjectsInactive.Include);

        if (manager == null)
        {
            throw new InvalidOperationException("Boss scene is missing ObeliskManager.");
        }

        PortalPromptUI prompt = EnsureSharedPrompt();
        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("interactionPrompt").objectReferenceValue = prompt;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        foreach (Portal portal in UnityEngine.Object.FindObjectsByType<Portal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            SerializedObject portalData = new SerializedObject(portal);
            portalData.FindProperty("promptUI").objectReferenceValue = prompt;
            portalData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(portal);
        }

        EditorUtility.SetDirty(prompt);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("OBELISK PROMPT REPAIRED: shared [E] interaction UI restored and assigned.");
    }

    [MenuItem(MenuPath)]
    public static void ConfigureTwoObeliskCounter()
    {
        EnsureFolder(AnimationFolder);
        EnsureFolder(PrefabFolder);
        ConfigureBeamFrameImporters();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        AnimatorController obeliskAnimator = CreateObeliskAnimator();
        AnimatorController beamAnimator = CreateBeamAnimator();
        GameObject obeliskPrefab = CreateObeliskPrefab(obeliskAnimator);
        GameObject beamPrefab = CreateBeamPrefab(beamAnimator);

        Scene scene = EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        NecromancerBoss boss = UnityEngine.Object.FindFirstObjectByType<NecromancerBoss>(
            FindObjectsInactive.Include);

        if (boss == null)
        {
            Debug.LogError("Two Obelisk setup requires NecromancerBoss in Boss scene.");
            return;
        }

        PortalPromptUI prompt = EnsureSharedPrompt();
        GameObject mechanicRoot = GameObject.Find("ObeliskMechanic");
        if (mechanicRoot == null)
        {
            mechanicRoot = new GameObject("ObeliskMechanic");
        }

        ObeliskManager manager = mechanicRoot.GetComponent<ObeliskManager>();
        if (manager == null)
        {
            manager = mechanicRoot.AddComponent<ObeliskManager>();
        }

        ObeliskController left = EnsureObeliskInstance(
            mechanicRoot.transform,
            "Obelisk_Left",
            obeliskPrefab);
        ObeliskController right = EnsureObeliskInstance(
            mechanicRoot.transform,
            "Obelisk_Right",
            obeliskPrefab);

        Transform skillPosition = GetSerializedReference<Transform>(boss, "bossSkillPosition");
        float centerX = skillPosition != null ? skillPosition.position.x : boss.transform.position.x;
        Collider2D bossCollider = boss.GetComponent<Collider2D>();
        float groundY = bossCollider != null ? bossCollider.bounds.min.y : boss.transform.position.y - 1.5f;
        float obeliskHalfHeight = GetObeliskHalfHeight(left);

        left.transform.position = new Vector3(centerX - 5.5f, groundY + obeliskHalfHeight, 0f);
        right.transform.position = new Vector3(centerX + 5.5f, groundY + obeliskHalfHeight, 0f);

        Transform beamTarget = boss.transform.Find("ObeliskBeamTarget");
        if (beamTarget == null)
        {
            GameObject targetObject = new GameObject("ObeliskBeamTarget");
            beamTarget = targetObject.transform;
            beamTarget.SetParent(boss.transform, false);
            beamTarget.localPosition = new Vector3(0f, 0.2f, 0f);
        }

        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("leftObelisk").objectReferenceValue = left;
        managerData.FindProperty("rightObelisk").objectReferenceValue = right;
        managerData.FindProperty("boss").objectReferenceValue = boss;
        managerData.FindProperty("beamPrefab").objectReferenceValue =
            beamPrefab.GetComponent<EnergyBeamController>();
        managerData.FindProperty("interactionPrompt").objectReferenceValue = prompt;
        managerData.FindProperty("obeliskDamage").intValue = 25;
        managerData.FindProperty("chargeDelay").floatValue = 0.3f;
        managerData.FindProperty("beamHitDelay").floatValue = 0.2f;
        managerData.FindProperty("beamVisibleDuration").floatValue = 0.75f;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bossData = new SerializedObject(boss);
        bossData.FindProperty("obeliskManager").objectReferenceValue = manager;
        bossData.FindProperty("obeliskBeamTarget").objectReferenceValue = beamTarget;
        bossData.FindProperty("obeliskStunDuration").floatValue = 2.5f;
        bossData.ApplyModifiedPropertiesWithoutUndo();

        foreach (Portal portal in UnityEngine.Object.FindObjectsByType<Portal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            SerializedObject portalData = new SerializedObject(portal);
            portalData.FindProperty("promptUI").objectReferenceValue = prompt;
            portalData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(portal);
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(left);
        EditorUtility.SetDirty(right);
        EditorUtility.SetDirty(boss);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Two Obelisk Counter configured. Left=" + left.transform.position
            + ", Right=" + right.transform.position
            + ", Target=" + beamTarget.position);
    }

    public static void ValidateTwoObeliskCounter()
    {
        EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);

        NecromancerBoss boss = UnityEngine.Object.FindFirstObjectByType<NecromancerBoss>(
            FindObjectsInactive.Include);
        ObeliskManager manager = UnityEngine.Object.FindFirstObjectByType<ObeliskManager>(
            FindObjectsInactive.Include);
        ObeliskController[] obelisks = UnityEngine.Object.FindObjectsByType<ObeliskController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        PortalPromptUI prompt = UnityEngine.Object.FindFirstObjectByType<PortalPromptUI>(
            FindObjectsInactive.Include);

        Require(boss != null, "Boss scene is missing NecromancerBoss.");
        Require(manager != null, "Boss scene is missing ObeliskManager.");
        Require(obelisks.Length == 2, "Boss scene must contain exactly two ObeliskControllers.");
        Require(prompt != null, "Boss scene is missing the shared interaction prompt.");

        SerializedObject bossData = new SerializedObject(boss);
        SerializedObject managerData = new SerializedObject(manager);
        Require(
            bossData.FindProperty("obeliskManager").objectReferenceValue == manager,
            "NecromancerBoss is not linked to ObeliskManager.");
        Require(
            bossData.FindProperty("obeliskBeamTarget").objectReferenceValue != null,
            "NecromancerBoss is missing ObeliskBeamTarget.");
        Require(
            Mathf.Approximately(bossData.FindProperty("obeliskStunDuration").floatValue, 2.5f),
            "Obelisk stun duration must be 2.5 seconds.");
        Require(
            managerData.FindProperty("leftObelisk").objectReferenceValue != null
            && managerData.FindProperty("rightObelisk").objectReferenceValue != null,
            "ObeliskManager is missing Left or Right Obelisk.");
        Require(
            managerData.FindProperty("beamPrefab").objectReferenceValue != null,
            "ObeliskManager is missing Energy Beam prefab.");
        Require(
            managerData.FindProperty("interactionPrompt").objectReferenceValue == prompt,
            "ObeliskManager is not using the shared prompt.");
        Require(
            managerData.FindProperty("obeliskDamage").intValue == 25,
            "Obelisk damage must be 25.");

        foreach (ObeliskController obelisk in obelisks)
        {
            BoxCollider2D trigger = obelisk.GetComponent<BoxCollider2D>();
            Require(trigger != null && trigger.isTrigger, obelisk.name + " needs an interaction trigger.");
            Require(obelisk.BeamOrigin != obelisk.transform, obelisk.name + " is missing BeamOrigin.");
            Animator animator = obelisk.GetComponent<Animator>();
            Require(
                animator != null && animator.runtimeAnimatorController != null,
                obelisk.name + " is missing its Animator Controller.");
        }

        GameObject beamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabPath);
        Require(beamPrefab != null, "EnergyBeam prefab was not generated.");
        Require(
            beamPrefab.GetComponent<EnergyBeamController>() != null,
            "EnergyBeam prefab is missing EnergyBeamController.");
        Require(
            AssetDatabase.FindAssets("t:Sprite", new[] { BeamFramesFolder }).Length == 26,
            "Energy Beam animation must contain 26 imported sprites.");
        Require(
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ObeliskControllerPath) != null,
            "Obelisk Animator Controller was not generated.");
        Require(
            AssetDatabase.LoadAssetAtPath<AnimatorController>(BeamControllerPath) != null,
            "Energy Beam Animator Controller was not generated.");

        Debug.Log(
            "OBELISK VALIDATION PASSED: 2 obelisks, 26 beam frames, damage=25, stun=2.5s, all references assigned.");
    }

    private static AnimatorController CreateObeliskAnimator()
    {
        Sprite[] baseSprites = LoadSprites(ObeliskSheetPath)
            .OrderBy(sprite => NumericSuffix(sprite.name))
            .ToArray();
        Sprite[] effectSprites = LoadSprites(ObeliskEffectsPath)
            .Where(sprite => NumericSuffix(sprite.name) >= 30 && NumericSuffix(sprite.name) <= 43)
            .OrderBy(sprite => NumericSuffix(sprite.name))
            .ToArray();

        if (baseSprites.Length < 14 || effectSprites.Length < 14)
        {
            throw new InvalidOperationException(
                "Obelisk sheets do not contain the expected 14 base and 14 effect frames.");
        }

        AnimationClip inactive = CreateSpriteClip(
            AnimationFolder + "/Obelisk_Inactive.anim",
            new[] { baseSprites[8] },
            10f,
            true);
        AnimationClip ready = CreateSpriteClip(
            AnimationFolder + "/Obelisk_Ready.anim",
            baseSprites,
            10f,
            true);
        AnimationClip activated = CreateSpriteClip(
            AnimationFolder + "/Obelisk_Activated.anim",
            new[] { baseSprites[0] },
            10f,
            true);
        AnimationClip firing = CreateSpriteClip(
            AnimationFolder + "/Obelisk_Firing.anim",
            effectSprites,
            12f,
            true);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            ObeliskControllerPath);
        if (controller != null)
        {
            return controller;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(ObeliskControllerPath);
        controller.AddParameter("State", AnimatorControllerParameterType.Int);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState inactiveState = stateMachine.AddState("Inactive");
        AnimatorState readyState = stateMachine.AddState("Ready");
        AnimatorState activatedState = stateMachine.AddState("Activated");
        AnimatorState firingState = stateMachine.AddState("Firing");
        inactiveState.motion = inactive;
        readyState.motion = ready;
        activatedState.motion = activated;
        firingState.motion = firing;
        stateMachine.defaultState = inactiveState;

        AddStateTransition(stateMachine, inactiveState, 0);
        AddStateTransition(stateMachine, readyState, 1);
        AddStateTransition(stateMachine, activatedState, 2);
        AddStateTransition(stateMachine, firingState, 3);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorController CreateBeamAnimator()
    {
        Sprite[] frames = AssetDatabase.FindAssets("t:Sprite", new[] { BeamFramesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .Where(sprite => sprite != null)
            .OrderBy(sprite => NumericSuffix(sprite.name))
            .ToArray();

        if (frames.Length != 26)
        {
            throw new InvalidOperationException("Energy Beam requires exactly 26 extracted GIF frames.");
        }

        AnimationClip beamClip = CreateSpriteClip(
            AnimationFolder + "/EnergyBeam.anim",
            frames,
            16.6667f,
            true);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            BeamControllerPath);
        if (controller != null)
        {
            return controller;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(BeamControllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState("EnergyBeam");
        state.motion = beamClip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateObeliskPrefab(AnimatorController controller)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ObeliskPrefabPath);
        if (existing != null)
        {
            return existing;
        }

        Sprite inactiveSprite = LoadSprites(ObeliskSheetPath)
            .First(sprite => sprite.name == "Obelisk_8");
        GameObject root = new GameObject("Obelisk");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = inactiveSprite;
        renderer.sortingLayerName = "Enemy";
        renderer.sortingOrder = 2;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.6f, 3.2f);

        GameObject originObject = new GameObject("BeamOrigin");
        originObject.transform.SetParent(root.transform, false);
        originObject.transform.localPosition = new Vector3(0f, 0.78f, 0f);

        ObeliskController controllerComponent = root.AddComponent<ObeliskController>();
        SerializedObject controllerData = new SerializedObject(controllerComponent);
        controllerData.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        controllerData.FindProperty("animator").objectReferenceValue = animator;
        controllerData.FindProperty("beamOrigin").objectReferenceValue = originObject.transform;
        controllerData.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ObeliskPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateBeamPrefab(AnimatorController controller)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabPath);
        if (existing != null)
        {
            EnergyBeamController existingBeam = existing.GetComponent<EnergyBeamController>();
            if (existingBeam != null)
            {
                SerializedObject existingData = new SerializedObject(existingBeam);
                existingData.FindProperty("spriteAngleOffset").floatValue = 180f;
                existingData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existingBeam);
                EditorUtility.SetDirty(existing);
            }

            return existing;
        }

        Sprite firstFrame = AssetDatabase.FindAssets("Beam_00 t:Sprite", new[] { BeamFramesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .First(sprite => sprite != null);

        GameObject root = new GameObject("EnergyBeam");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = firstFrame;
        renderer.sortingLayerName = "Enemy";
        renderer.sortingOrder = 5;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        EnergyBeamController beam = root.AddComponent<EnergyBeamController>();
        SerializedObject beamData = new SerializedObject(beam);
        beamData.FindProperty("beamRenderer").objectReferenceValue = renderer;
        beamData.FindProperty("animator").objectReferenceValue = animator;
        beamData.FindProperty("nativeLength").floatValue = 1.92f;
        beamData.FindProperty("thicknessScale").floatValue = 0.55f;
        beamData.FindProperty("spriteAngleOffset").floatValue = 180f;
        beamData.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BeamPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static PortalPromptUI EnsureSharedPrompt()
    {
        PortalPromptUI existing = UnityEngine.Object.FindFirstObjectByType<PortalPromptUI>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            throw new InvalidOperationException("Boss scene has no Canvas for interaction prompt.");
        }

        GameObject promptObject = new GameObject(
            "InteractionPrompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(CanvasGroup),
            typeof(PortalPromptUI));
        promptObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = promptObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -250f);
        rect.sizeDelta = new Vector2(360f, 64f);

        TextMeshProUGUI text = promptObject.GetComponent<TextMeshProUGUI>();
        text.text = "[E] Activate";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        CanvasGroup group = promptObject.GetComponent<CanvasGroup>();
        PortalPromptUI prompt = promptObject.GetComponent<PortalPromptUI>();
        SerializedObject promptData = new SerializedObject(prompt);
        promptData.FindProperty("promptGroup").objectReferenceValue = group;
        promptData.FindProperty("promptText").objectReferenceValue = text;
        promptData.FindProperty("message").stringValue = "[E] Enter";
        promptData.ApplyModifiedPropertiesWithoutUndo();
        group.alpha = 0f;
        return prompt;
    }

    private static ObeliskController EnsureObeliskInstance(
        Transform parent,
        string objectName,
        GameObject prefab)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<ObeliskController>();
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = objectName;
        instance.transform.SetParent(parent, true);
        return instance.GetComponent<ObeliskController>();
    }

    private static float GetObeliskHalfHeight(ObeliskController obelisk)
    {
        SpriteRenderer renderer = obelisk.GetComponent<SpriteRenderer>();
        return renderer != null && renderer.sprite != null
            ? renderer.sprite.bounds.extents.y
            : 1.03f;
    }

    private static AnimationClip CreateSpriteClip(
        string path,
        IReadOnlyList<Sprite> sprites,
        float frameRate,
        bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[Mathf.Max(2, sprites.Count)];
        for (int i = 0; i < keys.Length; i++)
        {
            int spriteIndex = Mathf.Min(i, sprites.Count - 1);
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[spriteIndex]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        SerializedObject clipData = new SerializedObject(clip);
        SerializedProperty settings = clipData.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            SerializedProperty loopTime = settings.FindPropertyRelative("m_LoopTime");
            if (loopTime != null)
            {
                loopTime.boolValue = loop;
            }
        }

        clipData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        int stateValue)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToArray();
    }

    private static int NumericSuffix(string assetName)
    {
        int separator = assetName.LastIndexOf('_');
        return separator >= 0 && int.TryParse(assetName.Substring(separator + 1), out int value)
            ? value
            : -1;
    }

    private static T GetSerializedReference<T>(UnityEngine.Object target, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedObject data = new SerializedObject(target);
        return data.FindProperty(propertyName).objectReferenceValue as T;
    }

    private static void ConfigureBeamFrameImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BeamFramesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
        string name = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
