using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Collections;

// Gera armários modulares baseados em configurações XML
// Controla a estrutura externa, módulos internos e interação com portas
public class ArmarioGenerator : MonoBehaviour
{
    [Header("Deslocamento da estrutura externa")]
    public Vector3 deslocamentoEstrutura = new Vector3(-1f, 0f, 0f);

    // Prefabs para os módulos (Pequeno, Cubículo, Gaveta)
    [Header("Prefabs dos módulos")]
    public GameObject moduloP;
    public GameObject moduloC;
    public GameObject moduloG;

    // Materiais para metal e vidro
    [Header("Materiais")]
    public Material metalMaterial;
    public Material vidroMaterial;

    [Header("XML com configuração DESTE armário")] // Nome da variável ajustado
    public TextAsset xmlArmarioConfig; // Mudado para xmlArmarioConfig (um único armário)

    // A lista armarioSpaces não é mais necessária aqui, pois cada script será anexado a um.
    // [Header("Referências aos armario spaces")]
    // public List<GameObject> armarioSpaces = new List<GameObject>(); 

    [Header("Parâmetros do grid interno dos módulos")]
    [Tooltip("Espaçamento entre os módulos. Valor 1 significa módulos colados; valores maiores dão margem.")]
    public float moduleSpacing = 2f;
    [Tooltip("Profundidade interna do armário onde os módulos se posicionam")]
    public float internalDepth = 2.5f;

    [Header("Limites do armário (para validação)")]
    public int maxColunas = 8;
    public int maxLinhas = 5;

    [Header("Dimensões da estrutura do armário")]
    public float baseThickness = 0.25f;
    public float ceilingThickness = 0.25f;
    public float wallThickness = 0.25f;
    public float backThickness = 0.1f;
    public float doorThickness = 0.05f;

    [Header("Prefabs")]
    public GameObject postePrefab;
    public float posteDistance = 15f; // Distance from armario to post

    [Header("Interação com Portas")]
    public GameObject player; // Marvin reference
    public float doorInteractionRadius = 15f;
    public Color doorGlowColor = Color.green;
    public float doorGlowIntensity = 0.01f; // Add this new variable
    public float doorSlideSpeed = 2f;
    private Dictionary<string, (Vector3 position, Transform parent)> doorOriginalPositions = new Dictionary<string, (Vector3, Transform)>();
    private GameObject lastGlowingDoor = null;

    // Add debug property
    private bool debugDoors = true;

    public AudioClip openDoorSound;
    public float volumeOpenDoorSound = 1f;

    public AudioClip closeDoorSound;
    public float volumeCloseDoorSound = 1f;


    private void Start()
    {
        // Certifica-se de que o XML de configuração DESTE armário foi atribuído.
        if (xmlArmarioConfig == null)
        {
            Debug.LogError($"Arquivo XML de configuração não atribuído para o ArmarioGenerator em {gameObject.name}.");
            return;
        }

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlArmarioConfig.text);

        // Seleciona o único nó de armário esperado no XML
        XmlNode armarioNode = xmlDoc.SelectSingleNode("/armario"); // Note: mudou para /armario diretamente

        if (armarioNode == null)
        {
            Debug.LogError($"XML inválido para o ArmarioGenerator em {gameObject.name}. Não foi encontrado o nó '/armario'.");
            return;
        }

        // Lemos os atributos deste único armário
        int id = int.Parse(armarioNode.Attributes["id"].Value);
        int colunas = int.Parse(armarioNode.Attributes["col"].Value);
        int linhas = int.Parse(armarioNode.Attributes["lin"].Value);

        if (colunas > maxColunas || linhas > maxLinhas)
        {
            Debug.LogWarning($"Armário {id} em {gameObject.name} excede os limites permitidos. Ignorado.");
            return; // Interrompe a geração se os limites forem excedidos
        }

