using UnityEngine;
using TMPro;
using System.Collections;

public class TiempoRonda : MonoBehaviour
{
    public float tiempoMax = 10f;
    public TMP_Text texto;

    void Update()
    {
        tiempoMax -= Time.deltaTime;
        texto.text = tiempoMax.ToString("0");

        if (tiempoMax <= 0)
        {
            GameManager.I.ShotFail();
        }
    }
}
