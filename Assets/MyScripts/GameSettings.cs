using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings I;

    public Difficulty selectedDifficulty = Difficulty.Normal;

    void Awake()
    {
        if (I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}
