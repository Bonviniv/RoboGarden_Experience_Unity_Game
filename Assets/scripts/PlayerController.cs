using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // Velocidade de movimento do jogador (não exceder 10 para evitar problemas de colisão)
    public float moveSpeed = 7f;

    // Parâmetros para o efeito de flutuação vertical
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;

    // Velocidade de rotação do personagem
    public float rotationSpeed = 10f;

    public float posteDist=18f;

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
     public bool wannaDrop=false;

    private Vector3 startPosition;         // Posição inicial do jogador
    private Transform cameraTransform;     // Referência à câmera principal

    private List<GameObject> vasosInScene = new List<GameObject>(); // Cache local dos vasos com tag "vaso"
                                                                 
    private Dictionary<Light, (Color, float)> posteOriginalSettings = new Dictionary<Light, (Color, float)>(); // Guarda a intensidade e cor original das luzes dos postes


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
        MoveArms();
        CheckWaitingAnimation(); // Add this line
    }


     // Add this function after MoveArms()
    void CheckWaitingAnimation()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("waiting") )
            {
                if ( !currentPlanta)
                {

                    Debug.Log(" currentPlanta == null");
                    animator.SetTrigger("drop");
                    carrying = false;
                    Debug.Log("Drop triggered from waiting state");
                }
            }
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
        GameObject closestVaso = null;

        foreach (GameObject vaso in vasosInScene)
        {
            float dist = Vector3.Distance(transform.position, vaso.transform.position);

            // Reseta a cor do filho com tag "vasoBase" para branco
            foreach (Transform child in vaso.transform)
            {
                if (child.CompareTag("vasoBase"))
                {
                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                        rend.material.color = Color.white;
                }
            }

            // Seleciona o vaso mais próximo dentro de 5 unidades de distância
            if (dist < 5f && dist < minDist)
            {
                minDist = dist;
                closestVaso = vaso;
            }
        }

        if (closestVaso != null)
        {
            canPick = true;
            currentVaso = closestVaso;

            // Destaca o filho com tag "vasoBase" do vaso mais próximo
            foreach (Transform child in currentVaso.transform)
            {
                if (child.CompareTag("vasoBase"))
                {
                    Renderer rend = child.GetComponent<Renderer>();
                    if (rend != null)
                        rend.material.color = Color.green;
                }
            }
        }
        else
        {
            // Nenhum vaso próximo
            canPick = false;
            currentVaso = null;
        }
    }


    /// Trata as ações de pegar e soltar vasos ao pressionar a tecla Espaço.
    void HandlePickDrop(float groundHeight)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Pegar vaso
            if (canPick && !carrying && currentVaso != null)
            {
                animator.SetTrigger("pick");

                // Coloca o vaso como filho do ponto de carregamento
                currentVaso.transform.SetParent(vaso_space);
                currentVaso.transform.localPosition = Vector3.zero;

                carrying = true;
                Debug.Log("Trigger 'pick' ativado. carrying = true");
                currentPlanta = currentVaso;
            }
            // Soltar vaso
        

          
        }
        if (Input.GetKeyDown(KeyCode.M))
        {

         if (carrying && currentPlanta != null )
            {
                animator.SetTrigger("drop");

                // Solta o vaso no chão, próximo do jogador
                currentPlanta.transform.SetParent(null);
                currentPlanta.transform.position = new Vector3(transform.position.x, groundHeight + 0.05f, transform.position.z);

                carrying = false;
                Debug.Log("Trigger 'drop' ativado. carrying = false");
                currentPlanta = null;
                currentVaso = null;
            }
        }
    }
    

   
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
                                    }

                                    // Apaga a luz
                                    luz.enabled = false;
                                }
                                else
                                {
                                    // Liga a luz com os valores originais
                                    if (posteOriginalSettings.TryGetValue(luz, out var settings))
                                    {
                                        luz.color = settings.Item1;
                                        luz.intensity = settings.Item2;
                                    }

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