        // A posição base será a do próprio GameObject onde o script está anexado.
        Vector3 basePosition = this.transform.position;

        // Cria o objeto raiz para o armário (como filho do GameObject atual)
        GameObject armarioRoot = new GameObject($"Armario_{id}");
        armarioRoot.transform.position = basePosition; // Posição do pai
        armarioRoot.transform.rotation = this.transform.rotation; // Rotação do pai
        armarioRoot.transform.parent = this.transform; // Torna o armário filho do GameObject atual

        // A linha basePosition.y = 0.2f; agora pode ser desnecessária se o armarioSpace já estiver na altura correta
        // Ou você pode ajustar a posição do armarioRoot em relação ao seu pai aqui se necessário.
        // Por exemplo, se o "armarioSpace" for o ponto do chão, e o armário precisa começar um pouco acima:
        armarioRoot.transform.localPosition = new Vector3(0, 0.2f, 0); // Ajusta a altura localmente

        // Calcula dimensões internas do grid de módulos
        float width = colunas * moduleSpacing;
        float height = linhas * moduleSpacing;

        // Cria a estrutura externa com as dimensões baseadas no grid
        CriarEstruturaArmario(armarioRoot.transform, width, height, internalDepth);

        // Adiciona poste em frente ao armário.
        // A lógica do poste precisa ser ajustada para a nova hierarquia.
        if (postePrefab != null)
        {
            // O poste será um filho direto do armarioRoot, e sua posição será relativa a ele.
            // A posição e rotação do poste deve ser ajustada para ficar na frente do armário.
            // Assumimos que o postePrefab tem a frente (Z positivo) para onde deve apontar.
            GameObject poste = Instantiate(postePrefab, armarioRoot.transform); // Instancia como filho do armarioRoot

            // Posiciona o poste na frente do armário. O centro do armário é (width/2, height/2, depth/2).
            // A frente do armário é na direção Z positiva em relação à sua localPosition.
            // Para posicionar na frente, usaremos o eixo Z local do armarioRoot.
            // O poste deve ser deslocado para fora do armário.
            poste.transform.localPosition = new Vector3(width / 2f, -0.5f, internalDepth + posteDistance);

            // Se o poste for um candeeiro que ilumina o armário, ele deve estar virado para o armário.
            // Sua rotação padrão pode precisar ser ajustada para apontar para trás.
            // Assumindo que postePrefab já está orientado corretamente ou pode ser rotacionado.
            // Se o poste aponta para Z+ por padrão e queremos que ele ilumine o armário (que está em Z-),
            // ele precisa ser rotacionado 180 graus no eixo Y.
            poste.transform.localRotation = Quaternion.Euler(0, 180, 0);



            // Se o poste estiver muito alto/baixo ou muito para os lados, ajustes finos aqui.
            // Ex: poste.transform.localPosition += new Vector3(0, someHeightOffset, 0);
        }

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

