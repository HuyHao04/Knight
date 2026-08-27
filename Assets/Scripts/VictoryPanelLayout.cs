using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class VictoryPanelLayout
{
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static void Apply(
        GameObject panel,
        TextMeshProUGUI hpText,
        TextMeshProUGUI coinText,
        TextMeshProUGUI killText,
        TextMeshProUGUI totalScoreText)
    {
        if (panel == null)
        {
            return;
        }

        ConfigureCanvas(panel);

        RectTransform panelRect = panel.transform as RectTransform;
        if (panelRect == null)
        {
            return;
        }

        SetRect(panelRect, new Vector2(720f, 165f), new Vector2(0f, 170f));

        if (panel.TryGetComponent(out Image victoryBanner))
        {
            victoryBanner.preserveAspect = true;
        }

        RectTransform background = FindDirectChild(panelRect, "Image");
        SetRect(background, new Vector2(560f, 400f), new Vector2(0f, -235f));
        background?.SetAsFirstSibling();

        ConfigureIcon(panelRect, "HPIcon", new Vector2(-220f, -125f));
        ConfigureIcon(panelRect, "CoinIcon", new Vector2(-220f, -185f));
        ConfigureIcon(panelRect, "KillIcon", new Vector2(-220f, -245f));

        ConfigureStatText(hpText, new Vector2(25f, -125f), false);
        ConfigureStatText(coinText, new Vector2(25f, -185f), false);
        ConfigureStatText(killText, new Vector2(25f, -245f), false);
        ConfigureStatText(totalScoreText, new Vector2(0f, -305f), true);

        ArrangeButtons(panelRect);
    }

    private static void ConfigureCanvas(GameObject panel)
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas == null || !canvas.TryGetComponent(out CanvasScaler scaler))
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void ConfigureIcon(RectTransform parent, string childName, Vector2 position)
    {
        RectTransform icon = FindDirectChild(parent, childName);
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(true);
        SetRect(icon, new Vector2(38f, 38f), position);

        if (icon.TryGetComponent(out Image image))
        {
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }

    private static void ConfigureStatText(
        TextMeshProUGUI label,
        Vector2 position,
        bool centered)
    {
        if (label == null)
        {
            return;
        }

        RectTransform rect = label.rectTransform;
        SetRect(rect, new Vector2(centered ? 480f : 430f, 46f), position);

        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 26f;
        label.alignment = centered
            ? TextAlignmentOptions.Center
            : TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
    }

    private static void ArrangeButtons(RectTransform panelRect)
    {
        List<RectTransform> buttons = new List<RectTransform>();

        for (int i = 0; i < panelRect.childCount; i++)
        {
            RectTransform child = panelRect.GetChild(i) as RectTransform;
            if (child != null && child.TryGetComponent<Button>(out _))
            {
                buttons.Add(child);
            }
        }

        buttons.Sort((left, right) =>
            GetButtonOrder(left.name).CompareTo(GetButtonOrder(right.name)));

        const float spacing = 115f;
        float firstX = -spacing * (buttons.Count - 1) * 0.5f;

        for (int i = 0; i < buttons.Count; i++)
        {
            SetRect(
                buttons[i],
                new Vector2(90f, 45f),
                new Vector2(firstX + spacing * i, -390f));
        }
    }

    private static int GetButtonOrder(string buttonName)
    {
        string normalizedName = buttonName.Trim();

        if (normalizedName.Equals("BackBtn", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (normalizedName.Equals("AgainBtn", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (normalizedName.Equals("NextBtn", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private static RectTransform FindDirectChild(RectTransform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform child = parent.GetChild(i) as RectTransform;
            if (child != null
                && child.name.Trim().Equals(childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
