using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level2GroundStabilitySetup
{
    private const string ScenePath = "Assets/Scenes/Level_2.unity";

    [MenuItem("Tools/Levels/Fix Level 2 Ground Stability")]
    public static void FixGroundStability()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform gridRoot = FindRoot(scene, "Grid");
        Require(gridRoot != null, "Level_2 is missing its Grid root.");

        Transform bridgeRoot = FindRoot(scene, "Bridge");
        Require(bridgeRoot != null, "Level_2 is missing its Bridge root.");

        if (IsGroundStable(gridRoot, bridgeRoot))
        {
            ValidateScene(gridRoot, bridgeRoot);
            Debug.Log("LEVEL 2 GROUND STABILITY FIX SKIPPED: scene is already stable.");
            return;
        }

        DisableLegacyMovementControllers();

        int fixedBodyCount = 0;
        foreach (TilemapCollider2D tilemapCollider in UnityEngine.Object.FindObjectsByType<TilemapCollider2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!tilemapCollider.transform.IsChildOf(gridRoot))
            {
                continue;
            }

            Rigidbody2D body = tilemapCollider.attachedRigidbody;
            if (body == null)
            {
                body = tilemapCollider.gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.freezeRotation = true;
            EditorUtility.SetDirty(body);
            fixedBodyCount++;
        }

        foreach (SeesawBridgeController bridge in bridgeRoot.GetComponentsInChildren<SeesawBridgeController>(true))
        {
            Rigidbody2D plank = bridge.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            Require(plank != null && hinge != null && hinge.connectedBody != null,
                bridge.name + " is missing its seesaw physics.");

            plank.bodyType = RigidbodyType2D.Dynamic;
            plank.simulated = true;
            plank.freezeRotation = false;
            hinge.connectedBody.bodyType = RigidbodyType2D.Static;
            hinge.connectedBody.simulated = true;
            EditorUtility.SetDirty(plank);
            EditorUtility.SetDirty(hinge.connectedBody);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidateScene(gridRoot, bridgeRoot);
        Debug.Log("LEVEL 2 GROUND STABILITY FIX COMPLETE: " + fixedBodyCount
            + " tilemap bodies are Static; only seesaw planks remain Dynamic.");
    }

    [MenuItem("Tools/Levels/Validate Level 2 Ground Stability")]
    public static void ValidateGroundStability()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform gridRoot = FindRoot(scene, "Grid");
        Transform bridgeRoot = FindRoot(scene, "Bridge");
        Require(gridRoot != null && bridgeRoot != null,
            "Level_2 is missing Grid or Bridge root.");

        ValidateScene(gridRoot, bridgeRoot);
    }

    private static void ValidateScene(Transform gridRoot, Transform bridgeRoot)
    {

        int tilemapBodyCount = 0;
        foreach (TilemapCollider2D tilemapCollider in UnityEngine.Object.FindObjectsByType<TilemapCollider2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!tilemapCollider.transform.IsChildOf(gridRoot))
            {
                continue;
            }

            Rigidbody2D body = tilemapCollider.attachedRigidbody;
            Require(body != null, GetPath(tilemapCollider.transform) + " needs a Rigidbody2D.");
            Require(body.bodyType == RigidbodyType2D.Static,
                GetPath(tilemapCollider.transform) + " must stay Static.");
            tilemapBodyCount++;
        }

        Require(tilemapBodyCount > 0, "Level_2 has no terrain TilemapCollider2D to validate.");
        Require(!HasEnabledLegacyMovementController(),
            "A legacy moving/falling platform controller is still enabled in Level_2.");

        int bridgeCount = 0;
        foreach (SeesawBridgeController bridge in bridgeRoot.GetComponentsInChildren<SeesawBridgeController>(true))
        {
            Rigidbody2D plank = bridge.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            Require(plank.bodyType == RigidbodyType2D.Dynamic && !plank.freezeRotation,
                bridge.name + " plank must remain freely rotating and Dynamic.");
            Require(hinge.connectedBody != null && hinge.connectedBody.bodyType == RigidbodyType2D.Static,
                bridge.name + " centre anchor must remain Static.");
            Require(!bridge.transform.IsChildOf(gridRoot),
                bridge.name + " must not be parented under the Grid terrain.");
            bridgeCount++;
        }

        Require(bridgeCount > 0, "Level_2 has no seesaw bridge to validate.");
        Debug.Log("LEVEL 2 GROUND STABILITY VALIDATION PASSED: " + tilemapBodyCount
            + " static terrain bodies, " + bridgeCount + " independent seesaws.");
    }

    private static bool IsGroundStable(Transform gridRoot, Transform bridgeRoot)
    {
        foreach (TilemapCollider2D tilemapCollider in UnityEngine.Object.FindObjectsByType<TilemapCollider2D>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!tilemapCollider.transform.IsChildOf(gridRoot))
            {
                continue;
            }

            Rigidbody2D body = tilemapCollider.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Static)
            {
                return false;
            }
        }

        if (HasEnabledLegacyMovementController())
        {
            return false;
        }

        foreach (SeesawBridgeController bridge in bridgeRoot.GetComponentsInChildren<SeesawBridgeController>(true))
        {
            Rigidbody2D plank = bridge.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = bridge.GetComponent<HingeJoint2D>();
            if (plank == null
                || hinge == null
                || hinge.connectedBody == null
                || plank.bodyType != RigidbodyType2D.Dynamic
                || plank.freezeRotation
                || hinge.connectedBody.bodyType != RigidbodyType2D.Static)
            {
                return false;
            }
        }

        return true;
    }

    private static void DisableLegacyMovementControllers()
    {
        foreach (MovingGround controller in UnityEngine.Object.FindObjectsByType<MovingGround>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            controller.enabled = false;
            EditorUtility.SetDirty(controller);
        }

        foreach (HorizontalMovingPlatforms controller in UnityEngine.Object.FindObjectsByType<HorizontalMovingPlatforms>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            controller.enabled = false;
            EditorUtility.SetDirty(controller);
        }

        foreach (VerticalMovingPlatforms controller in UnityEngine.Object.FindObjectsByType<VerticalMovingPlatforms>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            controller.enabled = false;
            EditorUtility.SetDirty(controller);
        }

        foreach (FallingPlatform controller in UnityEngine.Object.FindObjectsByType<FallingPlatform>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            controller.enabled = false;
            EditorUtility.SetDirty(controller);
        }
    }

    private static bool HasEnabledLegacyMovementController()
    {
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (!behaviour.enabled)
            {
                continue;
            }

            if (behaviour is MovingGround
                || behaviour is HorizontalMovingPlatforms
                || behaviour is VerticalMovingPlatforms
                || behaviour is FallingPlatform)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root.transform;
            }
        }

        return null;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
