using System.Collections.Generic;
using UnityEngine;

public class L_SystemGenerator : MonoBehaviour
{
    public string axiom = "F";
    public int iterations = 5;

    private Dictionary<char, List<string>> rules = new Dictionary<char, List<string>>() {
        { 'F', new List<string> { "F[+F]F[-F]F", "F[+F]F", "F[-F]F" } }
    };

    public string GeneratePlant()
    {
        string current = axiom;

        for (int i = 0; i < iterations; i++)
        {
            string next = "";
            foreach (char c in current)
            {
                if (rules.ContainsKey(c))
                {
                    List<string> options = rules[c];
                    next += options[Random.Range(0, options.Count)];
                }
                else
                {
                    next += c.ToString();
                }
            }
            current = next;
        }

        return current;
    }
}
