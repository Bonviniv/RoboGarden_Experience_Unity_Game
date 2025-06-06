using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantHUDManager : MonoBehaviour
{
    [Header("Referência à UI")]
    public Slider angleSlider;
    public Slider lengthSlider;
    public Slider iterationsSlider;
    public Slider flowerProbabilitySlider;
    public Toggle windToggle;

    [Header("Textos de valores dos sliders")]
    public TextMeshProUGUI angleValueText;
    public TextMeshProUGUI lengthValueText;
    public TextMeshProUGUI iterationsValueText;
    public TextMeshProUGUI flowerProbabilityValueText;

    public Button playButton;
    public Button pauseButton;
    public Button restartButton;

    [Header("Planta atual selecionada")]
    public PlantInterpreter currentPlant;

    public GameObject canvas;

    public GameObject vaso;

    private bool isPaused = false;

    void Start()
    {
        // Liga os eventos dos UI elements
        angleSlider.onValueChanged.AddListener(OnAngleChanged);
        lengthSlider.onValueChanged.AddListener(OnLengthChanged);
        iterationsSlider.onValueChanged.AddListener(OnIterationsChanged);
        flowerProbabilitySlider.onValueChanged.AddListener(OnFlowerProbabilityChanged);
        windToggle.onValueChanged.AddListener(OnWindToggle);

        playButton.onClick.AddListener(OnPlay);
        pauseButton.onClick.AddListener(OnPause);
        restartButton.onClick.AddListener(OnRestart);

        // Limites para os sliders
        angleSlider.minValue = 0f;
        angleSlider.maxValue = 90f;

        lengthSlider.minValue = 0.1f;
        lengthSlider.maxValue = 2.0f;

        iterationsSlider.minValue = 1;
        iterationsSlider.maxValue = 10;
        iterationsSlider.wholeNumbers = true;

        flowerProbabilitySlider.minValue = 0f;
        flowerProbabilitySlider.maxValue = 1f;

        gameObject.SetActive(false); // Oculta o HUD ao início
    }

    public void SetCurrentPlant(PlantInterpreter plant)
    {
        currentPlant = plant;

        if (plant == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Atualiza os sliders com os valores da planta
        angleSlider.value = plant.angle;
        lengthSlider.value = plant.length;
        iterationsSlider.value = plant.iterations;
        flowerProbabilitySlider.value = plant.flowerProbability;
        windToggle.isOn = true; // Ou obter de um flag se usares vento por planta

        gameObject.SetActive(true);
    }

    void OnAngleChanged(float value)
    {
        angleValueText.text = value.ToString("F1") + "°";
        if (currentPlant != null)
            currentPlant.angle = value;
    }

    void OnLengthChanged(float value)
    {   
        lengthValueText.text = value.ToString("F2") + " m";
        if (currentPlant != null)
            currentPlant.length = value;
    }

    void OnIterationsChanged(float value)
    {
        int val = Mathf.RoundToInt(value);
        iterationsValueText.text = val.ToString() + "x";
        if (currentPlant != null)
            currentPlant.iterations = val;
    }

    void OnFlowerProbabilityChanged(float value)
    {
        flowerProbabilityValueText.text = (value * 100).ToString("F0") + "%";
        if (currentPlant != null)
            currentPlant.flowerProbability = value;
    }

    void OnWindToggle(bool on)
    {
        if (currentPlant != null)
        {
            currentPlant.isWindOn = on;
        }
    }

    void OnPlay()
    {
        if (currentPlant != null)
        {
            isPaused = false;
            Time.timeScale = 1f;
        }
    }

    void OnPause()
    {
        if (currentPlant != null)
        {
            isPaused = true;
            Time.timeScale = 0f;
        }
    }

    void OnRestart()
    {
        if (currentPlant != null)
        {
            // Guardar posição e hierarquia do vaso atual
            Vector3 pos = currentPlant.transform.position;
            Quaternion rot = currentPlant.transform.rotation;
            Transform parent = currentPlant.transform.parent;

            // Remover o vaso antigo
            Destroy(currentPlant.transform.root.gameObject);
            currentPlant = null;

            // Instanciar novo vaso (sem parent direto)
            GameObject nova = Instantiate(vaso, pos, rot);
            nova.transform.SetParent(null); // associar à root da cena

            // Obter o novo interpretador da planta
            PlantInterpreter novoInterpreter = nova.GetComponentInChildren<PlantInterpreter>();

            // Atualizar no PlayerController
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.currentVaso = nova;
                player.UpdateVasosCache();
            }
            else
            {
                Debug.Log("Variável player == null");
            }

            // Atualizar HUD
            SetCurrentPlant(novoInterpreter);

            // Ocultar HUD (opcional — pode ser removido se quiseres que fique aberto)
            HideHUD();
        }
    }

    public void HideHUD()
    {
        canvas.SetActive(false);
        // Reativa o controlo do jogador/câmara
        GameObject.FindAnyObjectByType<PlayerController>()?.ToggleHUDControl(false);
    }

}

