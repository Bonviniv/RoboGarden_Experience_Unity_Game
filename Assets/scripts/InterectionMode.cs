using UnityEngine;
using System.Collections.Generic;
using System.Linq;  // Add this line

public class InterectionMode : MonoBehaviour
{
    public GameObject playerModel;
    public GameObject camera;
    private PlayerController playerController;
    private CameraScript cameraScript;
    private List<GameObject> interactionAreas = new List<GameObject>();
    private bool isInInteractionMode = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private GameObject currentInteractionArea;
    public bool showInteractionAreas = false;
    public float spaceFromPoste = -2f;
    public float interactionAreaHeight = 5f;  
    public bool inInteractionMode = false;  // Add this line
    public float interactionRadius = 6f;
    private GameObject currentModulo;
    private bool isModuloOpen = false;

    void Start()
    {
        // Get components
        playerController = FindObjectOfType<PlayerController>();
        cameraScript = camera.GetComponent<CameraScript>();
        
        // Delay the creation of interaction areas
        Invoke("CreateInteractionAreas", 1f);
    }

    void RotateInteractionArea(GameObject interactionArea, Vector3 postePos, Vector3 armarioPos)
    {
        // Calculate direction from interaction area to armario
        Vector3 directionToArmario = (armarioPos - postePos).normalized;
        
        // Create rotation to face the armario
        Quaternion targetRotation = Quaternion.LookRotation(directionToArmario);
        
        // Apply rotation
        interactionArea.transform.rotation = targetRotation;
    }

