using UnityEngine;

public class PlantConnectionHUD : MonoBehaviour
{
    public PlantInterpreter interpreterSource;

    public PlantInterpreter GetInterpreter()
    {
        return interpreterSource;
    }
}
