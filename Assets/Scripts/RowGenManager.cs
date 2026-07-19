using UnityEngine;
using System.IO;
using Newtonsoft.Json;


public class RowGenManager : MonoBehaviour
{
    [Header("JSON Data")]
    // public TextAsset jsonFile;
    private string jsonFileName = "patterns.json"; // Path to the JSON file
    private PatternArray patternArray;

    [Header("Row Generation Settings")]
    public int maxCellValue = 1;            // Linked to the GameManager's number of tiles -1
    private int newestCrackColumn;
    public float erosionChance = 0.2f;
    private bool using_patterns = false;
    private int currentIntervalLimit = -1;
    private int rowsSinceLastPattern = 0;
    public struct PatternUsage
    {
        public Pattern pattern;
        public int lastUsedRow;
    }
    private PatternUsage lastUsedPattern;

    [Header("Board Data")]
    public int boardWidth;

    // Always start from interval generation, then switch to pattern usage after a random number of rows
    void Start()
    {
        ReadFromJson();

        // give a random value to newestCrackColumn
        newestCrackColumn = Random.Range(2, boardWidth - 2);
        using_patterns = false;
        currentIntervalLimit = Random.Range(patternArray.min_interval, patternArray.max_interval + 1);

        lastUsedPattern = new PatternUsage
        {
            pattern = null,
            lastUsedRow = -1
        };
    }

    private void ReadFromJson()
    {
        // if (jsonFile == null)
        // {
        //     Debug.LogError("JSON file is not assigned!");
        //     return;
        // }

        // // patternArray = JsonUtility.FromJson<PatternArray>(jsonFile.text);
        // patternArray = JsonConvert.DeserializeObject<PatternArray>(jsonFile.text);


        // if (patternArray == null || patternArray.patterns == null)
        // {
        //     Debug.LogError("Failed to parse JSON!");
        //     return;
        // }

        // Debug.Log($"Successfully loaded {patternArray.pattern_count} patterns.");

        // patternArray.DebugPrint();

        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"JSON file not found at path: {filePath}");
            return;
        }

        string jsonContent = File.ReadAllText(filePath);

        patternArray = JsonConvert.DeserializeObject<PatternArray>(jsonContent);

        if (patternArray == null || patternArray.patterns == null)
        {
            Debug.LogError("Failed to parse JSON!");
            return;
        }

        Debug.Log($"Successfully loaded {patternArray.pattern_count} patterns.");

        patternArray.DebugPrint();

    }

    private int[] CrackTheRow(int[] row)
    {
        // 1: get new crack column by one off
        newestCrackColumn += Random.Range(-1, 2);
        newestCrackColumn = Mathf.Clamp(newestCrackColumn, 0, boardWidth - 1);

        // 2: crack the row at the new column
        row[newestCrackColumn] = 0;

        // 3: apply erosion chance to adjacent columns
        if (newestCrackColumn > 0 && Random.value < erosionChance)
        {
            row[newestCrackColumn - 1] = 0;
        }
        if (newestCrackColumn < boardWidth - 1 && Random.value < erosionChance)
        {
            row[newestCrackColumn + 1] = 0;
        }

        // Implementation for cracking the row
        return row;
    }

    private int[] GenerateIntervalRow()
    {
        // Implementation for generating a row based on interval logic
        int[] row = new int[boardWidth];
        // get a rwo of intervals within 1 and maxCellValue
        int cellValue = Random.Range(1, maxCellValue + 1);
        for (int i = 0; i < boardWidth; i++)
        {
            row[i] = cellValue;
        }

        return CrackTheRow(row);
    }

    // go from top to bottom of a pattern
    private int[] GetRowFromPattern()
    {
        // Implementation for extracting integers from a PatternUsage
        int[] row = new int[boardWidth];

        // get row from the selected pattern based on the last used row
        row = lastUsedPattern.pattern.shape[lastUsedPattern.lastUsedRow];
        lastUsedPattern.lastUsedRow++;              // If all rows of the pattern have been used, reset in GetNewRow
    
        // Apply cracking logic if the pattern has a crack
        if (lastUsedPattern.pattern.have_crack)
        {
            row = CrackTheRow(row);
        }

        return row; // Placeholder for actual pattern extraction logic
    }

    private Pattern DeepCopyPattern(Pattern original)
    {
        Pattern copy = new Pattern
        {
            name = original.name,
            have_crack = original.have_crack,
            cols = original.cols,
            rows = original.rows,
            shape = new int[original.rows][]
        };

        for (int i = 0; i < original.rows; i++)
        {
            copy.shape[i] = new int[original.cols];
            for (int j = 0; j < original.cols; j++)
            {
                copy.shape[i][j] = original.shape[i][j];
            }
        }

        return copy;
    }

    // Provides a new row based on the current state and pattern usage
    public int[] GetNewRow()
    {
        Debug.Log($"Board Width: {boardWidth}, Max Cell Value: {maxCellValue}, Using Patterns: {using_patterns}, Rows Since Last Pattern: {rowsSinceLastPattern}, Current Interval Limit: {currentIntervalLimit}");
        int[] returnRow = new int[boardWidth];
        if (using_patterns && patternArray != null && patternArray.patterns.Length > 0)
        {
            // Logic to select and use a pattern
            returnRow =  GetRowFromPattern();

            if (lastUsedPattern.lastUsedRow >= lastUsedPattern.pattern.rows)
            {
                // Switch to interval generation after the pattern is fully used
                using_patterns = false;
                rowsSinceLastPattern = 0;
                currentIntervalLimit = Random.Range(patternArray.min_interval, patternArray.max_interval + 1);
            }
        }
        else
        {
            // Logic to generate a row based on interval logic
            returnRow = GenerateIntervalRow();

            // Check if we should switch to pattern usage
            rowsSinceLastPattern++;
            if (rowsSinceLastPattern >= currentIntervalLimit)
            {
                using_patterns = true;
                // select a new pattern for the next time
                lastUsedPattern.pattern = DeepCopyPattern(patternArray.patterns[Random.Range(0, patternArray.patterns.Length)]);
                lastUsedPattern.lastUsedRow = 0;
            }
        }
        return returnRow;
        
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