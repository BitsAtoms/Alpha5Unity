using UnityEngine;
using TMPro; // Cambia a UnityEngine.UI si usas Text normal

public class LeaderboardRowUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text scoreText;

    // Esta función la llamaremos desde el código principal para rellenar los datos
    public void Setup(int rank, string id, int score)
    {
        if (rankText) rankText.text = $"{rank}º";
        if (scoreText) scoreText.text = $"{score} Pts";
    }
}