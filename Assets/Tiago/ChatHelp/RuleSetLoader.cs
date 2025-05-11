using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class RuleSetLoader : MonoBehaviour
{
    public string fileName = "plant_rules.json";
    public LSystemRuleSet ruleSet;

    [System.Serializable]
    private class RuleListWrapper
    {
        public string axiom;
        public float angle;
        public int iterations;
        public Dictionary<string, List<Rule>> rules;
    }

    void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var wrapper = JsonConvert.DeserializeObject<RuleListWrapper>(json);
            ruleSet = new LSystemRuleSet
            {
                axiom = wrapper.axiom,
                angle = wrapper.angle,
                iterations = wrapper.iterations,
                rules = wrapper.rules
            };
        }
        else
        {
            Debug.LogError("Rules file not found: " + path);
        }
    }
}