                Vector3 localPos = new Vector3(x * moduleSpacing, y * moduleSpacing, internalDepth / 2f);
                GameObject modulo = Instantiate(prefab, armarioRoot.transform);
                modulo.transform.localPosition = localPos;
                modulo.transform.localRotation = Quaternion.identity;
                // A função ArrumarOrientacoes precisa ser ajustada, pois armarioSpace não é mais relevante aqui
                // Ela provavelmente deveria apenas ajustar a orientação do módulo.
                ArrumarOrientacoesModulo(modulo);
            }
        }

        // After creating the armário and all its parts, save door positions
        SaveDoorPositions(armarioRoot);
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

        // Itera sobre as portas SALVAS neste Dicionário, que pertencem a ESTE armário.
        // Isso é mais eficiente do que FindObjectsOfType e evita interagir com portas de outros armários.
        foreach (var entry in doorOriginalPositions)
        {
            string doorName = entry.Key;
            Transform parentTransform = entry.Value.parent; // Onde a porta foi originalmente salva

            // Re-encontra a porta no cenário usando o nome e o pai para garantir que é a porta correta deste armário.
            // Isso é importante se você tiver portas com nomes repetidos em outros armários.
            // Para simplificar, podemos assumir que os nomes das portas são únicos por armário,
            // ou podemos usar GetChild para buscar apenas nas portas filhas do armarioRoot.
            // A busca abaixo é mais genérica, mas se os nomes forem únicos, está ok.
            GameObject door = parentTransform.Find(doorName)?.gameObject;

            if (door == null) continue; // Porta não encontrada (talvez o nome não seja único ou foi destruída)

            float distance = Vector3.Distance(player.transform.position, door.transform.position);

            if (distance < doorInteractionRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestDoor = door;
                //if (debugDoors) Debug.Log($"Found nearest door: {door.name} at distance {distance}");
            }

            // Return door to original position if out of range and not already at original position
            // Verifica a posição local da porta, pois o parent foi salvo
            if (distance >= doorInteractionRadius &&
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

    void PlayCloseDoorSound()
    {
        if (closeDoorSound != null)
        {
            AudioSource.PlayClipAtPoint(closeDoorSound, Camera.main.transform.position, volumeCloseDoorSound);
        }
    }

    void PlayOpenDoorSound()
    {
        if (openDoorSound != null)
        {
            AudioSource.PlayClipAtPoint(openDoorSound, Camera.main.transform.position, volumeOpenDoorSound);
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
                // Play open door sound
                PlayOpenDoorSound();
            }
            else
            {
                // Return to original position
                targetPos = doorOriginalPositions[door.name].position;
                PlayCloseDoorSound();
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
        // Encontra as portas como filhos diretos do armarioRoot (ou use Find para buscar em toda a hierarquia)
        GameObject portaE = armarioRoot.transform.Find($"{armarioRoot.name}_Porta_E")?.gameObject;
        GameObject portaD = armarioRoot.transform.Find($"{armarioRoot.name}_Porta_D")?.gameObject;

        // Se o nome da porta gerado for apenas "Porta_E" e "Porta_D", use:
        // GameObject portaE = armarioRoot.transform.Find("Porta_E")?.gameObject;
        // GameObject portaD = armarioRoot.transform.Find("Porta_D")?.gameObject;


        if (portaE != null)
        {
            doorOriginalPositions[portaE.name] = (portaE.transform.localPosition, portaE.transform.parent);
        }

        if (portaD != null)
        {
            doorOriginalPositions[portaD.name] = (portaD.transform.localPosition, portaD.transform.parent);
        }
    }

    // Função renomeada para ser mais específica (ArrumarOrientacoesModulo)
    // E removido o parâmetro armarioSpace, pois não é mais relevante aqui.
    private void ArrumarOrientacoesModulo(GameObject modulo)
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

        // A lógica de rotacao do armarioSpace foi removida pois não se aplica mais.
        // if (armarioSpace.transform.position.z > 0)
        // {
        //     armarioSpace.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        // }
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
        GameObject portaE = CriarParteEstatic($"Porta_E", // Nome agora é apenas "Porta_E"
            new Vector3(width / 2f, height, doorThickness),
            new Vector3(width / 4f, height / 2f, depth + doorThickness / 2f),
            parent, vidroMaterial);

        // Porta Direita
        GameObject portaD = CriarParteEstatic($"Porta_D", // Nome agora é apenas "Porta_D"
            new Vector3(width / 2f, height, doorThickness),
            new Vector3(3f * width / 4f, height / 2f, depth + doorThickness / 2f),
            parent, vidroMaterial);

        // Save door positions immediately after creation
        if (portaE != null)
        {
            doorOriginalPositions[portaE.name] = (portaE.transform.localPosition, portaE.transform.parent);
        }

        if (portaD != null)
        {
            doorOriginalPositions[portaD.name] = (portaD.transform.localPosition, portaD.transform.parent);
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