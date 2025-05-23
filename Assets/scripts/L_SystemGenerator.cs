using System.Collections.Generic;
using UnityEngine;

public class L_SystemGenerator : MonoBehaviour
{
    [Header("L-System Settings")]
    public string axiom = "F";
    public int iterations = 5;

    private Dictionary<char, string[]> ruleSets;

    private void Awake()
    {
        InitializeRules();
    }

    private void InitializeRules()
    {
        ruleSets = new Dictionary<char, string[]>
        {
            { 'F', new string[]
                {
                    "F[+F]L[-F]L",
                    "F[+F]L",
                    "L[-F]F",
                    "L[+F]F[+F]",
                    "F[-F]L[-F]",
                    "F[+F[-L]]F",
                    "F[+L[-F]]F",
                    "F[-F[+F]]L",
                    "F[+F]L[+F[-F]]",
                    "F[-L]F[+F]",
                    "F[+F[-F]L]F"
                }
            }
        };
    }

    public string GeneratePlant()
    {
        string current = axiom;

        for (int i = 0; i < iterations; i++)
        {
            string next = "";

            foreach (char symbol in current)
            {
                if (ruleSets.ContainsKey(symbol))
                {
                    string[] options = ruleSets[symbol];
                    next += options[Random.Range(0, options.Length)];
                }
                else
                {
                    next += symbol.ToString();
                }
            }

            current = next;
        }

        return current;
    }
}
