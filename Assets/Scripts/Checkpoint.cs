using UnityEngine;

/// <summary>
/// Place on a trigger collider to make that location the current scene respawn point.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

    private void Reset()
    {
        Collider2D checkpointCollider = GetComponent<Collider2D>();
        checkpointCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        CheckpointManager.Instance.SetCheckpoint(RespawnPosition);
    }
}
