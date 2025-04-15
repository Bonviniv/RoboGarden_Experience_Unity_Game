using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DNAFloatAndRotate : MonoBehaviour
{
    [Header("Rotação")]
    public float rotationSpeed = 30f;

    [Header("Flutuação Senoidal")]
    public float floatAmplitude = 1f;
    public float floatFrequency = 0.5f;

    [Header("Efeito Bloom")]
    public float bloomIntensity = 1f;
    public Color bloomTint = Color.cyan;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        SetupBloom();
    }

    void Update()
    {
        // Rotação contínua
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Flutuação vertical
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void SetupBloom()
    {
        // Procurar ou criar volume global
        Volume volume = FindAnyObjectByType<Volume>();

        if (volume == null)
        {
            GameObject volumeObj = new GameObject("PostProcessVolume_Global");
            volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
        }

        if (volume.profile == null)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        Bloom bloom;
        if (!volume.profile.TryGet(out bloom))
        {
            bloom = volume.profile.Add<Bloom>(true);
        }

        bloom.intensity.value = bloomIntensity;
        bloom.tint.value = bloomTint;
        bloom.threshold.value = 1f;
        bloom.scatter.value = 0.7f;
        bloom.active = true;
    }
}
