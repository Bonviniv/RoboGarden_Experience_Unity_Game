using UnityEngine;

public class PlantSpawner : MonoBehaviour
{
    public L_SystemGenerator generator;
    public PlantInterpreter interpreter;

    void Start()
    {
        string result = generator.GeneratePlant();
        Debug.Log(result);

        // Passa o transform como ponto de origem
        interpreter.Interpret(result, interpreter.transform);
    }
}
