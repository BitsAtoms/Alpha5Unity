using UnityEngine;

public class BallController : MonoBehaviour
{
    public bool TouchedKeeper { get; private set; } = false;

    public void ResetFlags() => TouchedKeeper = false;

    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Keeper"))
        {
            TouchedKeeper = true;
            Debug.Log("[Ball] Tocó al Keeper");
        }
    }
}
