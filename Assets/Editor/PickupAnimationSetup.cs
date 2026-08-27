using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the animated Coin and Heart pickups from the Medieval Ruins sprites.
/// Pickup effects remain owned by PlayerController; this utility only prepares
/// visuals, Animator components, trigger colliders, and the existing prefabs.
/// </summary>
public static class PickupAnimationSetup
{
    private const string CollectableFolder =
        "Assets/Sprites/craftpix-net-370528-free-medieval-ruins-cartoon-2d-tileset/PNG/Collectable Object";
    private const string AnimationFolder = "Assets/Animation/Pickups";
    private const string CoinPrefabPath = "Assets/Prelabs/Coin.prefab";
    private const string HeartPrefabPath = "Assets/Prelabs/Heart.prefab";

    [MenuItem("Tools/Pickups/Create Animated Coin and Heart")]
    public static void CreateOrUpdate()
    {
        EnsureFolder(AnimationFolder);

        List<Sprite> coinFrames = Enumerable.Range(1, 6)
            .Select(index => LoadSprite(CollectableFolder + "/Coin_0" + index + ".png"))
            .ToList();
        Sprite heartSprite = LoadSprite(CollectableFolder + "/Life.png");

        AnimationClip coinClip = CreateCoinClip(coinFrames);
        AnimationClip heartClip = CreateHeartClip(heartSprite);
        AnimatorController coinController = CreateSingleStateController("Coin", coinClip);
        AnimatorController heartController = CreateSingleStateController("Heart", heartClip);

        SavePickupPrefab(
            CoinPrefabPath,
            "Coin",
            "Coin",
            coinFrames[0],
            coinController
        );
        SavePickupPrefab(
            HeartPrefabPath,
            "Heart",
            "Heart",
            heartSprite,
            heartController
        );

        UpdateExistingScenePickups(coinFrames[0], heartSprite, coinController, heartController);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Animated Coin and Heart pickups were created and scene pickups were updated.");
    }

    private static AnimationClip CreateCoinClip(IReadOnlyList<Sprite> frames)
    {
        const string path = AnimationFolder + "/Coin_Idle.anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { name = "Coin_Idle", frameRate = 10f };
        ObjectReferenceKeyframe[] keys = frames
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
            keys
        );
        SetLooping(clip);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateHeartClip(Sprite heartSprite)
    {
        const string path = AnimationFolder + "/Heart_Idle.anim";
        AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { name = "Heart_Idle", frameRate = 6f };
        // Keeps the original Life sprite while adding a gentle shine. Transform
        // properties are deliberately untouched so placed Heart instances keep
        // their designer-authored positions.
        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
            new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = heartSprite },
                new ObjectReferenceKeyframe { time = 0.4f, value = heartSprite },
                new ObjectReferenceKeyframe { time = 0.8f, value = heartSprite }
            }
        );
        SetColorCurve(clip, "m_Color.r", 1f, 1f, 1f);
        SetColorCurve(clip, "m_Color.g", 1f, 0.9f, 1f);
        SetColorCurve(clip, "m_Color.b", 1f, 0.9f, 1f);
        SetColorCurve(clip, "m_Color.a", 1f, 0.9f, 1f);
        SetLooping(clip);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetColorCurve(AnimationClip clip, string propertyName, float start, float middle, float end)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, start),
            new Keyframe(0.4f, middle),
            new Keyframe(0.8f, end)
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(string.Empty, typeof(SpriteRenderer), propertyName),
            curve
        );
    }

    private static void SetLooping(AnimationClip clip)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static AnimatorController CreateSingleStateController(string pickupName, AnimationClip clip)
    {
        string path = AnimationFolder + "/" + pickupName + ".controller";
        AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState(pickupName + "_Idle");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    private static void SavePickupPrefab(
        string prefabPath,
        string objectName,
        string tagName,
        Sprite initialSprite,
        RuntimeAnimatorController controller)
    {
        GameObject root;
        bool loadedPrefab = File.Exists(prefabPath);
        if (loadedPrefab)
        {
            root = PrefabUtility.LoadPrefabContents(prefabPath);
        }
        else
        {
            root = new GameObject(objectName);
        }

        ConfigurePickup(root, objectName, tagName, initialSprite, controller);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        if (loadedPrefab)
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void ConfigurePickup(
        GameObject pickup,
        string objectName,
        string tagName,
        Sprite initialSprite,
        RuntimeAnimatorController controller)
    {
        pickup.name = objectName;
        pickup.tag = tagName;
        pickup.layer = LayerMask.NameToLayer("Default");
        pickup.transform.localScale = Vector3.one * 0.45f;

        SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = pickup.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = initialSprite;
        renderer.sortingLayerName = "Item";
        renderer.sortingOrder = 1;
        renderer.color = Color.white;

        Animator animator = pickup.GetComponent<Animator>();
        if (animator == null)
        {
            animator = pickup.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        CircleCollider2D collider = pickup.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = pickup.AddComponent<CircleCollider2D>();
        }

        collider.isTrigger = true;
        collider.offset = Vector2.zero;
        collider.radius = 0.48f;
    }

    private static void UpdateExistingScenePickups(
        Sprite coinSprite,
        Sprite heartSprite,
        RuntimeAnimatorController coinController,
        RuntimeAnimatorController heartController)
    {
        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity")
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.CompareTag("Coin"))
                    {
                        ConfigurePickup(child.gameObject, child.name, "Coin", coinSprite, coinController);
                    }
                    else if (child.CompareTag("Heart"))
                    {
                        ConfigurePickup(child.gameObject, child.name, "Heart", heartSprite, heartController);
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
        }
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            throw new FileNotFoundException("Pickup sprite was not found", assetPath);
        }

        return sprite;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
    }
}
