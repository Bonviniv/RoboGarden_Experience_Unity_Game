using System.Collections.Generic;
using UnityEngine;

public class PlantInterpreter : MonoBehaviour
{
    public GameObject branchPrefab;
    public GameObject leafPrefab;
    public GameObject flowerPrefab;
    public float length = 1f;
    public float angle = 25f;

    private Stack<TurtleState> stateStack = new Stack<TurtleState>();
    private List<Vector3> endpoints = new List<Vector3>();

    public void Interpret(string instructions)
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        foreach (char c in instructions)
        {
            switch (c)
            {
                case 'F':
                    Vector3 newPosition = position + rotation * Vector3.up * length;
                    Instantiate(branchPrefab, position + (newPosition - position) / 2, rotation);
                    position = newPosition;
                    break;

                case '+':
                    rotation *= Quaternion.Euler(0, 0, angle + Random.Range(-5f, 5f));
                    break;

                case '-':
                    rotation *= Quaternion.Euler(0, 0, -angle + Random.Range(-5f, 5f));
                    break;

                case '[':
                    stateStack.Push(new TurtleState(position, rotation));
                    break;

                case ']':
                    if (stateStack.Count > 0)
                    {
                        TurtleState state = stateStack.Pop();
                        position = state.position;
                        rotation = state.rotation;
                    }
                    break;

                case 'L':
                    Instantiate(leafPrefab, position, Random.rotation);
                    break;
            }
        }

        // 🌸 Colocar flores nas extremidades da stack
        foreach (var pos in GetLeafEndpoints(instructions))
        {
            Instantiate(flowerPrefab, pos, Random.rotation);
        }
    }

    private List<Vector3> GetLeafEndpoints(string instructions)
    {
        // (EXEMPLO SIMPLES) Para já assume que extremidades são as posições finais após 'F' seguidos de ']'
        // Podes depois melhorar com lógica mais robusta (ex: árvore de conexões)
        return new List<Vector3> { transform.position + Vector3.up * 5 }; // mock
    }
}
