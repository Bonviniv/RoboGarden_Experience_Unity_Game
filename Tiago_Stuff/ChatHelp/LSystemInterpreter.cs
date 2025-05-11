using System.Collections.Generic;
using UnityEngine;

public class LSystemInterpreter : MonoBehaviour
{
    public RuleSetLoader ruleLoader;
    public GameObject branchPrefab;
    public GameObject leafPrefab;
    public GameObject flowerPrefab;

    private Stack<TurtleState> stack = new Stack<TurtleState>();

    private class TurtleState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Transform parent;
    }

    public void GeneratePlant()
    {
        string instructions = LSystemGenerator.Generate(ruleLoader.ruleSet);
        Interpret(instructions, ruleLoader.ruleSet.angle);

        Debug.Log("Generated L-System string length: " + instructions.Length);
    }

    private void Interpret(string instructions, float angle)
    {
        Transform parent = this.transform;
        Vector3 position = parent.position;
        Quaternion rotation = parent.rotation;

        foreach (char symbol in instructions)
        {
            switch (symbol)
            {
                case 'F':
                    GameObject branch = Instantiate(branchPrefab, position, rotation, parent);
                    branch.transform.localScale = new Vector3(0.1f, 1f, 0.1f); // ajustável
                    position += Vector3.up * 1f;
                    parent = branch.transform;
                    break;

                case 'L':
                    GameObject leaf = Instantiate(leafPrefab, position, rotation, parent);
                    RandomizeRotation(leaf.transform);
                    break;

                case 'X':
                    GameObject flower = Instantiate(flowerPrefab, position, rotation, parent);
                    RandomizeRotation(flower.transform);
                    break;

                case '+':
                    rotation *= Quaternion.Euler(0, angle, 0);
                    break;
                case '-':
                    rotation *= Quaternion.Euler(0, -angle, 0);
                    break;
                case '&':
                    rotation *= Quaternion.Euler(angle, 0, 0);
                    break;
                case '^':
                    rotation *= Quaternion.Euler(-angle, 0, 0);
                    break;
                case '/':
                    rotation *= Quaternion.Euler(0, 0, angle);
                    break;
                case '\\':
                    rotation *= Quaternion.Euler(0, 0, -angle);
                    break;

                case '[':
                    stack.Push(new TurtleState { position = position, rotation = rotation, parent = parent });
                    break;

                case ']':
                    if (stack.Count > 0)
                    {
                        TurtleState state = stack.Pop();
                        position = state.position;
                        rotation = state.rotation;
                        parent = state.parent;
                    }
                    break;
            }
        }

        PlaceFlowersAtExtremities();
    }

    private void RandomizeRotation(Transform t)
    {
        t.localRotation *= Quaternion.Euler(
            Random.Range(-30f, 30f),
            Random.Range(0f, 360f),
            Random.Range(-30f, 30f)
        );
    }

    private void PlaceFlowersAtExtremities()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child.childCount == 0 && child != this.transform)
            {
                GameObject flower = Instantiate(flowerPrefab, child.position, child.rotation, child);
                RandomizeRotation(flower.transform);
            }
        }
    }

    void Start()
    {
        GeneratePlant();
        
    }
}
