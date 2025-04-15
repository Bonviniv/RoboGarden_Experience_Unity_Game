using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 13f;
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minYAngle = -10f, maxYAngle = 60f;

    private float rotationX;
    private float rotationY;
    private bool followFloating = true;
    
    // Cache de componentes e variáveis para reduzir alocação de memória
    private Vector3 correctedTargetPos;
    private Vector3 desiredCameraPos;
    private Vector3 direction;
    private Quaternion rotation;
    private RaycastHit hit;
    private readonly Vector3 offsetVector = new Vector3(0, 0, -1); // Vector3 constante para evitar alocação

    private void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            rotationX = angles.y;
            rotationY = angles.x;
        }

        // Configuração do cursor apenas uma vez no início
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return; // Retorno rápido se não houver target

        // Verifica input apenas quando necessário
        if (Input.GetKeyDown(KeyCode.Return))
        {
            followFloating = !followFloating;
        }

        // Calcula rotação
        rotationX += Input.GetAxis("Mouse X") * sensitivity;
        rotationY = Mathf.Clamp(rotationY - Input.GetAxis("Mouse Y") * sensitivity, minYAngle, maxYAngle);
        
        rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Otimização do cálculo de flutuação
        float adjustedY = target.position.y;
        if (!followFloating)
        {
            adjustedY -= Mathf.Sin(Time.time * 2f) * 0.5f;
        }

        // Reutiliza vetores para reduzir garbage collection
        correctedTargetPos.Set(target.position.x, adjustedY, target.position.z);
        desiredCameraPos = correctedTargetPos + rotation * (offsetVector * distance);
        direction = (desiredCameraPos - correctedTargetPos).normalized;

        // Otimização do Raycast com layer mask se necessário
        // Physics.Raycast(correctedTargetPos, direction, out hit, distance, layerMask);
        if (Physics.Raycast(correctedTargetPos, direction, out hit, distance))
        {
            transform.position = hit.point - direction * 0.5f;
        }
        else
        {
            transform.position = desiredCameraPos;
        }

        transform.LookAt(correctedTargetPos);
    }
}