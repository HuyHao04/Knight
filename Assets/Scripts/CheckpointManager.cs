using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stores the single respawn position for the current scene only.
/// It is created on demand and is never persisted between scene loads.
/// </summary>
public sealed class CheckpointManager : MonoBehaviour
{
    private static CheckpointManager instance;
    private static bool hasPendingSceneReloadRespawn;
    private static string pendingRespawnScene;
    private static Vector3 pendingRespawnPosition;

    private Vector3 defaultSpawnPosition;
    private Vector3 currentRespawnPosition;
    private bool hasRegisteredPlayer;

    public static CheckpointManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<CheckpointManager>();
            if (instance == null)
            {
                GameObject managerObject = new GameObject("CheckpointManager");
                instance = managerObject.AddComponent<CheckpointManager>();
            }

            return instance;
        }
    }

    public Vector3 RespawnPosition => currentRespawnPosition;
    public bool PlayerSpawnRestoredFromReload { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
        hasPendingSceneReloadRespawn = false;
        pendingRespawnScene = string.Empty;
        pendingRespawnPosition = Vector3.zero;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public Vector3 RegisterPlayerSpawn(Vector3 playerStartPosition)
    {
        if (hasRegisteredPlayer)
        {
            return currentRespawnPosition;
        }

        hasRegisteredPlayer = true;
        defaultSpawnPosition = playerStartPosition;
        PlayerSpawnRestoredFromReload = hasPendingSceneReloadRespawn
            && pendingRespawnScene == SceneManager.GetActiveScene().name;

        currentRespawnPosition = PlayerSpawnRestoredFromReload
            ? pendingRespawnPosition
            : defaultSpawnPosition;

        if (hasPendingSceneReloadRespawn)
        {
            hasPendingSceneReloadRespawn = false;
            pendingRespawnScene = string.Empty;
            pendingRespawnPosition = Vector3.zero;
        }

        return currentRespawnPosition;
    }

    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        currentRespawnPosition = checkpointPosition;
    }

    public static void PrepareSceneReloadRespawn(string sceneName, Vector3 respawnPosition)
    {
        hasPendingSceneReloadRespawn = true;
        pendingRespawnScene = sceneName;
        pendingRespawnPosition = respawnPosition;
    }
}
