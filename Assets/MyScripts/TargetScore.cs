using UnityEngine;

public class TargetScore : MonoBehaviour
{
    [Header("Multiplicador de esta diana")]
    public int multiplier = 2;

    private bool triggeredThisRound = false;

    public bool TryActivateFromLidar()
    {
        if (triggeredThisRound) return false;
        if (GameManager.I && !GameManager.I.CanShoot()) return false;

        triggeredThisRound = true;

        if (GameManager.I)
            GameManager.I.SetGoalMultiplier(multiplier);

        Debug.Log($"[TargetScore] Diana activada por LiDAR -> x{multiplier}");
        return true;
    }

    public void ResetForNewRound()
    {
        triggeredThisRound = false;
    }
}