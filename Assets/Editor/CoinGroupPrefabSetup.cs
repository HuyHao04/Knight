using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CoinGroupPrefabSetup
{
    private const string MenuPath = "Tools/Items/Create Coin Group Prefabs";
    private const string CoinPrefabPath = "Assets/Prelabs/Coin.prefab";
    private const string OutputFolder = "Assets/Prelabs/CoinGroups";
    private const float Spacing = 1f;

    private enum GroupLayout
    {
        Horizontal,
        Vertical,
        DiagonalUp,
        DiagonalDown
    }

    private readonly struct GroupDefinition
    {
        public readonly string Name;
        public readonly int Count;
        public readonly GroupLayout Layout;

        public GroupDefinition(string name, int count, GroupLayout layout)
        {
            Name = name;
            Count = count;
            Layout = layout;
        }
    }

    private static readonly GroupDefinition[] Definitions =
    {
        new GroupDefinition("Coin_2_Horizontal", 2, GroupLayout.Horizontal),
        new GroupDefinition("Coin_3_Horizontal", 3, GroupLayout.Horizontal),
        new GroupDefinition("Coin_2_Vertical", 2, GroupLayout.Vertical),
        new GroupDefinition("Coin_3_Vertical", 3, GroupLayout.Vertical),
        new GroupDefinition("Coin_2_DiagonalUp", 2, GroupLayout.DiagonalUp),
        new GroupDefinition("Coin_3_DiagonalUp", 3, GroupLayout.DiagonalUp),
        new GroupDefinition("Coin_2_DiagonalDown", 2, GroupLayout.DiagonalDown),
        new GroupDefinition("Coin_3_DiagonalDown", 3, GroupLayout.DiagonalDown)
    };

    [MenuItem(MenuPath)]
    public static void CreateCoinGroupPrefabs()
    {
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        if (coinPrefab == null)
        {
            throw new InvalidOperationException(
                "Coin group setup could not find " + CoinPrefabPath);
        }

        EnsureOutputFolder();

        foreach (GroupDefinition definition in Definitions)
        {
            CreateGroupPrefab(coinPrefab, definition);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateCoinGroupPrefabs();

        Debug.Log(
            "COIN GROUP PREFABS CREATED: 2/3 coins, horizontal/vertical/diagonal, spacing="
            + Spacing);
    }

    public static void ValidateCoinGroupPrefabs()
    {
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        Require(coinPrefab != null, "The source Coin prefab is missing.");

        foreach (GroupDefinition definition in Definitions)
        {
            string path = GetPrefabPath(definition);
            GameObject groupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(groupPrefab != null, path + " was not created.");
            Require(
                groupPrefab.transform.childCount == definition.Count,
                definition.Name + " has an incorrect child count.");

            for (int index = 0; index < definition.Count; index++)
            {
                Transform coin = groupPrefab.transform.GetChild(index);
                Vector3 expectedPosition = GetLocalPosition(
                    index,
                    definition.Count,
                    definition.Layout);

                Require(
                    Vector3.Distance(coin.localPosition, expectedPosition) < 0.001f,
                    definition.Name + "/" + coin.name + " has an incorrect position.");
                Require(
                    coin.CompareTag("Coin"),
                    definition.Name + "/" + coin.name + " is missing the Coin tag.");
                Require(
                    coin.GetComponent<CircleCollider2D>() != null
                    && coin.GetComponent<CircleCollider2D>().isTrigger,
                    definition.Name + "/" + coin.name + " needs a trigger collider.");
                Require(
                    coin.GetComponent<SpriteRenderer>() != null
                    && coin.GetComponent<Animator>() != null,
                    definition.Name + "/" + coin.name + " lost its visuals or animation.");
                Require(
                    PrefabUtility.GetCorrespondingObjectFromSource(coin.gameObject)
                    == coinPrefab,
                    definition.Name + "/" + coin.name
                    + " must remain linked to the source Coin prefab.");
            }
        }

        Debug.Log(
            "COIN GROUP VALIDATION PASSED: eight centered nested-prefab groups are ready.");
    }

    private static void CreateGroupPrefab(
        GameObject coinPrefab,
        GroupDefinition definition)
    {
        GameObject root = new GameObject(definition.Name);

        try
        {
            for (int index = 0; index < definition.Count; index++)
            {
                GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                coin.name = "Coin_" + (index + 1);
                coin.transform.SetParent(root.transform, false);
                coin.transform.localPosition = GetLocalPosition(
                    index,
                    definition.Count,
                    definition.Layout);
                coin.transform.localRotation = Quaternion.identity;
            }

            PrefabUtility.SaveAsPrefabAsset(root, GetPrefabPath(definition));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Vector3 GetLocalPosition(
        int index,
        int count,
        GroupLayout layout)
    {
        float offset = (index - (count - 1) * 0.5f) * Spacing;
        float diagonalOffset = offset * 0.70710678f;

        switch (layout)
        {
            case GroupLayout.Vertical:
                return new Vector3(0f, offset, 0f);

            case GroupLayout.DiagonalUp:
                return new Vector3(diagonalOffset, diagonalOffset, 0f);

            case GroupLayout.DiagonalDown:
                return new Vector3(diagonalOffset, -diagonalOffset, 0f);

            default:
                return new Vector3(offset, 0f, 0f);
        }
    }

    private static string GetPrefabPath(GroupDefinition definition)
    {
        return OutputFolder + "/" + definition.Name + ".prefab";
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prelabs", "CoinGroups");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
