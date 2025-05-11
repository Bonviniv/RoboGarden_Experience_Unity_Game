using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Rule
{
    public string result;
    public float probability;
}

[System.Serializable]
public class LSystemRuleSet
{
    public string axiom;
    public float angle;
    public int iterations;
    public Dictionary<string, List<Rule>> rules;
}