using UnityEngine;

/// <summary>
/// Inspector-configurable score given once when this enemy is actually defeated.
/// Add it to any enemy or boss prefab instead of teaching PlayerController about names.
/// </summary>
public sealed class ScoreReward : MonoBehaviour
{
    [SerializeField, Min(0)] private int scoreValue = 200;

    private bool awarded;

    public int ScoreValue => scoreValue;

    public bool TryAwardDefeat()
    {
        if (awarded)
        {
            return false;
        }

        awarded = true;
        ScoreManager.Instance.AddEnemyDefeat(scoreValue);
        return true;
    }
}
