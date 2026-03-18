using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 1. Estructura Ultra Minimalista (ID + Puntuación)
[Serializable]
public class ScoreEntry
{
    public string id;
    public int score;
}

[Serializable]
public class LeaderboardData
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager I;

    [Header("Configuración")]
    public int maxEntriesToKeep = 10; // Top 10 mejores puntuaciones únicas

    private string saveFilePath;
    private LeaderboardData currentData;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Archivo JSON donde se guardará todo
        saveFilePath = Path.Combine(Application.persistentDataPath, "Alpha5_Ranking.json");
        
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            currentData = new LeaderboardData();
        }
    }

    // 2. Método principal ultra simple (Solo recibe la puntuación)
    public void AddScore(int score)
    {
        // Regla: Si la puntuación exacta ya está en el Top, no la guardamos
        bool alreadyExists = currentData.entries.Any(e => e.score == score);

        if (alreadyExists)
        {
            Debug.Log($"[RANKING] La puntuación {score} ya existe. Ignorada.");
            return; 
        }

        // Creamos la nueva entrada con un ID único universal (GUID)
        ScoreEntry newEntry = new ScoreEntry
        {
            id = Guid.NewGuid().ToString(), // Genera un ID alfanumérico único
            score = score
        };

        currentData.entries.Add(newEntry);

        // Ordenamos estrictamente por puntuación (de mayor a menor)
        currentData.entries = currentData.entries
            .OrderByDescending(e => e.score)
            .ToList();

        // Si nos pasamos del límite (ej. 10), borramos las puntuaciones más bajas
        if (currentData.entries.Count > maxEntriesToKeep)
        {
            currentData.entries.RemoveRange(maxEntriesToKeep, currentData.entries.Count - maxEntriesToKeep);
        }

        SaveLeaderboard();
    }

    private void SaveLeaderboard()
    {
        // Guardamos en disco formateado para que sea fácil de leer por humanos
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[RANKING] Récord guardado en: {saveFilePath}");
    }

    public List<ScoreEntry> GetTopScores()
    {
        return currentData.entries;
    }
}