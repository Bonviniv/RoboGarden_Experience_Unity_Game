using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

[System.Serializable]

// Interpreta instruções L-Systems para gerar plantas 3D
// Controla crescimento, folhas, flores e efeitos de vento
public class ProductionRule
{
    public char predecessor;
    public string successor;
    [Range(0.01f, 1f)]
    public float probability = 1f;
}

[System.Serializable]
public class RuleSet
{
    public string name;
    public List<ProductionRule> rules = new List<ProductionRule>();
}

public class PlantInterpreter : MonoBehaviour
{
    [Header("Core Prefabs")]
    public GameObject potPrefab;

    [Header("L-System Settings")]
    public string initialAxiom = "F";
    public int iterations = 3;
    [Tooltip("Base length of a branch segment")]
    public float length = 0.5f;
    [Tooltip("Angle for turns (+, -, &, ^, <, >)")]
    public float angle = 22.5f;
    public bool isWindOn = true;

    [Header("Stochastic L-System Rule Sets")]
    public List<RuleSet> allRuleSets = new List<RuleSet>();
    public RuleSet selectedRuleSet;

    [Header("Attachment Prefabs")]
    public GameObject[] leafPrefabs;
    public GameObject[] flowerPrefabs;
    [Range(0f, 1f)]
    public float flowerProbability = 0.3f;

    [Header("Plant Max Height")]
    public float plantMaxHeight = 0f; // Altura máxima permitida para a planta

    [Header("Growing Limit")]
    public float limit = 1.2f;

    [Header("Wind Amplification")]
    [Tooltip("Fator de amplificação recursiva da rotação do vento por nível da árvore.")]
    public float windRotationAmplification = 1.333f;

    // Estado interno da "tartaruga" e da geração
    public Vector3 currentPosition;
    public Quaternion currentRotation;
    public Stack<TransformState> transformStack;
    public Transform currentParent;

    public int branchIdCounter = 0;
    public GameObject selectedLeaf;
    public GameObject selectedFlower;
    public List<GameObject> spawnedLeaves = new List<GameObject>(); 
    public List<Transform> allBranchPivots = new List<Transform>(); 

    // NOVO: Referência ao tronco principal para o vento
    private Transform mainTrunkPivot; 
    // NOVO: Lista para os ramos terminais para efeito de "farfalhar"
    private List<Transform> terminalBranchPivots = new List<Transform>();

    [Header("Wind Simulation")]
    public float windSpeed = 1.0f; // Velocidade do vento (frequência de balanço)
    public float windStrength = 5.0f; // Força do vento (amplitude de balanço do tronco)
    public float leafFlutterSpeed = 2.0f; // Velocidade do farfalhar das folhas/ramos terminais
    public float leafFlutterStrength = 2.0f; // Força do farfalhar das folhas/ramos terminais
    public Vector3 windDirection = new Vector3(0.5f, 0, 0.5f).normalized; // Direção preferencial do vento (X e Z)

    [System.Serializable]
    public struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Transform parent;

