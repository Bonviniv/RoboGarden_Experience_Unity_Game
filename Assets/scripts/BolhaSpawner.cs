using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Gera bolhas que flutuam dentro de um cilindro invisível
// Controla o efeito visual e o Bloom pós-processamento
public class BolhaSpawner : MonoBehaviour
{
    // Configuração do cilindro onde as bolhas são geradas
    [Header("Configuração do Cilindro")]
    public float cylinderRadius = 0.3f;
    public float cylinderHeight = 1.5f;

    // Configuração das bolhas
    [Header("Bolhas")]
    public GameObject bolhaPrefab;
    public int bolhaPoolSize = 20;
    public float bolhaSpeed = 0.5f;
    public Vector2 bolhaScaleRange = new Vector2(0.05f, 0.15f);
    public float spawnInterval = 0.4f; // ← intervalo entre bolhas em segundos

    // Efeito Bloom para realçar as bolhas
    [Header("Efeito Bloom")]
    public float bloomIntensity = 0.7f;
    public Color bloomTint = Color.white;

    private List<GameObject> bolhaPool;
    private float spawnTimer = 0f;

    void Start()
    {
        // Criar pool de bolhas
        bolhaPool = new List<GameObject>();
        for (int i = 0; i < bolhaPoolSize; i++)
        {
            GameObject b = Instantiate(bolhaPrefab, transform);
            b.SetActive(false);
            bolhaPool.Add(b);
        }

        SetupBloom();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        // Tenta spawnar bolha apenas se tempo suficiente passou
        if (spawnTimer >= spawnInterval)
        {
            if (TrySpawnBolha())
            {
                spawnTimer = 0f;
            }
        }

        // Mover bolhas ativas
        foreach (GameObject bolha in bolhaPool)
        {
            if (bolha.activeSelf)
            {
                bolha.transform.position += Vector3.up * bolhaSpeed * Time.deltaTime;

                if (bolha.transform.localPosition.y > cylinderHeight)
                    bolha.SetActive(false);
            }
        }
    }

    bool TrySpawnBolha()
    {
        foreach (GameObject bolha in bolhaPool)
        {
            if (!bolha.activeSelf)
            {
                Vector2 randomCircle = Random.insideUnitCircle * cylinderRadius;
                Vector3 spawnPos = new Vector3(randomCircle.x, 0f, randomCircle.y);

                bolha.transform.localPosition = spawnPos;
                bolha.transform.localScale = Vector3.one * Random.Range(bolhaScaleRange.x, bolhaScaleRange.y);
                bolha.SetActive(true);
                return true;
            }
        }
        return false;
    }

    void SetupBloom()
    {
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
