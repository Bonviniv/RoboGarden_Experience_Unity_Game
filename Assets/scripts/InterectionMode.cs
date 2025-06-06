using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System.Collections; // Make sure you have this line at the top of your script

// Controla o modo de interação com módulos do armário
// Permite abrir/fechar gavetas, cubículos e manipular plantas
public class InterectionMode : MonoBehaviour
{
    public GameObject playerModel;
    public GameObject camera;
    public GameObject characterCollider; 
    private PlayerController playerController;
    private CameraScript cameraScript;
    private List<GameObject> interactionAreas = new List<GameObject>();
    private bool isInInteractionMode = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private GameObject currentInteractionArea; // Usado para referenciar o poste para a lógica de drop
    public bool showInteractionAreas = false;
    public float spaceFromPoste = -2f;
    public float interactionAreaHeight = 5f;  
    public bool inInteractionMode = false;  
    public float interactionRadius = 6f;
    private GameObject currentModulo;
    private bool isModuloOpen = false;

    public AudioClip interactionModeSound;      
    public float volumeInteractionModeSound = 1f;   

    public AudioClip openAndCloseGavetaSound;      
    public float volumeOpenAndCloseGavetaSound = 0.5f; 

    public AudioClip openAndCloseCubiculoSound;      
    public float volumeOpenAndCloseCubiculoSound = 0.5f;

    private float lastInteractionTime = 0f;
    public float interactionCooldown = 1f; // Adjust as needed, e.g., 1 seconds

    public bool inInterection = false;

    public int tick = 0;
    public int tickInterval = 10; // Define o intervalo de ticks para alternar o estado de interação

