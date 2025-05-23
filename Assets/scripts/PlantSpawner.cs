using UnityEngine;

public class PlantSpawner : MonoBehaviour
{
    // A referência ao L_SystemGenerator não é mais necessária aqui,
    // pois a lógica de geração será feita inteiramente pelo PlantInterpreter.
    // Se você não for usar o L_SystemGenerator.cs em nenhum outro lugar,
    // pode até excluí-lo do seu projeto.
    // public L_SystemGenerator generator; 

    public PlantInterpreter interpreter; // Ainda precisamos da referência ao Interpreter

    void Start()
    {
        // Verifica se o PlantInterpreter está atribuído
        if (interpreter == null)
        {
            Debug.LogError("PlantInterpreter não está atribuído no PlantSpawner. A geração da planta não será iniciada.");
            enabled = false; // Desativa o script para evitar mais erros
            return;
        }

        // Não há necessidade de chamar interpreter.Interpret(result); explicitamente aqui.
        // O método Start() do PlantInterpreter já fará todo o trabalho de
        // expandir as instruções e interpretar para gerar a planta.
        Debug.Log("PlantSpawner: Iniciando a geração da planta via PlantInterpreter.Start().");
    }
}