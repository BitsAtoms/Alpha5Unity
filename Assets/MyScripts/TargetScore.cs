using UnityEngine;

public class TargetScore : MonoBehaviour
{
    private bool triggeredThisRound = false;

    public bool TryActivateFromLidar()
    {
        if (triggeredThisRound) return false;
        if (GameManager.I && !GameManager.I.CanShoot()) return false;

        triggeredThisRound = true;

        if (GameManager.I)
            GameManager.I.ActivateRoundTargetMultiplier();

        Debug.Log("[TargetScore] Diana activada por LiDAR");
        return true;
    }

    public void ResetForNewRound()
    {
        triggeredThisRound = false;
    }
}