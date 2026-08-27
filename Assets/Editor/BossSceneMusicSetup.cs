using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossSceneMusicSetup
{
    private const string ScenePath = "Assets/Scenes/Boss.unity";
    private const string ObjectName = "BossMusic";

    [MenuItem("Tools/Boss/Configure Boss Scene Music")]
    public static void ConfigureBossMusic()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SceneMusicOverride musicOverride = FindBossMusicOverride();

        if (musicOverride == null)
        {
            GameObject musicObject = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(musicObject, "Create Boss Music Override");
            musicOverride = musicObject.AddComponent<SceneMusicOverride>();
        }

        SerializedObject serializedOverride = new SerializedObject(musicOverride);
        serializedOverride.FindProperty("restartOnSceneLoad").boolValue = true;
        serializedOverride.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(musicOverride);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "BOSS SCENE MUSIC READY: assign an AudioClip to BossMusic > Scene Music Override."
        );
    }

    public static void ValidateBossMusic()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SceneMusicOverride musicOverride = FindBossMusicOverride();

        Require(musicOverride != null, "BossMusic SceneMusicOverride is missing.");
        Require(musicOverride.gameObject.name == ObjectName,
            "SceneMusicOverride must be on the standalone BossMusic object.");
        Require(musicOverride.transform.parent == null,
            "BossMusic must remain independent from the disposable scene AudioManager.");
        Require(musicOverride.RestartOnSceneLoad,
            "Boss music must restart when the Boss scene is loaded.");

        string clipStatus = musicOverride.MusicClip != null
            ? musicOverride.MusicClip.name
            : "unassigned (shared BGM fallback active)";
        Debug.Log("BOSS SCENE MUSIC VALIDATION PASSED: clip=" + clipStatus + ".");
    }

    private static SceneMusicOverride FindBossMusicOverride()
    {
        return UnityEngine.Object.FindObjectsByType<SceneMusicOverride>(
                FindObjectsInactive.Include)
            .FirstOrDefault(component => component.gameObject.name == ObjectName);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
