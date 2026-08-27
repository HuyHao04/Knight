using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DamageFlashOverlaySetup
{
    [MenuItem("Tools/UI/Validate Damage Flash Overlay")]
    public static void Validate()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        DamageFlashOverlay.EnsureExists();

        DamageFlashOverlay overlay = UnityEngine.Object.FindFirstObjectByType<DamageFlashOverlay>(
            FindObjectsInactive.Include);
        Require(overlay != null, "DamageFlashOverlay was not created.");

        Canvas canvas = overlay.GetComponent<Canvas>();
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay,
            "Damage overlay must use Screen Space Overlay.");
        Require(canvas.sortingOrder >= 32000,
            "Damage overlay must render above the HUD.");
        Require(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
            "Damage overlay must scale with screen size.");
        Require(group != null && !group.blocksRaycasts && !group.interactable && group.alpha == 0f,
            "Damage overlay must start hidden and never block UI input.");

        Image[] edges = overlay.GetComponentsInChildren<Image>(true);
        Require(edges.Length == 4, "Damage overlay must contain four screen-edge images.");
        foreach (Image edge in edges)
        {
            Require(!edge.raycastTarget, edge.name + " must not receive raycasts.");
            Require(edge.color.r > edge.color.g * 5f && edge.color.r > edge.color.b * 5f,
                edge.name + " must be visibly red.");
        }

        UnityEngine.Object.DestroyImmediate(overlay.gameObject);
        Debug.Log("DAMAGE FLASH OVERLAY VALIDATION PASSED: four responsive red edges, top HUD order, input passthrough.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
