using UnityEngine;
using System.Collections.Generic;

public class RowGenManager : MonoBehaviour
{
    [Header("JSON Data")]
    public TextAsset jsonFile;

    private PatternArray patternArray;

    void Start()
    {
        ReadFromJson();
    }

    private void ReadFromJson()
    {
        if (jsonFile == null)
        {
            Debug.LogError("JSON file is not assigned!");
            return;
        }

        patternArray = JsonUtility.FromJson<PatternArray>(jsonFile.text);

        if (patternArray == null || patternArray.patterns == null)
        {
            Debug.LogError("Failed to parse JSON!");
            return;
        }

        Debug.Log($"Successfully loaded {patternArray.pattern_count} patterns.");

        patternArray.DebugPrint();
    }

    [System.Serializable]
    public class PatternArray
    {
        public int pattern_count;
        public int min_interval;
        public int max_interval;
        public Pattern[] patterns;

        public void DebugPrint()
        {
            Debug.Log($"=== Pattern Data Loaded ===");
            Debug.Log($"Total Patterns: {pattern_count}, Interval: {min_interval}-{max_interval}");

            if (patterns != null)
            {
                foreach (var p in patterns)
                    p.DebugPrint();
            }
        }
    }

    [System.Serializable]
    public class Pattern
    {
        public string name;
        public bool have_crack;
        public int cols;
        public int rows;
        public int[][] shape;        // This works reliably with JsonUtility

        public void DebugPrint()
        {
            Debug.Log($"Pattern: {name} | {rows}x{cols} | Crack: {have_crack}");

            if (shape != null && shape.Length > 0)
            {
                Debug.Log("Shape:");
                for (int i = 0; i < shape.Length; i++)
                {
                    string row = "";
                    for (int j = 0; j < shape[i].Length; j++)
                    {
                        row += shape[i][j] + " ";
                    }
                    Debug.Log(row);
                }
            }
            else
            {
                Debug.Log("Shape data is null or empty!");
            }
        }
    }
}