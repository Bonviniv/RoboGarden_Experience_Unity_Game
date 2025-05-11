/*
PlantInterpreter.cs, com:

🌳 GeneratedPlant como GameObject raiz

🌿 Subgrupos: Branches, Leaves, Flowers

🎲 Aleatoriedade em orientações e posições

🌸 Flores apenas nas extremidades terminais

📦 Instanciação correta com parent-child
*/

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
    private Transform plantRoot;
    private Transform branchParent;
    private Transform leafParent;
    private Transform flowerParent;

    // Marcar ramos como terminais -> candidatos a ter flor
    private class DrawnSegment
    {
        public Vector3 position;
        public int depth;
        public bool isTerminal = true;
    }

    private List<DrawnSegment> drawnSegments = new List<DrawnSegment>();
    private int currentDepth = 0;

    public void Interpret(string instructions)
    {
        // Criar hierarquia
        plantRoot = new GameObject("GeneratedPlant").transform;
        plantRoot.SetParent(this.transform);

        branchParent = new GameObject("Branches").transform;
        branchParent.SetParent(plantRoot);

        leafParent = new GameObject("Leaves").transform;
        leafParent.SetParent(plantRoot);

        flowerParent = new GameObject("Flowers").transform;
        flowerParent.SetParent(plantRoot);

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        foreach (char c in instructions)
        {
            switch (c)
            {
                case 'F':
                    float segmentLength = length * Random.Range(0.8f, 1.2f);
                    Vector3 direction = rotation * Vector3.up;
                    Vector3 newPosition = position + direction * segmentLength;

                    // Criar rotação correta com base na direção real
                    Quaternion segmentRotation = Quaternion.LookRotation(direction);
                    segmentRotation *= Quaternion.Euler(90, 0, 0); // Corrige cilindros que crescem no eixo Y

                    // Instanciar no ponto médio
                    Vector3 middle = position + direction * (segmentLength / 2f);
                    Transform branch = Instantiate(branchPrefab, middle, segmentRotation).transform;
                    branch.SetParent(branchParent);
                    branch.gameObject.AddComponent<WindSwing>();  // Intereção com o vento

                    position = newPosition;

                    drawnSegments.Add(new DrawnSegment
                    {
                        position = position,
                        depth = currentDepth,
                        isTerminal = true
                    });
                    break;

                case '+':
                    rotation *= Quaternion.Euler(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        angle + Random.Range(-10f, 10f)
                    );
                    break;

                case '-':
                    rotation *= Quaternion.Euler(
                        Random.Range(-5f, 5f),
                        Random.Range(-5f, 5f),
                        -angle + Random.Range(-10f, 10f)
                    );
                    break;

                case '[':
                    stateStack.Push(new TurtleState(position, rotation));
                    currentDepth++;
                    break;

                case ']':
                    if (stateStack.Count > 0)
                    {
                        TurtleState state = stateStack.Pop();
                        position = state.position;
                        rotation = state.rotation;
                        currentDepth--;
                    }
                    break;

                case 'L':
                    Quaternion randomLeafRot = Quaternion.Euler(
                        Random.Range(-60f, 60f),
                        Random.Range(0f, 360f),
                        Random.Range(-60f, 60f)
                    );

                    Vector3 leafOffset = rotation * new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
                    Transform leaf = Instantiate(leafPrefab, position + leafOffset, randomLeafRot).transform;
                    leaf.SetParent(leafParent);
                    leaf.gameObject.AddComponent<WindSwing>();  // Intereção com o vento
                    break;
            }
        }

        PlaceFlowers();
    }

    private void PlaceFlowers()
    {
        // Marcar como não-terminais os ramos que originam mais profundidade
        for (int i = 0; i < drawnSegments.Count - 1; i++)
        {
            if (drawnSegments[i].depth < drawnSegments[i + 1].depth)
            {
                drawnSegments[i].isTerminal = false;
            }
        }

        foreach (var seg in drawnSegments)
        {
            if (seg.isTerminal)
            {
                Quaternion randomRot = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );

                Vector3 offset = new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    0,
                    Random.Range(-0.1f, 0.1f)
                );

                Transform flower = Instantiate(flowerPrefab, seg.position + offset, randomRot).transform;
                flower.SetParent(flowerParent);
                flower.gameObject.AddComponent<WindSwing>();  // Intereção com o vento
            }
        }
    }
}
