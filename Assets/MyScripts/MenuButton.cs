using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    public void PlayEasy()
    {
        GameSettings.I.selectedDifficulty = Difficulty.Easy;
        SceneManager.LoadScene("Campo_Facil");
    }

    public void PlayNormal()
    {
        GameSettings.I.selectedDifficulty = Difficulty.Normal;
        SceneManager.LoadScene("ProyectoFinalFinal");
    }

    public void PlayHard()
    {
        GameSettings.I.selectedDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("Campo_Dificil");
    }

    public void PlayLoco()
    {
        GameSettings.I.selectedDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("Campo_Loco");
    }
    public void PlayProgresive()
    {
        GameSettings.I.selectedDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("Campo_Progresivo");
    }
}
