using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NecromancerLightningCastSetup
{
    private const string SpriteSheetPath =
        "Assets/Sprites/Enemy/Necromancer/Necromancer_creativekind-Sheet.png";
    private const string ClipPath = "Assets/Animation/NecromancerLightningCast.anim";
    private const string ControllerPath = "Assets/Animation/Necromancer.controller";
    private const string StateName = "NecromancerLightningCast";
    private const string ParameterName = "IsCastingLightning";

    private static readonly int[] FrameNumbers = { 31, 33, 35, 37, 39, 41 };

    [MenuItem("Tools/Boss/Configure Necromancer Lightning Cast")]
    public static void ConfigureLightningCast()
    {
        Sprite[] frames = FrameNumbers
            .Select(number => LoadSprite("Necromancer_creativekind-Sheet_" + number))
            .ToArray();

        AnimationClip clip = CreateOrUpdateClip(frames);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            ControllerPath);

        if (controller == null)
        {
            throw new InvalidOperationException("Necromancer Animator Controller was not found.");
        }

        if (!controller.parameters.Any(parameter => parameter.name == ParameterName))
        {
            controller.AddParameter(ParameterName, AnimatorControllerParameterType.Bool);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState lightningState = stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(state => state.name == StateName);

        if (lightningState == null)
        {
            lightningState = stateMachine.AddState(StateName, new Vector3(600f, 60f, 0f));
        }

        lightningState.motion = clip;

        bool hasEnterTransition = stateMachine.anyStateTransitions.Any(transition =>
            transition.destinationState == lightningState
            && transition.conditions.Any(condition => condition.parameter == ParameterName));
        if (!hasEnterTransition)
        {
            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(lightningState);
            enter.hasExitTime = false;
            enter.duration = 0.05f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, ParameterName);
        }

        AnimatorState idleState = stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(state => state.name == "NercomancerIdle");
        if (idleState == null)
        {
            throw new InvalidOperationException("Necromancer idle state was not found.");
        }

        bool hasExitTransition = lightningState.transitions.Any(transition =>
            transition.destinationState == idleState
            && transition.conditions.Any(condition => condition.parameter == ParameterName));
        if (!hasExitTransition)
        {
            AnimatorStateTransition exit = lightningState.AddTransition(idleState);
            exit.hasExitTime = false;
            exit.duration = 0.05f;
            exit.AddCondition(AnimatorConditionMode.IfNot, 0f, ParameterName);
        }

        EditorUtility.SetDirty(lightningState);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Necromancer Lightning Cast configured with frames: "
            + string.Join(", ", FrameNumbers));
    }

    public static void ValidateLightningCast()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            ControllerPath);

        Require(clip != null, "Necromancer Lightning Cast clip is missing.");
        Require(controller != null, "Necromancer Animator Controller is missing.");
        Require(
            controller.parameters.Any(parameter =>
                parameter.name == ParameterName
                && parameter.type == AnimatorControllerParameterType.Bool),
            "IsCastingLightning Bool parameter is missing.");

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(animatorState => animatorState.name == StateName);
        Require(state != null && state.motion == clip, "Lightning Cast state is not wired to its clip.");

        EditorCurveBinding spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(binding => binding.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
        Require(keys != null && keys.Length == 6, "Lightning Cast clip must contain six sprite frames.");

        string[] actualFrames = keys
            .Select(key => key.value != null ? key.value.name : string.Empty)
            .ToArray();
        string[] expectedFrames = FrameNumbers
            .Select(number => "Necromancer_creativekind-Sheet_" + number)
            .ToArray();
        Require(actualFrames.SequenceEqual(expectedFrames), "Lightning Cast frames are in the wrong order.");

        Debug.Log("NECROMANCER LIGHTNING CAST VALIDATION PASSED: 31, 33, 35, 37, 39, 41.");
    }

    private static AnimationClip CreateOrUpdateClip(Sprite[] frames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        clip.frameRate = 12f;
        ObjectReferenceKeyframe[] keys = frames
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        SerializedObject clipData = new SerializedObject(clip);
        SerializedProperty settings = clipData.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        if (loopTime != null)
        {
            loopTime.boolValue = true;
        }

        clipData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .FirstOrDefault(candidate => candidate.name == spriteName);
        if (sprite == null)
        {
            throw new InvalidOperationException("Missing Necromancer sprite: " + spriteName);
        }

        return sprite;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
