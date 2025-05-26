using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

// Controla o jogador (Marvin), incluindo movimento, interação e física
// Gerencia pegar/soltar plantas e interagir com os postes de luz
public class PlayerController : MonoBehaviour
{
    // Velocidade de movimento do jogador (não exceder 10 para evitar problemas de colisão)
    public float moveSpeed = 7f;

    // Parâmetros para o efeito de flutuação vertical
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;
    public LayerMask groundLayer;

    // Velocidade de rotação do personagem
    public float rotationSpeed = 10f;

    public float posteDist = 18f;

    // Altura vertical do personagem em relação ao terreno
    public float heightOffset = 3.11f;

    [SerializeField] private Transform armacaoMarvin; // Referência à parte giratória do personagem

    public Animator animator;        // Referência ao componente Animator
    public Transform vaso_space;     // Local onde o vaso será posicionado quando carregado
    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;

    private Quaternion leftShoulderOriginal;
    private Quaternion rightShoulderOriginal;
    // Move os ombros do jogador com base nas teclas de seta.
    // Add these at the top with other private variables
    private Quaternion leftTargetRotation;
    private Quaternion rightTargetRotation;
    public float armRotationSpeed = 5f; // New variable for arm rotation speed

    // Add these with other private variables at the top
    private bool isIdleAnimationPlaying = true;


    private bool isLeftArmRaised = false;
    private bool isRightArmRaised = false;

    public bool canPick = false;   // Define se o jogador pode pegar um vaso
    public bool carrying = false;  // Define se o jogador está carregando um vaso

    public GameObject currentVaso = null; // Vaso mais próximo atualmente selecionado

    public GameObject currentPlanta = null; // Planta que esta carregando
    public bool wannaDrop = false;

    private Vector3 startPosition;         // Posição inicial do jogador
    private Transform cameraTransform;     // Referência à câmera principal

    private List<GameObject> vasosInScene = new List<GameObject>(); // Cache local dos vasos com tag "vaso"

    private Dictionary<Light, (Color, float)> posteOriginalSettings = new Dictionary<Light, (Color, float)>(); // Guarda a intensidade e cor original das luzes dos postes


    public AudioClip pickSound;     // Sound to play when picking up objects
    public float volumePickSound = 1f;   // Volume for the pick sound (0.0f to 1.0f)

    public AudioClip posteSound;
    public float volumePosteSound = 1f;



    void Start()
    {
        startPosition = transform.position;
        cameraTransform = Camera.main.transform;

        // Esconde e trava o cursor para controle com teclado
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //salva a rotação original dos ombros
        if (leftShoulder)
        {
            leftShoulderOriginal = new Quaternion(-0.415918887f, 0.538022041f, -0.356777161f, 0.640510619f);
            leftTargetRotation = leftShoulderOriginal;
            leftShoulder.rotation = leftShoulderOriginal;
        }
        if (rightShoulder)
        {
            rightShoulderOriginal = new Quaternion(0.487318575f, -0.0388078243f, 0.0576581918f, 0.870454013f);
            rightTargetRotation = rightShoulderOriginal;
            rightShoulder.rotation = rightShoulderOriginal;
        }

        // Preenche a lista de vasos na cena no início
        UpdateVasosCache();
    }

    // Add this new method after HandlePickDrop
    void Update()
    {
        // Lida com movimentação do jogador
        HandleMovement();

        // Check if plant/vaso was removed and trigger drop animation
        if (carrying && currentPlanta == null && currentVaso == null)
        {
            animator.SetTrigger("drop");
            carrying = false;
            Debug.Log("Auto-triggered drop animation after plant placement");
        }

        if (carrying && currentPlanta == null)
        {
            animator.SetTrigger("drop");
            carrying = false;
            Debug.Log("Auto-triggered drop animation after plant placement");
        }


        // Calcula altura do terreno sob o jogador
        float groundHeight = Terrain.activeTerrain.SampleHeight(transform.position);

        // Aplica flutuação vertical
        ApplyFloatingEffect(groundHeight);

        // Lida com a ação de ligar ou desligar postes
        HandlePosteInteraction();


        // Corrige a rotação da "armacaoMarvin"
        if (armacaoMarvin != null)
        {
            armacaoMarvin.rotation = Quaternion.Euler(-90, armacaoMarvin.rotation.eulerAngles.y, armacaoMarvin.rotation.eulerAngles.z);
        }

        // Verifica e destaca o vaso mais próximo
        HighlightClosestVaso();

        // Lida com a ação de pegar ou soltar vaso
        HandlePickDrop(groundHeight);

        MoveArms();

        HandlePickDrop(groundHeight);

        //CheckWaitingAnimation(); // Add this line
    }


