using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings I;

    public GameMode Mode = GameMode.Normal;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMode(GameMode mode)
    {
        Mode = mode;
        Debug.Log("[GameSettings] Mode = " + Mode);
    }
}
