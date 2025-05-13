using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public class PlantInterpreter : MonoBehaviour
{
    

public GameObject potPrefab; // Referência ao prefa

[Header("L-System Settings")]
   public string initialInstructions = "F[+F[+F]][-F]F";
   public int iterations = 3;

[Header("Folhas e Flores")]
     public GameObject[] leafPrefabs; // folhas disponíveis
     public GameObject[] flowerPrefabs; // 4 flores diferentes

    public int branchIdCounter = 0;


    public float length = 1f;

    private Vector3 position;
    private Quaternion rotation;
    private Stack<TransformState> transformStack;
    private Stack<BranchNode> nodeStack;

    private Transform currentParent;
    private BranchNode currentNode;
    private BranchNode rootNode;

    private static int nextBranchId = 0;

    
      
private GameObject selectedLeaf;
private GameObject selectedFlower;

private List<GameObject> spawnedLeaves = new List<GameObject>(); // todas as folhas instanciada


    [System.Serializable]
    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Transform parent;
        public BranchNode node;

        public TransformState(Vector3 pos, Quaternion rot, Transform par, BranchNode node)
        {
            position = pos;
            rotation = rot;
            parent = par;
            this.node = node;
        }
    }


void SelectRandomFlower()
{
    if (flowerPrefabs.Length == 0)
    {
        Debug.LogWarning("Nenhuma flor foi atribuída ao array flowerPrefabs.");
        return;
    }

    selectedFlower = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)];
}


public string ExpandInstructions(string instructions, int iterations)
{
    string result = instructions;

    for (int i = 0; i < iterations; i++)
    {
        // Aqui você pode substituir por regras mais complexas se quiser
        result = result.Replace("F", "F[+F]F[-F]F"); 
    }

    return result;
}



private class BranchNode
{
    public int id;
    public Transform pivot;
    public GameObject branchGO;
    public List<BranchNode> children;  // Lista para filhos
    public bool hasLeaf;

    public BranchNode(Transform pivot, GameObject branchGO)
    {
        this.id = nextBranchId++;
        this.pivot = pivot;
        this.branchGO = branchGO;
        this.children = new List<BranchNode>();  // Inicializa a lista de filhos
        hasLeaf = false;
    }
}


void Start()
{
    transformStack = new Stack<TransformState>();
    nodeStack = new Stack<BranchNode>();

    position = transform.position + Vector3.up * 0.5f;
    rotation = Quaternion.identity;

    GameObject pot = Instantiate(potPrefab, transform.position, Quaternion.identity);
    Transform potTransform = pot.transform;

    selectedLeaf = leafPrefabs[Random.Range(0, leafPrefabs.Length)];
    SelectRandomFlower(); // Seleciona flor antes de gerar

    string finalInstructions = ExpandInstructions(initialInstructions, iterations);
    Interpret(finalInstructions, potTransform);

    AddLeavesDFS(rootNode);
    ReplaceLeavesWithFlowers(); // Substitui 20% das folhas por flores
    DebugPrintTree(rootNode);
}


   


   public void Interpret(string instructions, Transform initialParent)
{
    // Configura estado inicial
    currentParent = initialParent;
    currentNode = null;
    rootNode = null;
    transformStack.Clear();
    nodeStack.Clear();

    branchIdCounter = 0; // Reinicia contagem de IDs se for recriar planta

    foreach (char symbol in instructions)
    {
        switch (symbol)
        {
            case 'F':
                GrowBranch(); // Cria ramo e atualiza currentNode
                break;

            case '+':
                rotation *= Quaternion.Euler(0, 0, 25);
                break;

            case '-':
                rotation *= Quaternion.Euler(0, 0, -25);
                break;

            case '[':
                // Empilha estado atual: posição, rotação, pai e nó da árvore
                transformStack.Push(new TransformState(position, rotation, currentParent, currentNode));
                break;

            case ']':
                if (transformStack.Count > 0)
                {
                    // Retorna para o estado anterior
                    TransformState state = transformStack.Pop();
                    position = state.position;
                    rotation = state.rotation;
                    currentParent = state.parent;
                    currentNode = state.node;
                }
                break;
        }
    }
}


