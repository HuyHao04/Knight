using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates the two configured melee skeleton prefabs from the existing sprite sheets.
/// Run again safely to refresh only the assets owned by this setup.
/// </summary>
public static class SkeletonEnemySetup
{
    private const string AnimationFolder = "Assets/Animation/SkeletonEnemies";
    private const string PrefabFolder = "Assets/Prelabs";

    [MenuItem("Tools/Enemies/Create Skeleton Warrior and Spearman")]
    public static void CreateOrUpdate()
    {
        EnsureFolder(AnimationFolder);

        CreateSkeleton(
            "Skeleton_Warrior",
            "Assets/Sprites/Enemy/Skeleton_Warrior",
            moveSpeed: 2.5f,
            detectionRange: 6f,
            attackRange: 1.2f,
            attackCooldown: 1.2f,
            attackHitDelay: 0.28f,
            attackAnimationDuration: 0.5f,
            attackPointOffset: 0.85f,
            attackHitRadius: 0.4f
        );

        CreateSkeleton(
            "Skeleton_Spearman",
            "Assets/Sprites/Enemy/Skeleton_Spearman",
            moveSpeed: 2.2f,
            detectionRange: 7f,
            attackRange: 1.8f,
            attackCooldown: 1.4f,
            attackHitDelay: 0.16f,
            attackAnimationDuration: 0.34f,
            attackPointOffset: 1.35f,
            attackHitRadius: 0.5f
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Skeleton Warrior and Spearman prefabs were created successfully.");
    }

    [MenuItem("Tools/Enemies/Validate Skeleton Warrior and Spearman")]
    public static void ValidateMenuItem()
    {
        ValidatePrefab("Skeleton_Warrior");
        ValidatePrefab("Skeleton_Spearman");
        Debug.Log("Skeleton enemy setup validation succeeded.");
    }

    private static void CreateSkeleton(
        string skeletonName,
        string spriteFolder,
        float moveSpeed,
        float detectionRange,
        float attackRange,
        float attackCooldown,
        float attackHitDelay,
        float attackAnimationDuration,
        float attackPointOffset,
        float attackHitRadius)
    {
        AnimationClip idle = CreateSpriteClip(
            skeletonName + "_Idle",
            spriteFolder + "/Idle.png",
            true
        );
        AnimationClip run = CreateSpriteClip(
            skeletonName + "_Run",
            spriteFolder + "/Run.png",
            true
        );
        AnimationClip attack = CreateSpriteClip(
            skeletonName + "_Attack",
            spriteFolder + "/Attack_1.png",
            false
        );
        AnimatorController controller = CreateController(
            skeletonName,
            idle,
            run,
            attack
        );

        GameObject root = new GameObject(skeletonName);
        root.tag = "enemy";
        root.layer = LayerMask.NameToLayer("Default");

        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadSprites(spriteFolder + "/Idle.png")[0];
        spriteRenderer.sortingLayerName = "Enemy";

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 1f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = spriteRenderer.sprite.bounds.size * 0.85f;
        collider.offset = spriteRenderer.sprite.bounds.center;

        Transform attackPoint = CreateChild(root.transform, "AttackPoint", new Vector3(attackPointOffset, 0.35f, 0f));
        float sensorX = Mathf.Max(0.25f, collider.size.x * 0.5f + 0.08f);
        Transform wallCheck = CreateChild(root.transform, "WallCheck", new Vector3(sensorX, 0f, 0f));
        Transform groundCheck = CreateChild(root.transform, "GroundCheck", new Vector3(sensorX, -collider.size.y * 0.5f + 0.04f, 0f));
        SkeletonEnemy skeleton = root.AddComponent<SkeletonEnemy>();

        SerializedObject serializedSkeleton = new SerializedObject(skeleton);
        serializedSkeleton.FindProperty("moveSpeed").floatValue = moveSpeed;
        serializedSkeleton.FindProperty("detectionRange").floatValue = detectionRange;
        serializedSkeleton.FindProperty("detectionExitBuffer").floatValue = 0.75f;
        serializedSkeleton.FindProperty("attackRange").floatValue = attackRange;
        serializedSkeleton.FindProperty("maxVerticalDetectionDifference").floatValue = 2.5f;
        serializedSkeleton.FindProperty("facingUpdateThreshold").floatValue = 0.1f;
        serializedSkeleton.FindProperty("attackCooldown").floatValue = attackCooldown;
        serializedSkeleton.FindProperty("attackDamage").intValue = 2;
        serializedSkeleton.FindProperty("attackHitDelay").floatValue = attackHitDelay;
        serializedSkeleton.FindProperty("attackAnimationDuration").floatValue = attackAnimationDuration;
        serializedSkeleton.FindProperty("attackPoint").objectReferenceValue = attackPoint;
        serializedSkeleton.FindProperty("attackPointOffset").floatValue = attackPointOffset;
        serializedSkeleton.FindProperty("attackHitRadius").floatValue = attackHitRadius;
        serializedSkeleton.FindProperty("playerLayers").intValue = ~0;
        serializedSkeleton.FindProperty("wallCheck").objectReferenceValue = wallCheck;
        serializedSkeleton.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        serializedSkeleton.FindProperty("wallCheckDistance").floatValue = 0.15f;
        serializedSkeleton.FindProperty("groundCheckDistance").floatValue = 0.55f;
        serializedSkeleton.FindProperty("groundLayers").intValue = ~0;
        serializedSkeleton.FindProperty("ignoreActorBodyCollisions").boolValue = true;
        serializedSkeleton.FindProperty("ignoreNonGroundCollisions").boolValue = true;
        serializedSkeleton.FindProperty("debugLogging").boolValue = false;
        serializedSkeleton.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = PrefabFolder + "/" + skeletonName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static AnimationClip CreateSpriteClip(
        string clipName,
        string spritePath,
        bool loop)
    {
        string path = AnimationFolder + "/" + clipName + ".anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { frameRate = 12f, name = clipName };
        Sprite[] sprites = LoadSprites(spritePath);
        ObjectReferenceKeyframe[] frames = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController CreateController(
        string skeletonName,
        AnimationClip idle,
        AnimationClip run,
        AnimationClip attack)
    {
        string path = AnimationFolder + "/" + skeletonName + ".controller";
        AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState runState = machine.AddState("Run");
        AnimatorState attackState = machine.AddState("Attack");
        idleState.motion = idle;
        runState.motion = run;
        attackState.motion = attack;
        machine.defaultState = idleState;

        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        ConfigureBoolTransition(idleToRun, true);
        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        ConfigureBoolTransition(runToIdle, false);

        AnimatorStateTransition anyToAttack = machine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.03f;
        anyToAttack.canTransitionToSelf = false;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.98f;
        attackToIdle.duration = 0.03f;

        return controller;
    }

    private static void ConfigureBoolTransition(
        AnimatorStateTransition transition,
        bool isMoving)
    {
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(
            isMoving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            "IsMoving"
        );
    }

    private static Transform CreateChild(Transform parent, string childName, Vector3 localPosition)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child.transform;
    }

    private static Sprite[] LoadSprites(string assetPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new InvalidOperationException("No sprites found at " + assetPath);
        }

        return sprites;
    }

    private static void ValidatePrefab(string skeletonName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PrefabFolder + "/" + skeletonName + ".prefab"
        );

        if (prefab == null || prefab.tag != "enemy")
        {
            throw new InvalidOperationException(skeletonName + " prefab or enemy tag is missing.");
        }

        SkeletonEnemy skeleton = prefab.GetComponent<SkeletonEnemy>();
        Animator animator = prefab.GetComponent<Animator>();
        Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
        Collider2D collider = prefab.GetComponent<Collider2D>();

        if (
            skeleton == null || animator == null ||
            animator.runtimeAnimatorController == null ||
            body == null || body.constraints != RigidbodyConstraints2D.FreezeRotation ||
            collider == null || prefab.transform.Find("AttackPoint") == null ||
            prefab.transform.Find("WallCheck") == null ||
            prefab.transform.Find("GroundCheck") == null
        )
        {
            throw new InvalidOperationException(skeletonName + " prefab is incomplete.");
        }
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        Directory.CreateDirectory(assetFolder);
        AssetDatabase.Refresh();
    }
}
