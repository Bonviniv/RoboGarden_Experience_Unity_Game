using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Splines;


public class TreeGenerator : MonoBehaviour
{
    private string tree;
    [SerializeField] private string axiom;
    [SerializeField] private int iterations;
    Stack<TransformationInfoHelp> stack = new Stack<TransformationInfoHelp>();
    Stack<int> splineIndexStack = new Stack<int>();
    private TransformationInfoHelp helper;
    [SerializeField] private float length;
    [SerializeField] private float angleMin;
    [SerializeField] private float angleMax;
    [SerializeField] private float angleYMin;
    [SerializeField] private float angleYMax;
    private List<List<Vector3>> lineList = new List<List<Vector3>>();
    [SerializeField] private Material treeMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tree = axiom;
        Debug.Log("Starting tree " + tree);
        ExpandTreeString();
        CreateMesh();
    }

    private void OnDrawGizmos()
    {
        foreach (List<Vector3> line in lineList)
        {
            Gizmos.DrawLine(line[0], line[1]);
        }
    }

    void ExpandTreeString()
    {
        string expandedTree;

        for (int i = 0; i < iterations; i++)
        {
            expandedTree = "";
            foreach (char j in tree)
            {
                switch (j) {
                    case 'F':
                        // Introducing somo randomness to the tree -> sometimes it doubles the lenght when goes foward
                        if (Random.Range(0f, 100f) < 50f) {
                            expandedTree += "F";
                        } else {
                            expandedTree += "FF";
                        }
                        break;
                    
                    case 'B':
                        if (Random.Range(0f, 100f) < 50f){
                            expandedTree += "[llFB][rFB]";
                        } else {
                            expandedTree += "[lFB][rrFB]";
                        }
                        break;
                    
                    default:
                        expandedTree += j.ToString();
                        break;
                }
            }
            tree = expandedTree;
            Debug.Log("Tree at iteration " + i + " is " + tree);
        }
    }

    void CreateMesh()
    {
        Vector3 initialPosition;

        GameObject treeObject = new GameObject("Tree");
        var meshFilter = treeObject.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        var meshRenderer = treeObject.AddComponent<MeshRenderer>();
        meshRenderer.material = treeMaterial;

        var container = treeObject.AddComponent<SplineContainer>();
        container.RemoveSplineAt(0);

        var currentSpline = container.AddSpline();
        var splineIndex = container.Splines.FindIndex(currentSpline);

        currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);

        foreach (char j in tree)
        {
            switch (j)
            {
                case 'F':
                    initialPosition = transform.position;
                    transform.position += transform.up * length;  // MOVIMENTO
                    currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);
                    break;

                case 'B':
                    // do nothing
                    break;

                case '[':
                    stack.Push(new TransformationInfoHelp()
                    {
                        position = transform.position,
                        rotation = transform.rotation
                    });
                    splineIndexStack.Push(splineIndex);

                    // Criar novo spline
                    int splineCount = currentSpline.Count;
                    int prevSplineIndex = splineIndex;
                    currentSpline = container.AddSpline();
                    splineIndex = container.Splines.FindIndex(currentSpline);
                    currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);

                    // Ligar o novo spline ao anterior
                    container.LinkKnots(new SplineKnotIndex(prevSplineIndex, splineCount - 1), new SplineKnotIndex(splineIndex, 0));
                    break;

                case ']':
                    TransformationInfoHelp helper = stack.Pop();
                    transform.position = helper.position;
                    transform.rotation = helper.rotation;
                    splineIndex = splineIndexStack.Pop();
                    currentSpline = container.Splines[splineIndex];
                    break;

                case 'l':
                    transform.Rotate(Vector3.back, Random.Range(angleMin, angleMax));
                    transform.Rotate(Vector3.back, Random.Range(angleYMin, angleYMax));
                    break;

                case 'r':
                    transform.Rotate(Vector3.forward, Random.Range(angleMin, angleMax));
                    transform.Rotate(Vector3.back, Random.Range(angleYMin, angleYMax));
                    break;
            }
        }

        // Remover splines com menos de 3 pontos (evita erro na extrusão)
        for (int i = container.Splines.Count - 1; i >= 0; i--)
        {
            if (container.Splines[i].Count < 3)
            {
                container.RemoveSplineAt(i);
            }
        }

        // Adicionar extrusão apenas se houver splines válidos
        if (container.Splines.Count > 0)
        {
            var extrude = treeObject.AddComponent<SplineExtrude>();
            extrude.Container = container;
        }
    }
}

public static class TreeGeneratorExtension
{
    public static int FindIndex(this IReadOnlyList<Spline> splines, Spline spline)
    {
        for (int i = 0; i < splines.Count; i++)
        {
            if (splines[i] == spline)
            {
                return i;
            }
        }
        return -1;
    }
}
