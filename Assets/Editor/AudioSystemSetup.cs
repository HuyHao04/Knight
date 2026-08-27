using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only setup for the persistent AudioManager used from MainMenu onward.
/// </summary>
public static class AudioSystemSetup
{
    private const string PrefabPath = "Assets/Prelabs/AudioManager.prefab";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Audio/Setup Persistent Audio Manager")]
    public static void Setup()
    {
        GameObject prefab = CreateOrReplacePrefab();

        Scene mainMenu = EditorSceneManager.OpenScene(
            MainMenuScenePath,
            OpenSceneMode.Single
        );
        AudioManager existingManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>(
            FindObjectsInactive.Include
        );

        if (existingManager == null)
        {
            PrefabUtility.InstantiatePrefab(prefab, mainMenu);
            EditorSceneManager.MarkSceneDirty(mainMenu);
            EditorSceneManager.SaveScene(mainMenu);
            Debug.Log("AudioManager added to MainMenu.");
        }
        else
        {
            Debug.Log("MainMenu already has an AudioManager.");
        }

        Validate(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Audio/Validate Persistent Audio Manager")]
    public static void ValidateMenuItem()
    {
        Validate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }

    private static GameObject CreateOrReplacePrefab()
    {
        AudioClip bgm = LoadClip("262403ffb70ec884f87b82a3374840be");
        AudioClip gameOver = LoadClip("475c6e298918e4a408aff0092e10ac93");
        AudioClip victory = LoadClip("a3867b1a95fbc5e40a41f6a6ec1031c6");
        AudioClip coin = LoadClip("2993bbdb7b30ade449ee26e10b48a680");
        AudioClip jump = LoadClip("b9f43971c70070f43a17ac586eb219c4");
        AudioClip attack = LoadClip("ea1bf9b1d32499d45a6d3550fdb6b210");
        AudioClip hitEnemy = LoadClip("11ca143d39afcda44ab2cc5c378b93f3");
        AudioClip hurt = LoadClip("ab809073af55608499a48c03c5188c7b");

        GameObject root = new GameObject("AudioManager");
        AudioManager manager = root.AddComponent<AudioManager>();

        AudioSource music = CreateSource(root.transform, "Music", true);
        AudioSource sfx = CreateSource(root.transform, "SFX", false);

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("musicSource").objectReferenceValue = music;
        serializedManager.FindProperty("sfxSource").objectReferenceValue = sfx;
        serializedManager.FindProperty("bgmClip").objectReferenceValue = bgm;
        serializedManager.FindProperty("gameOverClip").objectReferenceValue = gameOver;
        serializedManager.FindProperty("victoryClip").objectReferenceValue = victory;
        serializedManager.FindProperty("coinClip").objectReferenceValue = coin;
        serializedManager.FindProperty("jumpClip").objectReferenceValue = jump;
        serializedManager.FindProperty("attackClip").objectReferenceValue = attack;
        serializedManager.FindProperty("hitEnemyClip").objectReferenceValue = hitEnemy;
        serializedManager.FindProperty("hurtClip").objectReferenceValue = hurt;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static AudioSource CreateSource(
        Transform parent,
        string sourceName,
        bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(parent);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private static AudioClip LoadClip(string guid)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            throw new InvalidOperationException(
                "Required audio clip could not be loaded: " + guid
            );
        }

        return clip;
    }

    private static void Validate(GameObject prefab)
    {
        if (prefab == null)
        {
            throw new InvalidOperationException("AudioManager prefab is missing.");
        }

        AudioManager manager = prefab.GetComponent<AudioManager>();
        AudioSource[] sources = prefab.GetComponentsInChildren<AudioSource>(true);
        if (manager == null || sources.Length != 2)
        {
            throw new InvalidOperationException(
                "AudioManager prefab must contain one manager and two audio sources."
            );
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        if (
            serializedManager.FindProperty("musicSource").objectReferenceValue == null ||
            serializedManager.FindProperty("sfxSource").objectReferenceValue == null ||
            serializedManager.FindProperty("bgmClip").objectReferenceValue == null
        )
        {
            throw new InvalidOperationException(
                "AudioManager prefab has missing Music, SFX, or BGM references."
            );
        }

        Debug.Log("Persistent AudioManager validation succeeded.");
    }
}
