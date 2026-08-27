using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MultiPlatformBuildPipeline
{
    private const string ProductFileName = "MyProject";
    private const string WindowsOutput = "Builds/Windows/MyProject.exe";
    private const string AndroidOutput = "Builds/Android/MyProject.apk";
    private const string WebGLOutput = "Builds/WebGL";

    private static readonly string[] SceneOrder =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Level_1.unity",
        "Assets/Scenes/Level_2.unity",
        "Assets/Scenes/Level_3.unity",
        "Assets/Scenes/Boss.unity"
    };

    [MenuItem("Tools/Build/Configure Release Settings")]
    public static void ConfigureReleaseSettings()
    {
        ValidateScenes();
        EditorBuildSettings.scenes = SceneOrder
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        PlayerSettings.productName = "My project";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;

        PlayerSettings.SetApplicationIdentifier(
            UnityEditor.Build.NamedBuildTarget.Android,
            "com.defaultcompany.myproject");
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;

        AssetDatabase.SaveAssets();
        Debug.Log("RELEASE SETTINGS READY: scenes, Windows, Android APK and WebGL settings are valid.");
    }

    [MenuItem("Tools/Build/Build Windows")]
    public static void BuildWindows()
    {
        ConfigureReleaseSettings();
        Build(WindowsOutput, BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Tools/Build/Build Android APK")]
    public static void BuildAndroid()
    {
        ConfigureReleaseSettings();
        Build(AndroidOutput, BuildTarget.Android);
    }

    [MenuItem("Tools/Build/Build WebGL")]
    public static void BuildWebGL()
    {
        ConfigureReleaseSettings();
        Build(WebGLOutput, BuildTarget.WebGL);
    }

    [MenuItem("Tools/Build/Build All Platforms")]
    public static void BuildAllPlatforms()
    {
        BuildWindows();
        BuildAndroid();
        BuildWebGL();
    }

    public static void ValidateBuildConfiguration()
    {
        ConfigureReleaseSettings();
        Debug.Log("BUILD CONFIGURATION VALIDATION PASSED.");
    }

    private static void Build(string relativeOutputPath, BuildTarget target)
    {
        string absoluteOutputPath = Path.GetFullPath(relativeOutputPath);
        string outputDirectory = target == BuildTarget.WebGL
            ? absoluteOutputPath
            : Path.GetDirectoryName(absoluteOutputPath);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException("Could not resolve the build output directory.");
        }

        Directory.CreateDirectory(outputDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = SceneOrder,
            locationPathName = absoluteOutputPath,
            target = target,
            options = BuildOptions.None
        };

        Debug.Log($"BUILD STARTED: {target} -> {absoluteOutputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"{target} build failed with {summary.totalErrors} error(s) "
                + $"and {summary.totalWarnings} warning(s). See the Unity build log.");
        }

        Debug.Log(
            $"BUILD SUCCEEDED: {target} | "
            + $"Size={FormatBytes(summary.totalSize)} | "
            + $"Time={summary.totalTime} | "
            + $"Output={absoluteOutputPath}");
    }

    private static void ValidateScenes()
    {
        string[] missingScenes = SceneOrder
            .Where(path => !File.Exists(path))
            .ToArray();

        if (missingScenes.Length > 0)
        {
            throw new FileNotFoundException(
                "Build scene(s) missing: " + string.Join(", ", missingScenes));
        }

        if (!SceneOrder[0].EndsWith("MainMenu.unity", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainMenu must remain the first build scene.");
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double megabyte = 1024d * 1024d;
        return $"{bytes / megabyte:0.00} MB";
    }
}