    // Add this function after MoveArms()
    void CheckWaitingAnimation()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (currentPlanta)
            {
                Debug.Log(currentPlanta.name);
                if (!stateInfo.IsName("waiting"))
                {
                    animator.SetTrigger("pick");

                }

            }
            else
            {
                if (stateInfo.IsName("waiting"))
                {

                    carrying = false;

                    Debug.Log(" currentPlanta == null");
                    animator.SetTrigger("drop");

                    Debug.Log("Drop triggered from waiting state");

                }
            }


        }
    }


    public void PlayPickSound()
    {
        if (pickSound != null)
        {
            AudioSource.PlayClipAtPoint(pickSound, Camera.main.transform.position, volumePickSound);
        }
    }

    public void PlayPosteSound()
    {
        if (posteSound != null)
        {
            AudioSource.PlayClipAtPoint(posteSound, Camera.main.transform.position, volumePosteSound);
        }
    }


    void MoveArms()
    {
        // Left arm control
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("pressed L");
            if (!isLeftArmRaised)
            {
                animator.enabled = false;  // Disable animator when raising arm
                isIdleAnimationPlaying = false;
                leftTargetRotation = new Quaternion(-0.579968095f, 0.710711062f, 0.182253078f, 0.353992581f);
                isLeftArmRaised = true;
            }
            else
            {
                leftTargetRotation = leftShoulderOriginal;
                isLeftArmRaised = false;

                // Only re-enable animator if both arms are down
                if (!isRightArmRaised)
                {
                    animator.enabled = true;
                    isIdleAnimationPlaying = true;
                }
            }
        }

        // Right arm control
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("pressed R");
            if (!isRightArmRaised)
            {
                animator.enabled = false;  // Disable animator when raising arm
                isIdleAnimationPlaying = false;
                rightTargetRotation = new Quaternion(0.372511625f, -0.319608629f, 0.535428941f, 0.687314689f);
                isRightArmRaised = true;
            }
            else
            {
                rightTargetRotation = rightShoulderOriginal;
                isRightArmRaised = false;

                // Only re-enable animator if both arms are down
                if (!isLeftArmRaised)
                {
                    animator.enabled = true;
                    isIdleAnimationPlaying = true;
                }
            }
        }

        // Apply smooth rotation every frame
        if (leftShoulder != null && !isIdleAnimationPlaying)
        {
            leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, leftTargetRotation, armRotationSpeed * Time.deltaTime);
        }

        if (rightShoulder != null && !isIdleAnimationPlaying)
        {
            rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, rightTargetRotation, armRotationSpeed * Time.deltaTime);
        }
    }


    /// Lida com a movimentação do personagem baseado na câmera.
    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveZ + right * moveX).normalized;

        if (moveDirection.magnitude > 0)
        {
            // Move o jogador
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // Rotaciona o jogador suavemente na direção do movimento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }


    /// Aplica flutuação vertical simulando leveza do personagem.
    void ApplyFloatingEffect(float groundHeight)
    {
        float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, groundHeight + heightOffset + floatOffset, transform.position.z);
    }


    /// Atualiza a lista de vasos na cena com a tag "vaso".
    public void UpdateVasosCache()
    {
        vasosInScene.Clear();
        vasosInScene.AddRange(GameObject.FindGameObjectsWithTag("vaso"));
    }


    /// Identifica o vaso mais próximo e destaca visualmente seu "vasoBase".
    void HighlightClosestVaso()
    {
        float minDist = Mathf.Infinity;
        GameObject closestVasoGameObject = null; // Vai armazenar o GameObject "Vaso" real

        // Limpa qualquer destaque anterior do vaso atual
        if (currentVaso != null)
        {
            // currentVaso agora SEMPRE DEVE SER O GameObject "Vaso" (o que tem o collider/RB)
            // A mesh se chama "vaso" e é filha direta de currentVaso ("Vaso" pai)
            Transform vasoMeshTransform = currentVaso.transform.Find("vaso"); // <--- PONTO DE VERIFICAÇÃO 1
            if (vasoMeshTransform != null)
            {
                Renderer rend = vasoMeshTransform.GetComponent<Renderer>();
                if (rend != null)
                    rend.material.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"Could not find 'vaso' mesh child under {currentVaso.name} for highlight reset. Check mesh name and hierarchy.");
            }
        }

        canPick = false;
        currentVaso = null;

        foreach (GameObject taggedObject in vasosInScene) // vasosInScene tem todos com tag "vaso"
        {
            GameObject vasoCandidate = null;

            // Se o objeto com a tag "vaso" é o "Vaso" (o que tem Collider e Rigidbody)
            if (taggedObject.name.Equals("Vaso") && taggedObject.GetComponent<Collider>() != null && taggedObject.GetComponent<Rigidbody>() != null)
            {
                vasoCandidate = taggedObject;
            }
            // Se o objeto com a tag "vaso" é o "PlantGenerator", procuramos seu filho "Vaso"
            else if (taggedObject.name.Contains("PlantGenerator") && taggedObject.transform.Find("Vaso") != null)
            {
                vasoCandidate = taggedObject.transform.Find("Vaso").gameObject;
            }
            // Se o objeto com a tag "vaso" é o mesh "vaso", procuramos seu pai "Vaso"
            else if (taggedObject.name.Equals("vaso") && taggedObject.transform.parent != null && taggedObject.transform.parent.name.Equals("Vaso"))
            {
                vasoCandidate = taggedObject.transform.parent.gameObject;
            }

            // Só consideramos se encontramos o GameObject "Vaso" real (com Rigidbody)
            if (vasoCandidate == null || vasoCandidate.GetComponent<Rigidbody>() == null) continue;

            float dist = Vector3.Distance(transform.position, vasoCandidate.transform.position);

            if (dist < 5f && dist < minDist)
            {
                minDist = dist;
                closestVasoGameObject = vasoCandidate;
            }
        }

        if (closestVasoGameObject != null)
        {
            canPick = true;
            currentVaso = closestVasoGameObject; // currentVaso é o GameObject "Vaso"

            //Debug.Log($"Closest vaso found (actual Vaso GameObject): {currentVaso.name} at distance {minDist}.");

            // Highlight the 'vaso' mesh child of this 'currentVaso' ("Vaso" GameObject)
            Transform vasoMeshToHighlight = currentVaso.transform.Find("vaso"); // <--- PONTO DE VERIFICAÇÃO 2
            if (vasoMeshToHighlight != null)
            {
                Renderer rend = vasoMeshToHighlight.GetComponent<Renderer>();
                if (rend != null)
                    rend.material.color = Color.green; // <--- AQUI A COR DEVERIA MUDAR
            }
            else
            {
                //Debug.LogWarning($"Could not find 'vaso' mesh child under {currentVaso.name} for highlight. Check mesh name and hierarchy.");
            }
        }
        else
        {
            // Debug.Log("No vaso found within interaction distance. canPick is now false.");
        }
    }


    // Método para obter a altura do chão em uma determinada posição
    public float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        // Raycast para baixo a partir de uma altura segura para encontrar o chão
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 100f, groundLayer))
        {
            return hit.point.y;
        }
        return 0f; // Altura padrão se não encontrar o chão
    }
    // Em PlayerController.cs
    public void ForcePickUp(GameObject plantToPickUp)
    {
        if (plantToPickUp == null)
        {
            Debug.LogError("ForcePickUp: plantToPickUp is null.");
            return;
        }

        Rigidbody rb = plantToPickUp.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Torna cinemático para ser carregado
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero; // Garante que pare de se mover
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"Forced pickup: Rigidbody for {plantToPickUp.name} set to kinematic and gravity disabled. Velocity zeroed.");
        }
        else
        {
            Debug.LogWarning($"ForcePickUp: No Rigidbody found on {plantToPickUp.name}. Cannot pick up correctly.");
        }

        // Parente ao vaso_space do jogador
        plantToPickUp.transform.SetParent(vaso_space);
        plantToPickUp.transform.localPosition = Vector3.zero; // Posiciona na mão do player
        plantToPickUp.transform.localRotation = Quaternion.identity;

        //EnableAllRenderers(plantToPickUp);

        currentPlanta = plantToPickUp;
        currentVaso = plantToPickUp;
        carrying = true;
        canPick = false;

        Debug.Log($"Forced pickup: {plantToPickUp.name} is now carried.");

        // O PlantGenerator deve ter sido desabilitado a tag quando pego pela primeira vez
        // Se ainda for detectado, é um problema de tag duplicada ou hierarquia.
        // O código abaixo pode ser removido se o PlantGenerator estiver sempre untagged corretamente.
        GameObject plantGeneratorParent = plantToPickUp.transform.parent?.gameObject;
        if (plantGeneratorParent != null && plantGeneratorParent.name.Contains("PlantGenerator") && plantGeneratorParent.CompareTag("vaso"))
        {
            plantGeneratorParent.tag = "Untagged";
            Debug.Log($"Forced pickup: Untagged PlantGenerator: {plantGeneratorParent.name}");
        }

        UpdateVasosCache();
    }

    /// Trata as ações de pegar e soltar vasos ao pressionar a tecla Espaço.
    void HandlePickDrop(float groundHeight)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed.");
            Debug.Log($"canPick: {canPick}, carrying: {carrying}, currentVaso: {(currentVaso != null ? currentVaso.name : "NULL")}");

            // Pegar vaso
            if (canPick && !carrying && currentVaso != null)
            {
                PlayPickSound();
                animator.SetTrigger("pick");

                // currentVaso AGORA É SEMPRE O GameObject "Vaso" (o que tem o Rigidbody e Collider)
                Rigidbody rb = currentVaso.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Debug.Log("Rigidbody set to kinematic and gravity disabled for picked vaso.");
                }
                else
                {
                    Debug.LogWarning("No Rigidbody found on currentVaso! Cannot pick up correctly.");
                    // Não retorne aqui, pode haver outras coisas a fazer mesmo sem Rigidbody
                }

                // O PlantGenerator pai (se existir) será desabilitado/destagged.
                // O objeto que carregaremos é o Vaso (currentVaso).
                GameObject plantGeneratorParent = currentVaso.transform.parent?.gameObject;
                if (plantGeneratorParent != null && plantGeneratorParent.name.Contains("PlantGenerator"))
                {
                    // Se o Vaso ainda tem um PlantGenerator como pai (primeira vez que pega)
                    plantGeneratorParent.tag = "Untagged"; // Remove a tag do PlantGenerator
                    // plantGeneratorParent.SetActive(false); // Opcional: desativa o PlantGenerator
                    Debug.Log($"Untagged PlantGenerator: {plantGeneratorParent.name}");
                }
                else
                {
                    // Se o Vaso já estava solto e não tem um PlantGenerator como pai
                    Debug.Log("Vaso is already a top-level object or its PlantGenerator parent is gone.");
                }

                // Parente o Vaso (o currentVaso) ao vaso_space
                currentVaso.transform.SetParent(vaso_space);
                currentVaso.transform.localPosition = Vector3.zero;
                currentVaso.transform.localRotation = Quaternion.identity;

                //EnableAllRenderers(currentVaso); 
                Debug.Log($"Renderers re-enabled for {currentVaso.name} (the picked plant).");

                carrying = true;
                Debug.Log("Trigger 'pick' ativado. carrying = true");
                currentPlanta = currentVaso; // currentPlanta é o GameObject "Vaso"
            }
            else if (canPick && currentVaso != null && carrying)
            {
                Debug.Log("Already carrying a plant, cannot pick up another.");
            }
            else if (!canPick && currentVaso == null)
            {
                Debug.Log("No plant available to pick up (not near any or currentVaso is null).");
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (carrying && currentPlanta != null)
            {
                animator.SetTrigger("drop"); // Dispara a animação

                // currentPlanta AGORA É SEMPRE O GameObject "Vaso" (o que tem o Rigidbody e Collider)
                Rigidbody rb = currentPlanta.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    Debug.Log("Rigidbody set to non-kinematic and gravity enabled for dropped vaso.");
                }
                else
                {
                    Debug.LogWarning("No Rigidbody found on currentPlanta! Cannot drop correctly.");
                    return; // Retorna para evitar erros se não há RB
                }

                // Desparentar e posicionar o GameObject "Vaso" (currentPlanta)
                currentPlanta.transform.SetParent(null);
                currentPlanta.transform.position = new Vector3(transform.position.x, groundHeight + 0.05f, transform.position.z);
                currentPlanta.transform.rotation = Quaternion.identity;

                //EnableAllRenderers(currentPlanta); 
                Debug.Log($"Renderers re-enabled for {currentPlanta.name} (the dropped plant).");

                carrying = false;
                Debug.Log("Trigger 'drop' ativado. carrying = false.");

                currentPlanta = null;
                currentVaso = null; // Zera a referência para ser redescoberta

                UpdateVasosCache(); // Atualiza o cache para re-detectar o vaso no chão
                Debug.Log("Vasos cache updated after dropping plant.");
            }
        }
    }

    // Helper method for finding a child by name, if GetChild(0) is not reliable.
    // You can put this inside your PlayerController class or in a static helper.
    // private Transform FindDeepChild(Transform parent, string name)
    // {
    //     Queue<Transform> queue = new Queue<Transform>();
    //     queue.Enqueue(parent);
    //     while (queue.Count > 0)
    //     {
    //         var child = queue.Dequeue();
    //         if (child.name == name)
    //             return child;
    //         foreach (Transform grandchild in child)
    //             queue.Enqueue(grandchild);
    //     }
    //     return null;
    // }



    /// Permite ao jogador interagir com postes de luz. Pressionar 'P' alterna a luz ligada/desligada.
    void HandlePosteInteraction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject[] postes = GameObject.FindGameObjectsWithTag("poste");

            foreach (GameObject poste in postes)
            {
                float dist = Vector3.Distance(transform.position, poste.transform.position);

                if (dist <= posteDist)
                {
                    // Debug.Log("Perto do poste");
                    // Verifica se o jogador apertou a tecla "P"

                    foreach (Transform child in poste.transform)
                    {
                        if (child.CompareTag("luzPoste"))
                        {
                            Light luz = child.GetComponent<Light>();
                            if (luz != null)
                            {
                                if (luz.enabled)
                                {
                                    // Salva configurações originais se ainda não estiverem salvas
                                    if (!posteOriginalSettings.ContainsKey(luz))
                                    {
                                        posteOriginalSettings[luz] = (luz.color, luz.intensity);
                                        //PlayPosteSound();
                                    }

                                    // Apaga a luz
                                    luz.enabled = false;
                                    PlayPosteSound();
                                }
                                else
                                {
                                    // Liga a luz com os valores originais
                                    if (posteOriginalSettings.TryGetValue(luz, out var settings))
                                    {
                                        luz.color = settings.Item1;
                                        luz.intensity = settings.Item2;
                                        //PlayPosteSound();
                                    }

                                    PlayPosteSound();
                                    luz.enabled = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

}
