using System.Collections.Generic;
using UnityEngine;

public class L_SystemGenerator : MonoBehaviour
{
    public string axiom = "F";
    public int iterations = 5;


    private Dictionary<char, List<string>> rules = new Dictionary<char, List<string>>();

        private void Awake()
        {
            List<string[]> ruleSets = new List<string[]> {
                new string[] { "F[+F]L[-F]L", "F[+F]L", "L[-F]F" },
                new string[] { "L[+F]F[+F]", "F[-F]L[-F]", "F[+F[-L]]F" },
                new string[] { "F[+L[-F]]F", "F[+F]L[-F]", "F[-F[+F]]L" },
                new string[] { "F[+F]L[+F[-F]]", "F[-L]F[+F]", "F[+F[-F]L]F" }
            };

            string[] selectedSet = ruleSets[Random.Range(0, ruleSets.Count)];
            rules['F'] = new List<string>(selectedSet);
        }   

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