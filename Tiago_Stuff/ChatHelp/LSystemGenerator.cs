using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystemGenerator
{
    public static string Generate(LSystemRuleSet ruleSet)
    {
        string current = ruleSet.axiom;
        for (int i = 0; i < ruleSet.iterations; i++)
        {
            StringBuilder next = new StringBuilder();
            foreach (char c in current)
            {
                string key = c.ToString();
                if (ruleSet.rules.ContainsKey(key))
                {
                    next.Append(ChooseRule(ruleSet.rules[key]));
                }
                else
                {
                    next.Append(c); // unchanged
                }
            }
            current = next.ToString();
        }
        return current;
    }

    private static string ChooseRule(List<Rule> rules)
    {
        float r = UnityEngine.Random.value;
        float cumulative = 0f;
        foreach (var rule in rules)
        {
            cumulative += rule.probability;
            if (r <= cumulative)
                return rule.result;
        }
        return rules[^1].result; // fallback
    }
}