        public TransformState(Vector3 pos, Quaternion rot, Transform par)
        {
            position = pos;
            rotation = rot;
            parent = par;
        }
    }

    void Reset()
    {
        Debug.Log("PlantInterpreter: Resetting default L-System RuleSets and parameters.");

        initialAxiom = "F";
        iterations = 2;
        length = 0.4f;
        angle = 22.5f;
        flowerProbability = 0.3f;

        // --- REGRAS DE PRODUÇÃO AJUSTADAS PARA MAIS RAMIFICAÇÃO LATERAL E MENOS CRESCIMENTO VERTICAL ---
        allRuleSets = new List<RuleSet>()
        {
            // Rule Set 1: "ClassicBush" - Mais arbusto, menos poste
            new RuleSet()
            {
                name = "ClassicBush",
                rules = new List<ProductionRule>()
                {
                    // X deve criar ramificações laterais robustas.
                    new ProductionRule() { predecessor = 'X', successor = "F[+X][-X]F[+F]", probability = 0.6f }, // Mais ramos laterais, alguns com F
                    new ProductionRule() { predecessor = 'X', successor = "F[&X][^X]F[-F]", probability = 0.4f }, // Inclinações, e ramos extras
                    // F agora com maior chance de ramificar, e ramificações se afastam do tronco.
                    new ProductionRule() { predecessor = 'F', successor = "FF", probability = 0.3f }, // Cresce um pouco reto, mas com menor prob.
                    new ProductionRule() { predecessor = 'F', successor = "F[+FX][-FX]", probability = 0.4f }, // Ramos X inclinados
                    new ProductionRule() { predecessor = 'F', successor = "F[&F][^F]", probability = 0.3f } // Ramos inclinados
                }
            },

            // Rule Set 2: "TallPine" - Ainda alto, mas com ramificações mais evidentes
            new RuleSet()
            {
                name = "TallPine",
                rules = new List<ProductionRule>()
                {
                    // A é o ponto de ramificação da copa da árvore
                    new ProductionRule() { predecessor = 'A', successor = "F[&AFA][-AFA]", probability = 0.6f }, // Mais inclinação para baixo
                    new ProductionRule() { predecessor = 'A', successor = "F[+A][-A]F", probability = 0.4f }, // Ramificações laterais claras
                    // F (tronco/galhos) com mais foco em ramificações A
                    new ProductionRule() { predecessor = 'F', successor = "FF", probability = 0.3f }, // Tronco principal cresce, mas pouco.
                    new ProductionRule() { predecessor = 'F', successor = "F[+A][-A]", probability = 0.4f }, // Incentiva ramos A laterais
                    new ProductionRule() { predecessor = 'F', successor = "F[&A][^A]", probability = 0.3f }, // Incentiva ramos A inclinados
                    new ProductionRule() { predecessor = 'S', successor = "F[-S][+S]S", probability = 1.0f }
                }
            },

            // Rule Set 3: "GroundCover_UpwardVariant" - Mais espalhada, menos vertical, mas ainda subindo um pouco
            new RuleSet()
            {
                name = "GroundCover_UpwardVariant",
                rules = new List<ProductionRule>()
                {
                    // S deve se espalhar mais
                    new ProductionRule() { predecessor = 'S', successor = "F[+S][-S]FS", probability = 0.7f }, // Fortalece ramificação lateral
                    new ProductionRule() { predecessor = 'S', successor = "F[&S][^S]FS", probability = 0.3f }, // Inclinação variada
                    // F agora favorece iniciar o crescimento S lateral
                    new ProductionRule() { predecessor = 'F', successor = "F[+S][-S]", probability = 0.6f }, // Grande chance de ramificar com S
                    new ProductionRule() { predecessor = 'F', successor = "FS", probability = 0.4f } // Continua com S, menos F puro
                }
            },

            // Rule Set 4: "WildAsymmetricBush" - Mais caótico e espalhado
            new RuleSet()
            {
                name = "WildAsymmetricBush",
                rules = new List<ProductionRule>()
                {
                    // W deve criar ramificações muito divergentes
                    new ProductionRule() { predecessor = 'W', successor = "F[++W][--W]F[+W]F", probability = 0.3f }, // Mais rotações fortes
                    new ProductionRule() { predecessor = 'W', successor = "F[&F[W]][^F[W]]F", probability = 0.4f }, // Inclinações e ramificações complexas
                    new ProductionRule() { predecessor = 'W', successor = "FF[+W][-W]W", probability = 0.3f }, // Cresce reto e ramifica lateralmente
                    // F com mais chance de iniciar W
                    new ProductionRule() { predecessor = 'F', successor = "FW", probability = 0.4f },
                    new ProductionRule() { predecessor = 'F', successor = "F[+W]F[-W]", probability = 0.3f },
                    new ProductionRule() { predecessor = 'F', successor = "F[&W]F[^W]", probability = 0.3f } // Inclinações adicionais
                }
            }
        };
    }

    public void Start()
    {
        if (potPrefab == null)
        {
            Debug.LogError("Pot Prefab não está atribuído no PlantInterpreter. Não é possível gerar a planta.");
            enabled = false;
            return;
        }
        if (allRuleSets == null || allRuleSets.Count == 0)
        {
            Debug.LogWarning("Lista 'allRuleSets' está vazia ou nula no Start. Tentando popular com defaults via Reset().");
            Reset();
            if (allRuleSets == null || allRuleSets.Count == 0)
            {
                Debug.LogError("Não foi possível popular 'allRuleSets'. Verifique a implementação de Reset() ou configure manualmente no Inspector.");
                enabled = false;
                return;
            }
        }

        ClearGeneratedPlant(); 

        selectedRuleSet = allRuleSets[Random.Range(0, allRuleSets.Count)];
        Debug.Log($"PlantInterpreter: Usando RuleSet '{selectedRuleSet.name}' e Axioma Inicial '{initialAxiom}'.");

        transformStack = new Stack<TransformState>();
        spawnedLeaves.Clear(); 
        allBranchPivots.Clear(); 
        terminalBranchPivots.Clear(); // Limpar a lista de terminais

        GameObject potInstance = Instantiate(potPrefab, transform.position, transform.rotation);
        potInstance.transform.SetParent(this.transform); 

        // LIGA A PLANTA AO INTERPRETER
        PlantConnectionHUD ligacao = potInstance.AddComponent<PlantConnectionHUD>();
        ligacao.interpreterSource = this;

        potInstance.tag = "vaso";
        potInstance.name = "Vaso";

        // NOVO: limite seja sempre 1 metro acima do topo do vaso automaticamente
        plantMaxHeight = potInstance.transform.position.y + limit;

        currentParent = potInstance.transform; 
        currentPosition = potInstance.transform.position + potInstance.transform.up * 0.1f;
        currentRotation = potInstance.transform.rotation * Quaternion.Euler(0, Random.Range(0, 360f), 0);

        if (leafPrefabs != null && leafPrefabs.Length > 0)
            selectedLeaf = leafPrefabs[Random.Range(0, leafPrefabs.Length)];
        if (flowerPrefabs != null && flowerPrefabs.Length > 0)
            selectedFlower = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)];

        string finalInstructions = ExpandInstructions(initialAxiom, iterations, selectedRuleSet);
        Debug.Log($"Instruções Finais ({selectedRuleSet.name}): {finalInstructions}");

        branchIdCounter = 0; 
        Interpret(finalInstructions);

        // O primeiro pivot é o tronco principal
        if (allBranchPivots.Count > 0)
        {
            mainTrunkPivot = allBranchPivots[0];
            Debug.Log($"PlantInterpreter: Tronco principal identificado: {mainTrunkPivot.name}");
        }
        else
        {
            Debug.LogWarning("PlantInterpreter: Nenhum ramo principal foi gerado para aplicar o vento.");
        }

        AttachLeavesAndFlowers();
        
        Debug.Log("PlantInterpreter: Geração da planta concluída.");
    }

    void Update()
    {   
        ApplyWindEffect();
    }

    string ExpandInstructions(string currentAxiom, int numberOfIterations, RuleSet rulesToUse)
    {
        StringBuilder nextAxiomBuilder = new StringBuilder();

        for (int i = 0; i < numberOfIterations; i++)
        {
            nextAxiomBuilder.Clear();
            foreach (char symbol in currentAxiom)
            {
                List<ProductionRule> applicableRules = rulesToUse.rules
                                                                 .Where(r => r.predecessor == symbol)
                                                                 .ToList();

                if (applicableRules.Count > 0)
                {
                    float totalProb = applicableRules.Sum(r => r.probability);
                    if (totalProb == 0f) totalProb = 1f;

                    float randomValue = Random.Range(0f, totalProb);
                    float cumulativeProb = 0f;
                    bool ruleApplied = false;

                    foreach (ProductionRule rule in applicableRules)
                    {
                        cumulativeProb += rule.probability;
                        if (randomValue <= cumulativeProb)
                        {
                            nextAxiomBuilder.Append(rule.successor);
                            ruleApplied = true;
                            break;
                        }
                    }
                    if (!ruleApplied && applicableRules.Any())
                    {
                        nextAxiomBuilder.Append(applicableRules[0].successor); 
                    }
                }
                else
                {
                    nextAxiomBuilder.Append(symbol); 
                }
            }
            currentAxiom = nextAxiomBuilder.ToString();
        }
        return currentAxiom;
    }

    public void Interpret(string instructions)
    {
        transformStack.Clear();

        foreach (char symbol in instructions)
        {
            switch (symbol)
            {
                case 'F':
                case 'G':
                case 'S':
                case 'W': 
                case 'A':
                    GrowBranch();
                    break;
                case '+':
                    currentRotation *= Quaternion.Euler(0, angle, 0); 
                    break;
                case '-':
                    currentRotation *= Quaternion.Euler(0, -angle, 0); 
                    break;
                case '&':
                    currentRotation *= Quaternion.Euler(angle, 0, 0); 
                    break;
                case '^':
                    currentRotation *= Quaternion.Euler(-angle, 0, 0); 
                    break;
                case '<':
                    currentRotation *= Quaternion.Euler(0, 0, angle); 
                    break;
                case '>':
                    currentRotation *= Quaternion.Euler(0, 0, -angle); 
                    break;
                case '[':
                    transformStack.Push(new TransformState(currentPosition, currentRotation, currentParent));
                    break;
                case ']':
                    if (transformStack.Count > 0)
                    {
                        TransformState state = transformStack.Pop();
                        currentPosition = state.position;
                        currentRotation = state.rotation;
                        currentParent = state.parent;
                    }
                    break;
            }
        }
    }

    void GrowBranch()
    {
        if (currentPosition.y >= plantMaxHeight) {
            return;

        } else {
            GameObject pivotGO = new GameObject($"Pivot_Ramo_{branchIdCounter}");
            pivotGO.transform.SetParent(currentParent);
            pivotGO.transform.position = currentPosition;
            pivotGO.transform.rotation = currentRotation;

            GameObject branchGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branchGO.name = $"Ramo_{branchIdCounter}";
            branchIdCounter++;

            Renderer renderer = branchGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.55f, 0.27f, 0.07f);
            }
            Collider col = branchGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            branchGO.transform.SetParent(pivotGO.transform);
            branchGO.transform.localPosition = new Vector3(0, length / 2f, 0);
            branchGO.transform.localRotation = Quaternion.identity;
            branchGO.transform.localScale = new Vector3(0.05f, length / 2f, 0.05f);

            allBranchPivots.Add(pivotGO.transform);

            currentPosition += pivotGO.transform.up * length;
            currentParent = pivotGO.transform;
        }
    }
        

    void AttachLeavesAndFlowers()
    {
        if (selectedLeaf == null)
        {
            Debug.LogWarning("Prefab de folha não atribuído. Não é possível adicionar folhas.");
            return;
        }

        spawnedLeaves.Clear(); 
        terminalBranchPivots.Clear(); // Limpar lista antes de preencher

        foreach (Transform branchPivot in allBranchPivots)
        {
            bool isTerminalBranch = true;
            foreach (Transform child in branchPivot)
            {
                if (child.name.StartsWith("Pivot_Ramo_")) 
                {
                    isTerminalBranch = false;
                    break;
                }
            }

            if (isTerminalBranch)
            {
                Vector3 leafPosition = branchPivot.position + branchPivot.up * length;
                Quaternion leafRotation = branchPivot.rotation;

                GameObject leaf = Instantiate(selectedLeaf, leafPosition, leafRotation, branchPivot);
                leaf.transform.localScale = Vector3.one * 0.025f;
                spawnedLeaves.Add(leaf); 
                terminalBranchPivots.Add(branchPivot); // Adiciona este pivot à lista de terminais
            }
        }

        ConvertLeavesToFlowers();
    }

    void ConvertLeavesToFlowers()
    {
        if (selectedFlower == null || spawnedLeaves.Count == 0) return;

        int leavesToConvert = Mathf.RoundToInt(spawnedLeaves.Count * flowerProbability);
        
        List<GameObject> tempLeaves = new List<GameObject>(spawnedLeaves);

        for (int i = 0; i < leavesToConvert; i++)
        {
            if (tempLeaves.Count == 0) break;

            int randomIndex = Random.Range(0, tempLeaves.Count);
            GameObject leafToConvert = tempLeaves[randomIndex];

            if (leafToConvert != null)
            {
                GameObject flower = Instantiate(selectedFlower, leafToConvert.transform.position, leafToConvert.transform.rotation, leafToConvert.transform.parent);
                flower.transform.localScale = Vector3.one * 0.025f;

                Destroy(leafToConvert);
            }
            tempLeaves.RemoveAt(randomIndex);
        }
        spawnedLeaves.Clear();
    }

    // NOVO: Função para aplicar o efeito de vento
    void ApplyWindEffect()
    {
        if (mainTrunkPivot == null) return; // Não há tronco para balançar

        // Se o vento for desativado no Canvas
        if (isWindOn)
        {
            // Balanço do tronco principal (Global Sway)
            // Usa um seno para um movimento suave e repetitivo.
            // Multiplica por Time.time para movimento contínuo.
            // Multiplica por windSpeed para controlar a frequência.
            // Multiplica por windStrength para controlar a amplitude.
            // Usa windDirection para definir a direção do balanço.
            float sway = Mathf.Sin(Time.time * windSpeed) * windStrength;

            // Aplica a rotação ao tronco. Rotate(angle, axis, Space.World/Self)
            // Usamos Space.Self para que a rotação seja local ao tronco.
            // Para balançar para os lados, rotacionamos em torno do eixo X ou Z local.
            // Vamos usar o eixo X local para inclinar para frente/trás e o Z para os lados.
            // Uma mistura da direção do vento é mais interessante.
            Quaternion targetSwayRotation = Quaternion.Euler(windDirection.x * sway, 0, windDirection.z * sway);

            // Aplica a rotação ao tronco principal (sempre adicionando à rotação inicial do tronco)
            // Ou, uma rotação que se aplica em torno da posição do vaso.
            // Para balançar o tronco inteiro a partir da base:
            mainTrunkPivot.localRotation = Quaternion.Lerp(mainTrunkPivot.localRotation, Quaternion.identity * targetSwayRotation, Time.deltaTime * 5f); // Smooth out the rotation

            // Farfalhar dos ramos terminais (Local Flutter)
            foreach (Transform terminalPivot in terminalBranchPivots)
            {
                if (terminalPivot == null) continue; // Pode ser que um pivot foi destruído se a planta foi limpa no meio do jogo.

                // Aplica um balanço menor e mais rápido aos ramos terminais
                float flutter = Mathf.Sin(Time.time * leafFlutterSpeed + terminalPivot.GetInstanceID()) * leafFlutterStrength; // + InstanceID para variação
                // Balança os ramos terminais em torno do seu próprio eixo, para um efeito de folhagem.
                // Aqui, podemos usar o eixo Y local para um balanço "folhoso" ou Z para um balanço lateral.
                Quaternion targetFlutterRotation = Quaternion.Euler(0, flutter, 0); // Balança em Y local para simular o farfalhar

                terminalPivot.localRotation = Quaternion.Lerp(terminalPivot.localRotation, Quaternion.identity * targetFlutterRotation, Time.deltaTime * 10f); // Mais rápido
            }
        }
    }


    public void ClearGeneratedPlant()
    {
        Transform existingPotTransform = this.transform.Find("Vaso"); 
        if (existingPotTransform != null)
        {
            Destroy(existingPotTransform.gameObject);
        }
        spawnedLeaves.Clear(); 
        allBranchPivots.Clear(); 
        terminalBranchPivots.Clear(); // Limpa também os terminais
        branchIdCounter = 0; 
        mainTrunkPivot = null; // Zera a referência ao tronco
    }
}