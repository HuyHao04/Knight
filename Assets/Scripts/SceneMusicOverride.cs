using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneMusicOverride : MonoBehaviour
{
    [Header("Scene Music Override")]
    [Tooltip("Leave empty to keep using the shared level BGM.")]
    [SerializeField] private AudioClip musicClip;

    [Tooltip("Restart this clip from the beginning whenever the scene is loaded.")]
    [SerializeField] private bool restartOnSceneLoad = true;

    public AudioClip MusicClip => musicClip;
    public bool RestartOnSceneLoad => restartOnSceneLoad;
}