void GrowBranch()
{
    Vector3 direction = rotation * Vector3.up;
    float segmentLength = length * Random.Range(0.8f, 1.2f);

    // Criar o novo pivot para o ramo
    GameObject pivotGO = new GameObject("BranchPivot");
    pivotGO.transform.SetParent(currentParent);
    pivotGO.transform.position = position;
    pivotGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

    // Criar o cilindro que representa o ramo
    GameObject branchGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    branchGO.name = $"Ramo_{branchIdCounter}";
    Renderer renderer = branchGO.GetComponent<Renderer>();
if (renderer != null)
{
    renderer.material.color = new Color(0.55f, 0.27f, 0.07f); // marrom (RGB normalizado)
}
    branchIdCounter++; // Incrementa ID para debug

    // Configurar a hierarquia e transformações do cilindro
    branchGO.transform.SetParent(pivotGO.transform);
    branchGO.transform.localPosition = new Vector3(0, segmentLength / 2f, 0); // cresce para cima
    branchGO.transform.localRotation = Quaternion.identity;
    branchGO.transform.localScale = new Vector3(0.025f, segmentLength / 2f, 0.025f);

    // Atualiza posição para onde o próximo ramo sairá
    position += direction * segmentLength;
    currentParent = pivotGO.transform; // O próximo ramo será filho desse pivot

    // Cria nó na árvore lógica
    BranchNode newNode = new BranchNode(pivotGO.transform, branchGO);

    if (rootNode == null)
    {
        rootNode = newNode; // Primeiro ramo criado
        currentNode = rootNode;
    }
    else
    {
        currentNode.children.Add(newNode); // Liga o novo ramo ao nó anterior
        currentNode = newNode;
    }
}


  void AddLeavesDFS(BranchNode node)
{
    if (node == null) return;

    // Verificar se o nó é terminal (sem filhos)
    if (node.children.Count == 0)
    {
        float height = node.branchGO.transform.localScale.y;
        Vector3 localTop = node.branchGO.transform.localPosition + Vector3.up * height;

        // Criar a folha e posicioná-la corretamente
        GameObject leaf = Instantiate(selectedLeaf, node.pivot);
        leaf.transform.localPosition = localTop;
        leaf.transform.localRotation = Quaternion.identity;
        leaf.transform.localScale = Vector3.one * 0.025f;

        // Guardar para possível substituição por flor
        spawnedLeaves.Add(leaf);
    }

    // Recursão nos filhos
    foreach (var child in node.children)
    {
        AddLeavesDFS(child);
    }
}



void ReplaceLeavesWithFlowers()
{
    if (selectedFlower == null)
    {
        Debug.LogWarning("Flor não selecionada. Execute SelectRandomFlower() antes.");
        return;
    }

    int totalLeaves = spawnedLeaves.Count;
    int flowersToReplace = Mathf.RoundToInt(totalLeaves * 0.25f);

    List<int> indices = new List<int>();
    while (indices.Count < flowersToReplace)
    {
        int randomIndex = Random.Range(0, totalLeaves);
        if (!indices.Contains(randomIndex))
        {
            indices.Add(randomIndex);
        }
    }

    foreach (int index in indices)
    {
        GameObject leaf = spawnedLeaves[index];

        // Substituir folha por flor
        Vector3 position = leaf.transform.position;
        Quaternion rotation = leaf.transform.rotation;
        Transform parent = leaf.transform.parent;

        Destroy(leaf);

        GameObject flower = Instantiate(selectedFlower, parent);
        flower.transform.position = position;
        flower.transform.rotation = rotation;
    }
}



   void DebugPrintTree(BranchNode node, int depth = 0)
{
    if (node == null) return;

    string indent = new string(' ', depth * 2);

    string childrenIds = node.children.Count > 0 ? string.Join(", ", node.children.ConvertAll(child => child.id.ToString()).ToArray()) : "null";
    
    Debug.Log($"{indent}- Branch ID: {node.id}, Depth: {depth}, Leaf: {node.hasLeaf}, Children: {childrenIds}");

    foreach (var child in node.children)
    {
        DebugPrintTree(child, depth + 1);
    }
}

}
