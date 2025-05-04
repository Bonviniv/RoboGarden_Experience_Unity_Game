using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Collections;

public class ArmarioGenerator : MonoBehaviour
{
    [Header("Deslocamento da estrutura externa")]
    public Vector3 deslocamentoEstrutura = new Vector3(-1f, 0f, 0f);

    [Header("Prefabs dos módulos")]
    public GameObject moduloP;
    public GameObject moduloC;
    public GameObject moduloG;

    [Header("Materiais")]
    public Material metalMaterial;
    public Material vidroMaterial;

    [Header("XML com configuração dos armários")]
    public TextAsset xmlArmarios;

    [Header("Referências aos armario spaces")]
    public List<GameObject> armarioSpaces = new List<GameObject>(); // Adicione manualmente os 4 espaços

    [Header("Parâmetros do grid interno dos módulos")]
    [Tooltip("Espaçamento entre os módulos. Valor 1 significa módulos colados; valores maiores dão margem.")]
    public float moduleSpacing = 1.1f;  
    [Tooltip("Profundidade interna do armário onde os módulos se posicionam")]
    public float internalDepth = 1f;

    [Header("Limites do armário")]
    public int maxColunas = 8;
    public int maxLinhas = 5;

    [Header("Dimensões da estrutura do armário")]
    public float baseThickness = 0.1f;
    public float ceilingThickness = 0.1f;
    public float wallThickness = 0.1f;
    public float backThickness = 0.1f;
    public float doorThickness = 0.05f;

    [Header("Prefabs")]
    public GameObject postePrefab;
    public float posteDistance = 2f; // Distance from armario to post

    [Header("Interação com Portas")]
    public GameObject player; // Marvin reference
    public float doorInteractionRadius = 5f;
    public Color doorGlowColor = Color.green;
    public float doorGlowIntensity = 1f; // Add this new variable
    public float doorSlideSpeed = 2f;
    private Dictionary<string, (Vector3 position, Transform parent)> doorOriginalPositions = new Dictionary<string, (Vector3, Transform)>();
    private GameObject lastGlowingDoor = null;

    // Add debug property
    private bool debugDoors = true;

    private void Start()
    {
        if (xmlArmarios == null)
        {
            Debug.LogError("Arquivo XML não atribuído.");
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlArmarios.text);
        XmlNodeList armarioNodes = xmlDoc.SelectNodes("/armarios/armario");

        foreach (XmlNode armarioNode in armarioNodes)
        {
            int id = int.Parse(armarioNode.Attributes["id"].Value);
            int colunas = int.Parse(armarioNode.Attributes["col"].Value);
            int linhas = int.Parse(armarioNode.Attributes["lin"].Value);

            if (colunas > maxColunas || linhas > maxLinhas)
            {
                Debug.LogWarning($"Armário {id} excede os limites permitidos. Ignorado.");
                continue;
            }

            // Encontra o espaço correspondente (usando o nome que contenha o id)
            GameObject space = armarioSpaces.Find(obj => obj.name.Contains(id.ToString()));
            if (space == null)
            {
                Debug.LogWarning($"Não foi encontrado um armarioSpace correspondente ao id {id}.");
                continue;
            }

            Vector3 basePosition = space.transform.position;

            // Cria o objeto raiz para o armário
            GameObject armarioRoot = new GameObject($"Armario_{id}");
            basePosition.y = 0.2f; // Set Y position to -0.5
            armarioRoot.transform.position = basePosition;
            armarioRoot.transform.parent = space.transform;

            // Calculate center X position based on grid
            float totalWidth = colunas * moduleSpacing;
            float centerX = basePosition.x + (totalWidth / 2f) + deslocamentoEstrutura.x;
            
            // Add post in front of armario
            if (postePrefab != null)
            {
                Vector3 postePosition = new Vector3(centerX, basePosition.y, basePosition.z);
                GameObject poste = Instantiate(postePrefab, postePosition, Quaternion.identity);
                poste.transform.parent = armarioRoot.transform;
                
                // Rotate 180 degrees in Y axis first
                poste.transform.rotation = Quaternion.Euler(0, 180, 0);
                
                // Then move it forward by posteDistance
                poste.transform.position += poste.transform.forward * posteDistance;
            }

            // Calcula dimensões internas do grid de módulos
            float width = colunas * moduleSpacing;
            float height = linhas * moduleSpacing;

            // Cria a estrutura externa com as dimensões baseadas no grid
            CriarEstruturaArmario(armarioRoot.transform, width, height, internalDepth);

            // Instancia os módulos internos conforme o XML
            XmlNodeList linhasXML = armarioNode.SelectNodes("linha");
            for (int y = 0; y < linhasXML.Count; y++)
            {
                XmlNode linha = linhasXML[y];
                XmlNodeList modulos = linha.ChildNodes;

                for (int x = 0; x < modulos.Count; x++)
                {
                    GameObject prefab = GetPrefabByTag(modulos[x].Name);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"Tipo de módulo inválido no armário {id}: {modulos[x].Name}");
                        continue;
                    }

                    // Posicionar cada módulo de forma organizada em um grid.
                    // Cada módulo é posicionado com espaçamento definido por moduleSpacing,
                    // e é centralizado na caixa interna, com Z = internalDepth/2.
                    Vector3 localPos = new Vector3(x * moduleSpacing, y * moduleSpacing, internalDepth / 2f);
                    GameObject modulo = Instantiate(prefab, armarioRoot.transform);
                    modulo.transform.localPosition = localPos;
                    modulo.transform.localRotation = Quaternion.identity;
                    ArrumarOrientacoes(modulo, space); // Add this line before the end of the loop
                }
            }

