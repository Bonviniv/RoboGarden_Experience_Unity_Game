using UnityEngine;

public class WindSwing : MonoBehaviour
{
    public float amplitude = 5f;      // ângulo máximo (em graus)
    public float frequency = 1f;      // velocidade da oscilação
    public float offset = 0f;         // para desfasar diferentes ramos

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;

        // Aleatoriedade no offset e frequência
        offset = Random.Range(0f, 2f * Mathf.PI);
        frequency *= Random.Range(0.8f, 1.2f);
        amplitude *= Random.Range(0.8f, 1.2f);
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * frequency + offset) * amplitude;
        transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, angle);
    }
}