    void CreateInteractionAreas()
    {
        // Find all postes by tag
        GameObject[] postes = GameObject.FindGameObjectsWithTag("poste");
        Debug.Log($"Found {postes.Length} postes");
        
        foreach (GameObject poste in postes)
        {
            GameObject interactionArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interactionArea.name = "InteractionArea_" + poste.name;
            
            // Always destroy the MeshRenderer and MeshFilter
            Destroy(interactionArea.GetComponent<MeshRenderer>());
            Destroy(interactionArea.GetComponent<MeshFilter>());
            
            Vector3 postePos = poste.transform.position;
            Vector3 armarioPos = poste.transform.parent.position;

            // Check if interaction area would be between poste and armario
            float zOffset = -spaceFromPoste;
            Vector3 posteForward = poste.transform.forward;
            float dotProduct = Vector3.Dot(posteForward, (armarioPos - postePos).normalized);
            
            // If dot product is positive, poste is facing away from armario
            // so we need to reverse the offset
            if (dotProduct > 0)
            {
                zOffset = spaceFromPoste;
            }

            // Position the interaction area using the correct offset
            interactionArea.transform.position = postePos + poste.transform.forward * zOffset;
            interactionArea.transform.position = new Vector3(
                interactionArea.transform.position.x,
                interactionAreaHeight,
                interactionArea.transform.position.z
            );
            
            // Set debug scale
            interactionArea.transform.localScale = new Vector3(1f, 1f, 1f);
            
            interactionArea.transform.parent = poste.transform.parent;
            
            // Rotate to face armario
            RotateInteractionArea(interactionArea, postePos, armarioPos);
            
            // Only disable mesh renderer if not in debug mode
            if (!showInteractionAreas)
            {
                Destroy(interactionArea.GetComponent<MeshRenderer>());
            }
            else
            {
                // Set a semi-transparent material for debug visualization
                MeshRenderer renderer = interactionArea.GetComponent<MeshRenderer>();
                Material debugMaterial = new Material(Shader.Find("Standard"));
                debugMaterial.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent red
                renderer.material = debugMaterial;
            }
            
            interactionAreas.Add(interactionArea);
            Debug.Log($"Created interaction area at position {interactionArea.transform.position}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isInInteractionMode)
            {
                foreach (GameObject area in interactionAreas)
                {
                    float distance = Vector3.Distance(playerController.transform.position, area.transform.position);
                    if (distance <= interactionRadius)  // Changed from hardcoded 4f to variable
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

     if (isInInteractionMode && Input.GetMouseButtonDown(1))
        {
            Debug.Log("Mouse clicked in interaction mode");
            Ray ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                GameObject clickedObject = hit.transform.gameObject;
                Debug.Log("Raycast hit: " + clickedObject.name);
                
                Transform moduleTransform = clickedObject.transform;
                while (moduleTransform != null)
                {
                    if (moduleTransform.name.Contains("Modulo_C") || moduleTransform.name.Contains("Modulo_G")|| moduleTransform.name.Contains("Modulo_P"))
                    {
                        Transform moduleSpaceForPlant = moduleTransform.Find("planta Space");
                        
                        
                            if (moduleSpaceForPlant != null && moduleSpaceForPlant.CompareTag("plantaSpace"))
                            {
                                foreach (Transform child in moduleSpaceForPlant)
                                {
                                   if (child.gameObject.CompareTag("vaso"))
{
    // Get current vaso position and camera position
    Vector3 currentVasoPos = child.position;
    Vector3 cameraPos = camera.transform.position;
    
    // Calculate drop position between vaso and camera
    Vector3 dropPosition = (currentVasoPos + cameraPos) / 2f;
    dropPosition.y = 5f; // Slight offset from ground
    
    

    // Drop the plant
    child.SetParent(null);
    child.position = dropPosition;
    child.rotation = Quaternion.identity;
    Debug.Log("Plant dropped between current position and camera");
}
                                }
                            }
                        
                        
                        // Check if player is carrying a plant
                        if (playerController.currentPlanta != null)
                        {
                            if (moduleSpaceForPlant != null && moduleSpaceForPlant.CompareTag("plantaSpace"))
                            {
                                // Check if module already has a plant
                                bool hasPlant = false;
                                foreach (Transform child in moduleSpaceForPlant)
                                {
                                    if (child.gameObject.CompareTag("vaso"))
                                    {
                                        hasPlant = true;
                                        break;
                                    }
                                }

                                if (!hasPlant)
                                {
                                     Rigidbody rb =  playerController.currentPlanta.GetComponent<Rigidbody>();
                                     if (rb != null)
                                       {
                                        rb.isKinematic = false;
                                        rb.useGravity = true;
                                       }
                                    // Place the plant
                                    playerController.currentPlanta.transform.SetParent(moduleSpaceForPlant);
                                    playerController.currentPlanta.transform.localPosition = Vector3.zero;
                                    playerController.currentPlanta.transform.localRotation = Quaternion.identity;
                                    
                                    // Enable all renderers before setting currentPlanta to null
                                    EnableAllRenderers(playerController.currentPlanta);
                                    
                                    // Clear the reference and update carrying state
                                    playerController.currentPlanta = null;
                                    playerController.carrying = false;
                                    UpdateVasoState();
                                    Debug.Log("Plant placed in module");
                                }
                            }
                        }
                        // Handle regular module interaction
                        Animator animator = moduleTransform.GetComponent<Animator>();
                        if (animator != null)
                        {
                            if (currentModulo != moduleTransform.gameObject)
                            {
                                currentModulo = moduleTransform.gameObject;
                                isModuloOpen = false;
                            }

                            if (!isModuloOpen)
                            {
                                Debug.Log("Opening module: " + moduleTransform.name);
                                animator.SetTrigger("open");
                                isModuloOpen = true;
                            }
                            else
                            {
                                Debug.Log("Closing module: " + moduleTransform.name);
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

        // Add this function near other utility functions
    void UpdateVasoState()
    {
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
        isInInteractionMode = true;
        inInteractionMode = true;  // Set public variable
        currentInteractionArea = area;

        // Disable player movement
        if (playerController != null)
        {
           // playerController.enabled = false;
        }

        // Store camera's original state
        originalCameraPosition = camera.transform.position;
        originalCameraRotation = camera.transform.rotation;

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable player model renderer
        if (playerModel != null)
        {
            SetRendererState(playerModel, false);
          
        }

        // Disable plant renderer if carrying one
        if (playerController.currentPlanta != null)
        {
            SetRendererState(playerController.currentPlanta, false);
        }

        // Fix camera position and rotation
        camera.transform.position = area.transform.position;
        
        // Get current Y rotation and snap to nearest 180 or 0
        float currentYRotation = camera.transform.eulerAngles.y;
        float targetYRotation = (currentYRotation > 90 && currentYRotation < 270) ? 180 : 0;
        
        // Set camera rotation with X and Z = 0
        camera.transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        
        // Disable camera script
        if (cameraScript != null)
        {
            cameraScript.enabled = false;
        }
    }

    // Add this function after SetRendererState
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
        // Close all waiting modules
        CloseWaitingModules();

        // Close current module if left open
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
        inInteractionMode = false;  // Set public variable

        // Enable player movement
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Hide cursor when exiting
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        isInInteractionMode = false;

        // Enable player model renderer
        if (playerModel != null)
        {
            SetRendererState(playerModel, true);
        }

        // Enable plant renderer if carrying one
        if (playerController.currentPlanta != null)
        {
            SetRendererState(playerController.currentPlanta, true);
        }

        // Restore camera
        camera.transform.position = originalCameraPosition;
        camera.transform.rotation = originalCameraRotation;
        
        // Enable camera script
        if (cameraScript != null)
        {
            cameraScript.enabled = true;
        }

        currentInteractionArea = null;
    }

    void SetRendererState(GameObject obj, bool state)
    {
        // Handle regular MeshRenderers
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = state;
        }

        // Handle SkinnedMeshRenderers
        SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = state;
        }
    }
 // Add this utility function
    void EnableAllRenderers(GameObject obj)
    {
        // Enable all MeshRenderers
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = true;
        }

        // Enable all SkinnedMeshRenderers
        SkinnedMeshRenderer[] skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = true;
        }
    }




}