            // After creating the armário and all its parts, save door positions
            SaveDoorPositions(armarioRoot);
        }
    }

    private void Update()
    {
        CheckDoorInteraction();
    }

    // Update door finding logic in CheckDoorInteraction
    private void CheckDoorInteraction()
    {
        if (player == null) return;

        GameObject nearestDoor = null;
        float nearestDistance = float.MaxValue;

        // Use more specific name check for doors
        foreach (var door in GameObject.FindObjectsOfType<GameObject>().Where(go => 
            (go.name.Contains("_Porta_E") || go.name.Contains("_Porta_D")) && 
            doorOriginalPositions.ContainsKey(go.name)))
        {
            float distance = Vector3.Distance(player.transform.position, door.transform.position);
            
            if (distance < doorInteractionRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestDoor = door;
                //if (debugDoors) Debug.Log($"Found nearest door: {door.name} at distance {distance}");
            }

            // Return door to original position if out of range and not already at original position
            if (distance >= doorInteractionRadius && 
                doorOriginalPositions.ContainsKey(door.name) && 
                Vector3.Distance(door.transform.localPosition, doorOriginalPositions[door.name].position) > 0.01f)
            {
                StartCoroutine(SlideDoorCoroutine(door, doorOriginalPositions[door.name].position));
            }
        }

        // Remove glow from previous door if it's different
        if (lastGlowingDoor != null && lastGlowingDoor != nearestDoor)
        {
            SetDoorEmission(lastGlowingDoor, false);
        }

        // Make nearest door glow
        if (nearestDoor != null)
        {
            SetDoorEmission(nearestDoor, true);
            lastGlowingDoor = nearestDoor;

            // Check for door slide input
            if (Input.GetKeyDown(KeyCode.P))
            {
                SlideDoor(nearestDoor);
            }
        }
    }

    private void SetDoorEmission(GameObject door, bool enableEmission)
    {
        MeshRenderer renderer = door.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            if (enableEmission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", doorGlowColor * doorGlowIntensity);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void SlideDoor(GameObject door)
    {
        if (doorOriginalPositions.ContainsKey(door.name))
        {
            float doorWidth = door.transform.localScale.x;
            bool isAtOriginalPos = Vector3.Distance(door.transform.localPosition, doorOriginalPositions[door.name].position) < 0.01f;
            Vector3 targetPos;

            // Store current position before moving if at original position
            if (isAtOriginalPos)
            {
                // Moving from original position
                if (door.name.Contains("Porta_E"))
                {
                    targetPos = door.transform.localPosition + Vector3.left * doorWidth;
                }
                else // Porta_D
                {
                    targetPos = door.transform.localPosition + Vector3.right * doorWidth;
                }
            }
            else
            {
                // Return to original position
                targetPos = doorOriginalPositions[door.name].position;
            }

            StartCoroutine(SlideDoorCoroutine(door, targetPos));
        }
    }

    private IEnumerator SlideDoorCoroutine(GameObject door, Vector3 targetPosition)
    {
        Vector3 startPosition = door.transform.localPosition;
        float elapsedTime = 0;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * doorSlideSpeed;
            float newX = Mathf.Lerp(startPosition.x, targetPosition.x, elapsedTime);
            door.transform.localPosition = new Vector3(newX, startPosition.y, startPosition.z);
            yield return null;
        }

        door.transform.localPosition = new Vector3(targetPosition.x, startPosition.y, startPosition.z);
    }

    private void SaveDoorPositions(GameObject armarioRoot)
    {
        GameObject portaE = armarioRoot.transform.Find("Porta_E")?.gameObject;
        GameObject portaD = armarioRoot.transform.Find("Porta_D")?.gameObject;

        if (portaE != null)
        {
            doorOriginalPositions[portaE.name] = (portaE.transform.localPosition, armarioRoot.transform);
        }

        if (portaD != null)
        {
            doorOriginalPositions[portaD.name] = (portaD.transform.localPosition, armarioRoot.transform);
        }
    }

    private void ArrumarOrientacoes(GameObject modulo, GameObject armarioSpace)
    {
        // Rotaciona Modulo_G em 90 graus no eixo Y
        if (modulo.name.StartsWith("Modulo_G"))
        {
            modulo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        // Rotaciona Modulo_C em 180 graus no eixo Y
        else if (modulo.name.StartsWith("Modulo_C"))
        {
            modulo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // Verifica a posição Z do armarioSpace
        if (armarioSpace.transform.position.z > 0)
        {
            armarioSpace.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private GameObject GetPrefabByTag(string tag)
    {
        switch (tag)
        {
            case "P": return moduloP;
            case "C": return moduloC;
            case "G": return moduloG;
            default: return null;
        }
    }

    /// Cria a estrutura externa do armário (base, teto, paredes, fundo e portas).
    /// As dimensões são baseadas em width e height do grid interno e internalDepth.
    private void CriarEstruturaArmario(Transform parent, float width, float height, float depth)
    {
        // Base (modified to extend beyond walls)
        CriarParteEstatic("Base", 
            new Vector3(width + (wallThickness * 2), baseThickness, depth),
            new Vector3(width / 2f, -baseThickness / 2f, depth / 2f), 
            parent, metalMaterial);

        // Teto (modified to extend beyond walls)
        CriarParteEstatic("Teto", 
            new Vector3(width + (wallThickness * 2), ceilingThickness, depth),
            new Vector3(width / 2f, height + ceilingThickness / 2f, depth / 2f), 
            parent, metalMaterial);

        // Parede Esquerda
        CriarParteEstatic("Lado_E", 
            new Vector3(wallThickness, height, depth),
            new Vector3(-wallThickness / 2f, height / 2f, depth / 2f), 
            parent, metalMaterial);

        // Parede Direita
        CriarParteEstatic("Lado_D", 
            new Vector3(wallThickness, height, depth),
            new Vector3(width + wallThickness / 2f, height / 2f, depth / 2f), 
            parent, metalMaterial);

        // Fundo
        CriarParteEstatic("Fundo", 
            new Vector3(width, height, backThickness),
            new Vector3(width / 2f, height / 2f, -backThickness / 2f), 
            parent, metalMaterial);

        // Portas de vidro (frontal) — divididas em duas
        // Porta Esquerda
        GameObject portaE = CriarParteEstatic($"{parent.name}_Porta_E", 
            new Vector3(width / 2f, height, doorThickness),
            new Vector3(width / 4f, height / 2f, depth + doorThickness / 2f), 
            parent, vidroMaterial);

        // Porta Direita
        GameObject portaD = CriarParteEstatic($"{parent.name}_Porta_D", 
            new Vector3(width / 2f, height, doorThickness),
            new Vector3(3f * width / 4f, height / 2f, depth + doorThickness / 2f), 
            parent, vidroMaterial);

        // Save door positions immediately after creation
        if (portaE != null)
        {
            doorOriginalPositions[portaE.name] = (portaE.transform.localPosition, parent);
           // if (debugDoors) Debug.Log($"Saved {portaE.name} position: {portaE.transform.localPosition}");
        }
        
        if (portaD != null)
        {
            doorOriginalPositions[portaD.name] = (portaD.transform.localPosition, parent);
            //if (debugDoors) Debug.Log($"Saved {portaD.name} position: {portaD.transform.localPosition}");
        }
    }

    /// Cria uma parte do armário com as dimensões e posição informadas, aplicando o material especificado.
    private GameObject CriarParteEstatic(string nome, Vector3 escala, Vector3 posicaoLocal, Transform parent, Material mat)
    {
        GameObject parte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parte.name = nome;
        parte.transform.parent = parent;
        parte.transform.localScale = escala;
        parte.transform.localPosition = posicaoLocal + deslocamentoEstrutura; // Aplica o deslocamento

        if (mat != null)
        {
            MeshRenderer renderer = parte.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = mat;
        }

        return parte;
    }
}