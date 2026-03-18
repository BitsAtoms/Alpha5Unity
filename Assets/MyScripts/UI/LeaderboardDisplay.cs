using System.Collections.Generic;
using UnityEngine;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("Referencias UI")]
    public Transform container; // El panel que tiene el Vertical Layout Group
    public GameObject rowPrefab; // El prefab "RowTemplate" que guardaste

    void Start()
    {
        // Al activarse la pantalla, dibujamos la tabla
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        // 1. Limpiar la tabla por si ya tenía filas dibujadas (para evitar duplicados visuales)
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 2. Pedir los datos al LeaderboardManager (que lee el JSON)
        if (LeaderboardManager.I == null)
        {
            Debug.LogError("Falta el LeaderboardManager en la escena.");
            return;
        }

        List<ScoreEntry> topScores = LeaderboardManager.I.GetTopScores();

        // 3. Crear una fila visual por cada récord en el JSON
        for (int i = 0; i < topScores.Count; i++)
        {
            // Instanciar el prefab dentro del contenedor
            GameObject newRow = Instantiate(rowPrefab, container);
            
            // Coger el script de la fila
            LeaderboardRowUI rowUI = newRow.GetComponent<LeaderboardRowUI>();

            if (rowUI != null)
            {
                // El ranking visual es el índice del bucle + 1 (0 -> 1º, 1 -> 2º...)
                int currentRank = i + 1;
                
                // Rellenar los datos (pasamos el ID autogenerado y la puntuación)
                rowUI.Setup(currentRank, topScores[i].id, topScores[i].score);
            }
        }
    }
}