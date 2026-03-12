using UnityEngine;

public class StarfieldDrift : MonoBehaviour
{
    public float amplitudeX = 0.15f;
    public float amplitudeY = 0.1f;
    public float frequencyX = 0.12f;
    public float frequencyY = 0.18f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offsetX = Mathf.Sin(Time.time * frequencyX * Mathf.PI * 2f) * amplitudeX;
        float offsetY = Mathf.Cos(Time.time * frequencyY * Mathf.PI * 2f) * amplitudeY;

        transform.position = startPos + new Vector3(offsetX, offsetY, 0f);
    }
}