    private bool canPickOrDrop = true;


    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null) Debug.LogError("PlayerController not found in scene!");
        cameraScript = camera.GetComponent<CameraScript>();
        if (cameraScript == null) Debug.LogError("CameraScript not found on camera object!");
        
        Invoke("CreateInteractionAreas", 1f);
    }

    public void ToggleCharacterCollider()
    {
        if (characterCollider != null)
        {
            CapsuleCollider capsuleCollider = characterCollider.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = !capsuleCollider.enabled;
                Debug.Log($"Character collider is now {(capsuleCollider.enabled ? "enabled" : "disabled")}");
            } else {
                Debug.LogWarning("CapsuleCollider not found on characterCollider!");
            }
        } else {
            Debug.LogWarning("CharacterCollider GameObject is not assigned!");
        }
    }

    void RotateInteractionArea(GameObject interactionArea, Vector3 postePos, Vector3 armarioPos)
    {
        Vector3 directionToArmario = (armarioPos - postePos).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToArmario);
        interactionArea.transform.rotation = targetRotation;
    }

    void PlayInterectionModeSound()
    {
        if (interactionModeSound != null)
        {
            AudioSource.PlayClipAtPoint(interactionModeSound, Camera.main.transform.position, volumeInteractionModeSound);
        }
    }

    void PlayCubiculoSound()
    {
        if (openAndCloseCubiculoSound != null)
        {
            AudioSource.PlayClipAtPoint(openAndCloseCubiculoSound, Camera.main.transform.position, volumeOpenAndCloseCubiculoSound);
        }
    }

    void PlayGavetaSound()
    {
        if (openAndCloseGavetaSound != null)
        {
            AudioSource.PlayClipAtPoint(openAndCloseGavetaSound, Camera.main.transform.position, volumeOpenAndCloseGavetaSound);
        }
    }

    void CreateInteractionAreas()
    {
        GameObject[] postes = GameObject.FindGameObjectsWithTag("poste");
        Debug.Log($"Found {postes.Length} postes for interaction areas.");
        
        foreach (GameObject poste in postes)
        {
            GameObject interactionArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interactionArea.name = "InteractionArea_" + poste.name;
            
            // Sempre destrua o MeshRenderer e MeshFilter do primitivo
            Destroy(interactionArea.GetComponent<MeshRenderer>());
            Destroy(interactionArea.GetComponent<MeshFilter>());
            
            Vector3 postePos = poste.transform.position;
            Vector3 armarioPos = poste.transform.parent.position;

            float zOffset = -spaceFromPoste;
            Vector3 posteForward = poste.transform.forward;
            float dotProduct = Vector3.Dot(posteForward, (armarioPos - postePos).normalized);
            
            if (dotProduct > 0)
            {
                zOffset = spaceFromPoste;
            }

            interactionArea.transform.position = postePos + poste.transform.forward * zOffset;
            interactionArea.transform.position = new Vector3(
                interactionArea.transform.position.x,
                interactionAreaHeight,
                interactionArea.transform.position.z
            );
            
            interactionArea.transform.localScale = new Vector3(1f, 1f, 1f);
            
            interactionArea.transform.parent = poste.transform.parent;
            
            RotateInteractionArea(interactionArea, postePos, armarioPos);
            
            // Controla a visibilidade para debug
            if (!showInteractionAreas)
            {
                // Já foram destruídos acima, mas deixo para clareza se mudar de ideia
                // Destroy(interactionArea.GetComponent<MeshRenderer>()); 
            }
            else
            {
                MeshRenderer renderer = interactionArea.AddComponent<MeshRenderer>(); // Adiciona de volta se showInteractionAreas for true
                Material debugMaterial = new Material(Shader.Find("Standard"));
                debugMaterial.color = new Color(1f, 0f, 0f, 0.5f); 
                renderer.material = debugMaterial;
                interactionArea.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); // Adiciona de volta o MeshFilter
            }
            
            interactionAreas.Add(interactionArea);
            Debug.Log($"Created interaction area '{interactionArea.name}' at position {interactionArea.transform.position}");
        }
    }

    void Update()
    {
        tick++;
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isInInteractionMode)
            {
                foreach (GameObject area in interactionAreas)
                {
                    float distance = Vector3.Distance(playerController.transform.position, area.transform.position);
                    if (distance <= interactionRadius)
                    {
                        EnterInteractionMode(area);
                        break;
                    }
                }
            }
            else
            {
                ExitInteractionMode();
            }
        }

       


        // AQUI É A PARTE ALTERADA
        if (isInInteractionMode && Input.GetMouseButtonDown(1)) // Botão direito do mouse para interagir
        {

             if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                // Lógica para quando o cursor está bloqueado
            }   
            Debug.Log("Mouse clicked in interaction mode (Right Click)");
            Ray ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                GameObject clickedObject = hit.transform.gameObject;
                Debug.Log("Raycast hit: " + clickedObject.name);

                Transform moduleTransform = clickedObject.transform;
                while (moduleTransform != null)
                {
                    if (moduleTransform.name.Contains("Modulo_C") || moduleTransform.name.Contains("Modulo_G") || moduleTransform.name.Contains("Modulo_P"))
                    {
                        Debug.Log($"Clicked on a Module: {moduleTransform.name}");
                        Transform moduleSpaceForPlant = moduleTransform.Find("planta Space");

                        // Lógica para pegar ou colocar planta
                        if (moduleSpaceForPlant != null && moduleSpaceForPlant.CompareTag("plantaSpace"))
                        {
                            Debug.Log($"Found 'planta Space' for {moduleTransform.name}. Tag: {moduleSpaceForPlant.tag}");
                            GameObject plantInModule = null;
                            foreach (Transform child in moduleSpaceForPlant)
                            {
                                if (child.gameObject.CompareTag("vaso"))
                                {
                                    plantInModule = child.gameObject;
                                    Debug.Log($"Existing plant found in module space: {plantInModule.name}");
                                    break;
                                }
                            }

                            if (playerController.carrying && playerController.currentPlanta != null)
                            {
                                Debug.Log($"Player is carrying: {playerController.currentPlanta.name}. Module space occupied: {plantInModule != null}");
                                if (plantInModule == null)
                                {
                                    if (canPickOrDrop )
                                    {
                                        Debug.Log("Attempting to PLACE plant into empty module space.");
                                        Cursor.lockState = CursorLockMode.Locked;
                                        StartCoroutine(corrotineToPlace(playerController.currentPlanta, moduleSpaceForPlant.gameObject));

                                        //PlacePlantInModule(playerController.currentPlanta, moduleSpaceForPlant.gameObject);

                                    }
                                }
                                else
                                {
                                    Debug.Log("Module space already has a plant. Cannot place player's plant here.");
                                }
                            }
                            else if (!playerController.carrying && plantInModule != null)
                            {
                                if (canPickOrDrop )
                                {
                                    tick = 0; // Reset tick to prevent multiple placements in the same tick    
                                    Debug.Log($"Player not carrying, but plant exists in module: {plantInModule.name}. Attempting to PICK UP.");
                                    playerController.currentPlanta = plantInModule;
                                    playerController.currentVaso = plantInModule;
                                    playerController.carrying = true;
                                    playerController.UpdateVasosCache();

                                    // Desparenta a planta do módulo e habilita renderers
                                    Cursor.lockState = CursorLockMode.Locked;

                                    StartCoroutine(corrotineToPick(plantInModule));
                                    //PickUpPlantFromModule(plantInModule);
                                }


                            }

                            else
                            {
                                Debug.Log("No plant action: Player not carrying and module space empty, or Player carrying and module space full.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Module {moduleTransform.name} does not have a 'planta Space' child with 'plantaSpace' tag, or 'planta Space' is null. Plant interactions skipped for this module.");
                        }

                        // Lógica para abrir/fechar módulos (gavetas/cubículos)
                        // Esta lógica deve ser independente da lógica de plantas.
                        Animator animator = moduleTransform.GetComponent<Animator>();
                        if (animator != null)
                        {
                            // Gerencia o módulo atual para abrir/fechar
                            if (currentModulo != moduleTransform.gameObject)
                            {
                                if (currentModulo != null && isModuloOpen)
                                {
                                    Animator oldAnimator = currentModulo.GetComponent<Animator>();
                                    if (oldAnimator != null)
                                    {
                                        if (currentModulo.name.Contains("Modulo_C")) { PlayCubiculoSound(); }
                                        if (currentModulo.name.Contains("Modulo_G")) { PlayGavetaSound(); }
                                        oldAnimator.SetTrigger("close");
                                        Debug.Log("Closing previous module: " + currentModulo.name);
                                    }
                                }
                                currentModulo = moduleTransform.gameObject;
                                isModuloOpen = false;
                            }

                            if (!isModuloOpen)
                            {
                                Debug.Log("Opening module: " + moduleTransform.name);
                                if (moduleTransform.name.Contains("Modulo_C")) { PlayCubiculoSound(); }
                                if (moduleTransform.name.Contains("Modulo_G")) { PlayGavetaSound(); }
                                animator.SetTrigger("open");
                                isModuloOpen = true;
                            }
                            else if (isModuloOpen && currentModulo == moduleTransform.gameObject)
                            {
                                Debug.Log("Closing module: " + moduleTransform.name);
                                if (moduleTransform.name.Contains("Modulo_C")) { PlayCubiculoSound(); }
                                if (moduleTransform.name.Contains("Modulo_G")) { PlayGavetaSound(); }
                                animator.SetTrigger("close");
                                isModuloOpen = false;
                                currentModulo = null;
                            }
                        }

                        break;
                    }
                    moduleTransform = moduleTransform.parent;
                }
            }
        }
        
    }
 void PlacePlantInModule(GameObject plantToPlace, GameObject targetPlantSpace)
{
    Debug.Log("PlacePlantInModule called."); // Diagnóstico 1

    if (plantToPlace == null || targetPlantSpace == null)
    {
        Debug.LogError("PlacePlantInModule: Invalid plant or target space. Aborting."); // Diagnóstico 2
        return;
    }

    StartCoroutine(ResetCanPickOrDrop());

    Debug.Log($"PlacePlantInModule: plantToPlace = {plantToPlace.name}, targetPlantSpace = {targetPlantSpace.name}"); // Diagnóstico 2 (detalhado)

    // 1. Desparenta da mão do player primeiro (se ainda estiver parentado)
    plantToPlace.transform.SetParent(null);
    Debug.Log($"Plant {plantToPlace.name} detached from previous parent.");

    // 3. Garante que o Rigidbody seja cinemático
    Rigidbody rb = plantToPlace.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Debug.Log($"Rigidbody: isKinematic = {rb.isKinematic}, useGravity = {rb.useGravity}, velocity = {rb.linearVelocity}."); // Diagnóstico 4
    }
    else
    {
        Debug.LogWarning($"Rigidbody not found on plantToPlace! Object name: {plantToPlace.name}"); // Diagnóstico 4 (Erro)
    }

    // 2. Agora parenta ao targetPlantSpace
    plantToPlace.transform.SetParent(targetPlantSpace.transform);
    plantToPlace.transform.localPosition = Vector3.zero; // Garante que a posição local seja 0,0,0 primeiro
    plantToPlace.transform.localRotation = Quaternion.identity;

    Debug.Log($"After SetParent: Plant parent is {plantToPlace.transform.parent?.name}. Plant local position: {plantToPlace.transform.localPosition}. Plant world position: {plantToPlace.transform.position}"); // Diagnóstico 3

    // Deslocar a planta -0.4f no eixo Y localmente como o último passo
    plantToPlace.transform.localPosition += new Vector3(0f, -0.4f, 0f); // ESTA É A LINHA QUE VOCÊ PEDIU

    // 4. Habilita renderers caso tivessem sido desativados
    EnableAllRenderers(plantToPlace);
    Debug.Log("Renderers enabled for plant.");

    // 5. INFORMA O PLAYERCONTROLLER QUE A PLANTA FOI COLOCADA
    if (playerController != null)
    {
        playerController.currentPlanta = null;
        playerController.currentVaso = null;
        playerController.carrying = false;
        playerController.UpdateVasosCache();
        Animator animator = playerController.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("drop");
        }

        Debug.Log($"PlayerController state: carrying = {playerController.carrying}, currentPlanta = {playerController.currentPlanta?.name}"); // Diagnóstico 5
    }
    else
    {
        Debug.LogError("PlayerController reference is null when trying to update state in PlacePlantInModule!");
    }

    Debug.Log($"PlacePlantInModule finished for plant {plantToPlace.name}.");
}

    void PickUpPlantFromModule(GameObject plantToPickUp)
    {
        Debug.Log("PickUpPlantFromModule called.");

        if (plantToPickUp == null)
        {
            Debug.LogError("PickUpPlantFromModule: Invalid plant to pick up.");
            return;
        }

        StartCoroutine(ResetCanPickOrDrop());

        plantToPickUp.transform.SetParent(null);
        Debug.Log($"Plant {plantToPickUp.name} detached from module for drop.");

        Vector3 dropPosition = playerModel.transform.position + playerModel.transform.forward * 1.5f;
        if (playerController != null)
        {
            dropPosition.y = playerController.GetGroundHeight(playerModel.transform.position) + 0.05f;
        }
        else
        {
            dropPosition.y = 0.05f; // Fallback se playerController for null
            Debug.LogWarning("PlayerController is null, using default ground height for plant drop.");
        }

        plantToPickUp.transform.position = dropPosition;
        plantToPickUp.transform.rotation = Quaternion.identity;
        Debug.Log($"Plant {plantToPickUp.name} targeted drop position: {plantToPickUp.transform.position}");

        Rigidbody rb = plantToPickUp.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"Rigidbody for {plantToPickUp.name} set to non-kinematic and gravity enabled.");
        }
        else
        {
            Debug.LogWarning($"PickUpPlantFromModule: No Rigidbody found on {plantToPickUp.name}. Plant will not fall.");
        }

        EnableAllRenderers(plantToPickUp);
        Debug.Log("Renderers enabled for dropped plant.");

        if (playerController != null)
        {
            playerController.currentPlanta = null;
            playerController.currentVaso = null;
            playerController.carrying = false;
            playerController.UpdateVasosCache();
            Debug.Log($"PlayerController state updated after dropping: carrying = {playerController.carrying}, currentPlanta = {playerController.currentPlanta?.name}");
        }

        Debug.Log($"Plant {plantToPickUp.name} dropped from module.");

    }


    private IEnumerator ResetCanPickOrDrop()
    {
    canPickOrDrop = false; // Define a variável como false
    yield return new WaitForSeconds(0.1f); // Espera por 2 segundos
    canPickOrDrop = true; // Define a variável como true novamente
   }
    
   private IEnumerator corrotineToPick(GameObject plantToPickUp)
    {
    
    yield return new WaitForSeconds(1f); // Espera por 2 segundos
        if (canPickOrDrop)
        {
            PickUpPlantFromModule(plantToPickUp);
        }
   }

   
   private IEnumerator corrotineToPlace(GameObject plantToPlace, GameObject targetPlantSpace)
    {
    
    yield return new WaitForSeconds(0.5f); // Espera por 2 segundos
        if (canPickOrDrop)
        {
            PlacePlantInModule(plantToPlace, targetPlantSpace);
        }
   }



    void changeInteractionTick() 
    {
        inInterection = !inInterection;
    }


    // ... (rest of the class) ...
    // --- Funções Auxiliares Existentes (sem alteração) ---

    void UpdateVasoState()
    {
        // This function is still called by old logic, but might become redundant
        // if PlayerController methods are used consistently.
        if (playerController != null)
        {
            playerController.currentVaso = null;
            playerController.canPick = false;
            playerController.carrying = false;
            playerController.UpdateVasosCache();
        }
    }

    void EnterInteractionMode(GameObject area)
    {
        PlayInterectionModeSound();
        ToggleCharacterCollider();
        isInInteractionMode = true;
        inInteractionMode = true; 
        currentInteractionArea = area;

        if (playerController != null)
        {
            // playerController.enabled = false; // Uncomment if you want to disable player movement entirely
        }

        originalCameraPosition = camera.transform.position;
        originalCameraRotation = camera.transform.rotation;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerModel != null)
        {
            SetRendererState(playerModel, false);
        }

        if (playerController.currentPlanta != null)
        {
            SetRendererState(playerController.currentPlanta, false);
        }

        camera.transform.position = area.transform.position;
        
        float currentYRotation = camera.transform.eulerAngles.y;
        float targetYRotation = (currentYRotation > 90 && currentYRotation < 270) ? 180 : 0;
        
        camera.transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        
        if (cameraScript != null)
        {
            cameraScript.enabled = false;
        }
    }

    void CloseWaitingModules()
    {
        GameObject[] allModulos = GameObject.FindGameObjectsWithTag("modulo");
        foreach (GameObject modulo in allModulos)
        {
            Animator animator = modulo.GetComponent<Animator>();
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("waitingC") || stateInfo.IsName("waitingG"))
                {
                    Debug.Log($"Closing waiting module: {modulo.name}");
                    animator.SetTrigger("close");
                }
            }
        }
    }

    void ExitInteractionMode()
    {
        ToggleCharacterCollider();
        CloseWaitingModules();

        if (currentModulo != null && isModuloOpen)
        {
            Animator animator = currentModulo.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("close");
            }
            currentModulo = null;
            isModuloOpen = false;
        }

        isInInteractionMode = false;
        inInteractionMode = false; 

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerModel != null)
        {
            SetRendererState(playerModel, true);
        }

        if (playerController.currentPlanta != null)
        {
            SetRendererState(playerController.currentPlanta, true);
        }

        camera.transform.position = originalCameraPosition;
        camera.transform.rotation = originalCameraRotation;
        
        if (cameraScript != null)
        {
            cameraScript.enabled = true;
        }

        currentInteractionArea = null;
    }

    void SetRendererState(GameObject obj, bool state)
    {
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = state;
        }

        SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = state;
        }
    }

    void EnableAllRenderers(GameObject obj)
    {
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = true;
        }

        SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = true;
        }
    }
}