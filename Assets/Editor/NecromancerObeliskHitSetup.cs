using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NecromancerObeliskHitSetup
{
    private const string SpriteSheetPath =
        "Assets/Sprites/Enemy/Necromancer/Necromancer_creativekind-Sheet.png";
    private const string ControllerPath = "Assets/Animation/Necromancer.controller";
    private const string IdleStateName = "NercomancerIdle";

    private const string HitSpriteName = "Necromancer_creativekind-Sheet_100";
    private const string HitClipPath = "Assets/Animation/NecromancerObeliskHit.anim";
    private const string HitStateName = "NecromancerObeliskHit";
    private const string HitParameterName = "IsObeliskHit";

    private static readonly string[] FallStunSpriteNames =
    {
        "Necromancer_creativekind-Sheet_113",
        "Necromancer_creativekind-Sheet_115",
        "Necromancer_creativekind-Sheet_117"
    };
    private const string FallStunClipPath = "Assets/Animation/NecromancerObeliskFallStun.anim";
    private const string FallStunStateName = "NecromancerObeliskFallStun";
    private const string FallStunParameterName = "IsObeliskFallStun";

    [MenuItem("Tools/Boss/Configure Necromancer Obelisk Reactions")]
    public static void ConfigureObeliskHit()
    {
        AnimationClip hitClip = CreateOrUpdateClip(
            HitClipPath,
            new[] { LoadSprite(HitSpriteName) },
            12f);
        AnimationClip fallStunClip = CreateOrUpdateClip(
            FallStunClipPath,
            FallStunSpriteNames.Select(LoadSprite).ToArray(),
            10f);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            ControllerPath);

        if (controller == null)
        {
            throw new InvalidOperationException("Necromancer Animator Controller was not found.");
        }

        EnsureBoolParameter(controller, HitParameterName);
        EnsureBoolParameter(controller, FallStunParameterName);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = FindState(stateMachine, IdleStateName);
        if (idleState == null)
        {
            throw new InvalidOperationException("Necromancer idle state was not found.");
        }

        AnimatorState hitState = GetOrCreateState(
            stateMachine,
            HitStateName,
            new Vector3(600f, 150f, 0f));
        hitState.motion = hitClip;

        AnimatorState fallStunState = GetOrCreateState(
            stateMachine,
            FallStunStateName,
            new Vector3(600f, 240f, 0f));
        fallStunState.motion = fallStunClip;

        EnsureAnyStateBoolTransition(stateMachine, hitState, HitParameterName, 0.03f);
        EnsureAnyStateBoolTransition(stateMachine, fallStunState, FallStunParameterName, 0.03f);

        AnimatorStateTransition hitToFallStun = FindTransition(
            hitState,
            fallStunState,
            FallStunParameterName);
        if (hitToFallStun == null)
        {
            hitToFallStun = hitState.AddTransition(fallStunState);
            ConfigureImmediateTransition(hitToFallStun, 0.03f);
            hitToFallStun.AddCondition(AnimatorConditionMode.If, 0f, FallStunParameterName);
        }

        AnimatorStateTransition hitToIdle = hitState.transitions.FirstOrDefault(transition =>
            transition.destinationState == idleState
            && transition.conditions.Any(condition =>
                condition.parameter == HitParameterName
                && condition.mode == AnimatorConditionMode.IfNot));
        if (hitToIdle == null)
        {
            hitToIdle = hitState.AddTransition(idleState);
            ConfigureImmediateTransition(hitToIdle, 0.05f);
            hitToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, HitParameterName);
        }

        if (!hitToIdle.conditions.Any(condition =>
                condition.parameter == FallStunParameterName
                && condition.mode == AnimatorConditionMode.IfNot))
        {
            hitToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, FallStunParameterName);
        }

        AnimatorStateTransition fallStunToIdle = FindTransition(
            fallStunState,
            idleState,
            FallStunParameterName);
        if (fallStunToIdle == null)
        {
            fallStunToIdle = fallStunState.AddTransition(idleState);
            ConfigureImmediateTransition(fallStunToIdle, 0.05f);
            fallStunToIdle.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                FallStunParameterName);
        }

        EditorUtility.SetDirty(hitState);
        EditorUtility.SetDirty(fallStunState);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Necromancer Obelisk reactions configured: hit 100, fall/stun loop 113-115-117.");
    }

    public static void ValidateObeliskHit()
    {
        AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipPath);
        AnimationClip fallStunClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            FallStunClipPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            ControllerPath);

        Require(controller != null, "Necromancer Animator Controller is missing.");
        RequireBoolParameter(controller, HitParameterName);
        RequireBoolParameter(controller, FallStunParameterName);
        ValidateSpriteClip(hitClip, new[] { HitSpriteName }, "Obelisk Hit");
        ValidateSpriteClip(fallStunClip, FallStunSpriteNames, "Obelisk Fall/Stun");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = FindState(stateMachine, IdleStateName);
        AnimatorState hitState = FindState(stateMachine, HitStateName);
        AnimatorState fallStunState = FindState(stateMachine, FallStunStateName);

        Require(hitState != null && hitState.motion == hitClip,
            "Obelisk Hit state is not wired to sprite 100 clip.");
        Require(fallStunState != null && fallStunState.motion == fallStunClip,
            "Obelisk Fall/Stun state is not wired to the 113-115-117 clip.");
        Require(idleState != null, "Necromancer idle state is missing.");

        Require(HasAnyStateBoolTransition(stateMachine, hitState, HitParameterName),
            "Obelisk Hit enter transition is missing.");
        Require(HasAnyStateBoolTransition(stateMachine, fallStunState, FallStunParameterName),
            "Obelisk Fall/Stun enter transition is missing.");
        Require(FindTransition(hitState, fallStunState, FallStunParameterName) != null,
            "Direct transition from sprite 100 to the fall/stun loop is missing.");
        Require(FindTransition(fallStunState, idleState, FallStunParameterName) != null,
            "Transition from the fall/stun loop to Idle is missing.");

        Debug.Log(
            "NECROMANCER OBELISK REACTION VALIDATION PASSED: beam hit 100, "
            + "fall/stun loop 113-115-117.");
    }

    private static AnimationClip CreateOrUpdateClip(
        string clipPath,
        Sprite[] sprites,
        float frameRate)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = frameRate;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(
            clip,
            binding,
            sprites.Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = sprite
            }).ToArray());

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

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        return stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(state => state.name == stateName);
    }

    private static AnimatorState GetOrCreateState(
        AnimatorStateMachine stateMachine,
        string stateName,
        Vector3 position)
    {
        return FindState(stateMachine, stateName) ?? stateMachine.AddState(stateName, position);
    }

    private static void EnsureBoolParameter(AnimatorController controller, string parameterName)
    {
        if (!controller.parameters.Any(parameter => parameter.name == parameterName))
        {
            controller.AddParameter(parameterName, AnimatorControllerParameterType.Bool);
        }
    }

    private static void RequireBoolParameter(
        AnimatorController controller,
        string parameterName)
    {
        Require(controller.parameters.Any(parameter =>
                parameter.name == parameterName
                && parameter.type == AnimatorControllerParameterType.Bool),
            parameterName + " Bool parameter is missing.");
    }

    private static void EnsureAnyStateBoolTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destinationState,
        string parameterName,
        float duration)
    {
        if (HasAnyStateBoolTransition(stateMachine, destinationState, parameterName))
        {
            return;
        }

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destinationState);
        ConfigureImmediateTransition(transition, duration);
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
    }

    private static bool HasAnyStateBoolTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destinationState,
        string parameterName)
    {
        return stateMachine.anyStateTransitions.Any(transition =>
            transition.destinationState == destinationState
            && transition.conditions.Any(condition =>
                condition.parameter == parameterName
                && condition.mode == AnimatorConditionMode.If));
    }

    private static AnimatorStateTransition FindTransition(
        AnimatorState sourceState,
        AnimatorState destinationState,
        string parameterName)
    {
        return sourceState.transitions.FirstOrDefault(transition =>
            transition.destinationState == destinationState
            && transition.conditions.Any(condition => condition.parameter == parameterName));
    }

    private static void ConfigureImmediateTransition(
        AnimatorStateTransition transition,
        float duration)
    {
        transition.hasExitTime = false;
        transition.duration = duration;
    }

    private static void ValidateSpriteClip(
        AnimationClip clip,
        string[] spriteNames,
        string label)
    {
        Require(clip != null, label + " clip is missing.");
        EditorCurveBinding spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(binding => binding.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(
            clip,
            spriteBinding);
        Require(keys != null && keys.Length == spriteNames.Length,
            label + " clip has the wrong sprite frame count.");

        string[] actualNames = keys
            .Select(key => key.value != null ? key.value.name : string.Empty)
            .ToArray();
        Require(actualNames.SequenceEqual(spriteNames),
            label + " clip uses the wrong sprite sequence.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
