using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level3FeatureSyncSetup
{
    private const string MenuPath = "Tools/Levels/Sync Level 3 Features From Level 1";
    private const string Level1Path = "Assets/Scenes/Level_1.unity";
    private const string Level3Path = "Assets/Scenes/Level_3.unity";
    private const string AudioPrefabPath = "Assets/Prelabs/AudioManager.prefab";

    private sealed class AudioReferences
    {
        public AudioClip Bgm;
        public AudioClip GameOver;
        public AudioClip Victory;
        public AudioClip Coin;
        public AudioClip Jump;
        public AudioClip Attack;
        public AudioClip HitEnemy;
        public AudioClip Hurt;
        public AudioClip Portal;
    }

    [MenuItem(MenuPath)]
    public static void SyncLevel3Features()
    {
        AudioReferences audio = ReadLevel1AudioReferences();

        Scene level3 = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        ConfigureAudio(level3, audio);
        EditorSceneManager.SaveScene(level3);

        LevelCountdownTimerSetup.ConfigureScene(Level3Path);
        PortalTransitionSetup.ConfigurePortalPrefab();

        level3 = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        RemoveLegacyGateTriggers(level3);
        PortalTransitionSetup.ConfigureScene(level3, "Boss", 1f, true);
        EnableLevel3InBuildSettings();
        EditorSceneManager.MarkSceneDirty(level3);
        EditorSceneManager.SaveScene(level3);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateLevel3(audio);

        Debug.Log(
            "LEVEL 3 FEATURE SYNC COMPLETE: Level 1 audio, 300-second timer, "
            + "interactive Boss portal, right-facing entrance portal and screen fade are active.");
    }

    public static void ValidateLevel3Menu()
    {
        ValidateLevel3(ReadLevel1AudioReferences());
    }

    private static AudioReferences ReadLevel1AudioReferences()
    {
        EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
        AudioManager source = UnityEngine.Object.FindAnyObjectByType<AudioManager>(
            FindObjectsInactive.Include);
        Require(source != null, "Level_1 is missing AudioManager.");

        SerializedObject data = new SerializedObject(source);
        return new AudioReferences
        {
            Bgm = ReadClip(data, "bgmClip"),
            GameOver = ReadClip(data, "gameOverClip"),
            Victory = ReadClip(data, "victoryClip"),
            Coin = ReadClip(data, "coinClip"),
            Jump = ReadClip(data, "jumpClip"),
            Attack = ReadClip(data, "attackClip"),
            HitEnemy = ReadClip(data, "hitEnemyClip"),
            Hurt = ReadClip(data, "hurtClip"),
            Portal = ReadClip(data, "portalClip", false)
        };
    }

    private static AudioClip ReadClip(
        SerializedObject source,
        string propertyName,
        bool required = true)
    {
        AudioClip clip = source.FindProperty(propertyName).objectReferenceValue as AudioClip;
        if (required)
        {
            Require(clip != null, "Level_1 AudioManager is missing " + propertyName + ".");
        }

        return clip;
    }

    private static void ConfigureAudio(Scene scene, AudioReferences audio)
    {
        AudioManager[] managers = UnityEngine.Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include);
        AudioManager manager = managers.FirstOrDefault();

        if (manager == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefabPath);
            Require(prefab != null, "AudioManager prefab is missing.");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            manager = instance.GetComponent<AudioManager>();
        }

        foreach (AudioManager duplicate in managers.Skip(1))
        {
            UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
        }

        manager.gameObject.name = "AudioManager";
        AudioSource music = FindOrCreateSource(manager.transform, "Music", true);
        AudioSource sfx = FindOrCreateSource(manager.transform, "SFX", false);

        SerializedObject data = new SerializedObject(manager);
        data.FindProperty("musicSource").objectReferenceValue = music;
        data.FindProperty("sfxSource").objectReferenceValue = sfx;
        data.FindProperty("bgmClip").objectReferenceValue = audio.Bgm;
        data.FindProperty("gameOverClip").objectReferenceValue = audio.GameOver;
        data.FindProperty("victoryClip").objectReferenceValue = audio.Victory;
        data.FindProperty("coinClip").objectReferenceValue = audio.Coin;
        data.FindProperty("jumpClip").objectReferenceValue = audio.Jump;
        data.FindProperty("attackClip").objectReferenceValue = audio.Attack;
        data.FindProperty("hitEnemyClip").objectReferenceValue = audio.HitEnemy;
        data.FindProperty("hurtClip").objectReferenceValue = audio.Hurt;
        data.FindProperty("portalClip").objectReferenceValue = audio.Portal;
        data.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(music);
        EditorUtility.SetDirty(sfx);
        EditorUtility.SetDirty(manager);
    }

    private static AudioSource FindOrCreateSource(
        Transform parent,
        string sourceName,
        bool loop)
    {
        Transform sourceTransform = parent.Find(sourceName);
        GameObject sourceObject;

        if (sourceTransform == null)
        {
            sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(parent, false);
        }
        else
        {
            sourceObject = sourceTransform.gameObject;
        }

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private static void RemoveLegacyGateTriggers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == "Gate"
                    && candidate.GetComponent<Portal>() == null
                    && candidate.CompareTag("gate"))
                {
                    UnityEngine.Object.DestroyImmediate(candidate.gameObject);
                }
            }
        }
    }

    private static void EnableLevel3InBuildSettings()
    {
        GUID level3Guid = AssetDatabase.GUIDFromAssetPath(Level3Path);
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        int existingIndex = Array.FindIndex(scenes, entry => entry.path == Level3Path);

        if (existingIndex >= 0)
        {
            scenes[existingIndex] = new EditorBuildSettingsScene(level3Guid, true);
        }
        else
        {
            Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(level3Guid, true);
        }

        EditorBuildSettings.scenes = scenes;
    }

    private static void ValidateLevel3(AudioReferences audio)
    {
        Scene scene = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        Require(scene.GetRootGameObjects().Any(root => root.name == "Character"),
            "Level_3 map hierarchy was unexpectedly changed.");

        AudioManager[] managers = UnityEngine.Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include);
        Require(managers.Length == 1, "Level_3 must contain exactly one AudioManager.");
        SerializedObject audioData = new SerializedObject(managers[0]);
        Require(audioData.FindProperty("musicSource").objectReferenceValue != null,
            "Level_3 AudioManager is missing Music source.");
        Require(audioData.FindProperty("sfxSource").objectReferenceValue != null,
            "Level_3 AudioManager is missing SFX source.");
        Require(audioData.FindProperty("bgmClip").objectReferenceValue == audio.Bgm,
            "Level_3 BGM is not synchronized with Level_1.");
        Require(audioData.FindProperty("gameOverClip").objectReferenceValue == audio.GameOver,
            "Level_3 Game Over audio is not synchronized with Level_1.");
        Require(audioData.FindProperty("victoryClip").objectReferenceValue == audio.Victory,
            "Level_3 Victory audio is not synchronized with Level_1.");

        LevelCountdownTimer[] timers = UnityEngine.Object.FindObjectsByType<LevelCountdownTimer>(
            FindObjectsInactive.Include);
        Require(timers.Length == 1, "Level_3 must contain exactly one countdown timer.");
        SerializedObject timerData = new SerializedObject(timers[0]);
        Require(Mathf.Approximately(timerData.FindProperty("durationSeconds").floatValue, 300f),
            "Level_3 timer must start at 300 seconds.");
        Require(timerData.FindProperty("player").objectReferenceValue != null,
            "Level_3 timer is missing PlayerController reference.");
        Require(timerData.FindProperty("timerText").objectReferenceValue is TextMeshProUGUI,
            "Level_3 timer is missing its HUD text.");

        Portal[] exits = UnityEngine.Object.FindObjectsByType<Portal>(FindObjectsInactive.Include)
            .Where(portal => portal.enabled && portal.GetComponent<PortalArrival>() == null)
            .ToArray();
        Require(exits.Length == 1, "Level_3 must contain exactly one interactive exit Portal.");
        SerializedObject portalData = new SerializedObject(exits[0]);
        Require(portalData.FindProperty("destinationScene").stringValue == "Boss",
            "Level_3 exit Portal must lead to Boss.");
        Require(portalData.FindProperty("promptUI").objectReferenceValue != null,
            "Level_3 exit Portal is not linked to PortalPromptUI.");

        PortalArrival[] arrivals = UnityEngine.Object.FindObjectsByType<PortalArrival>(
            FindObjectsInactive.Include);
        Require(arrivals.Length == 1, "Level_3 must contain exactly one EntrancePortal.");
        SerializedObject arrivalData = new SerializedObject(arrivals[0]);
        Transform exitPoint = arrivalData.FindProperty("exitPoint").objectReferenceValue as Transform;
        Require(exitPoint != null && exitPoint.position.x > arrivals[0].transform.position.x,
            "Level_3 EntrancePortal must let the player walk out to the right.");

        SceneTransitionManager transition = UnityEngine.Object.FindAnyObjectByType<SceneTransitionManager>(
            FindObjectsInactive.Include);
        Require(transition != null, "Level_3 is missing SceneTransitionManager.");
        SerializedObject transitionData = new SerializedObject(transition);
        Require(transitionData.FindProperty("fadeGroup").objectReferenceValue != null,
            "Level_3 transition fade is not configured.");

        Require(!scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Any(candidate => candidate.name == "Gate"
                    && candidate.GetComponent<Portal>() == null
                    && candidate.CompareTag("gate")),
            "Level_3 still contains a legacy Gate trigger.");

        GUID level3Guid = AssetDatabase.GUIDFromAssetPath(Level3Path);
        Require(EditorBuildSettings.scenes.Any(entry => entry.path == Level3Path
            && entry.guid == level3Guid
            && entry.enabled),
            "Level_3 is not correctly enabled in Build Settings.");

        Debug.Log("LEVEL 3 FEATURE VALIDATION PASSED.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
