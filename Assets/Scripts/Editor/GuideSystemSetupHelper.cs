using UnityEngine;
using UnityEditor;

public class GuideSystemSetupHelper : EditorWindow
{
    [MenuItem("Window/Guide Blocks System/Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<GuideSystemSetupHelper>("Guide System Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Guide Blocks System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This system adds preview of upcoming blocks.");
        GUILayout.Label("Follow these steps:");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Open Setup Guide"))
        {
            string guidePath = "Assets/Scripts/SETUP_GUIDE.md";
            if (System.IO.File.Exists(guidePath))
            {
                System.Diagnostics.Process.Start(guidePath);
            }
            else
            {
                Debug.LogWarning("Setup guide not found at: " + guidePath);
            }
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Quick Checks:");
        
        // Check for GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            EditorGUILayout.HelpBox("GameManager not found in scene.", MessageType.Warning);
            if (GUILayout.Button("Create GameManager GameObject"))
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                Selection.activeGameObject = gmObj;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("GameManager found ✓", MessageType.Info);
        }
        
        // Check for GuideBlocksManager
        GuideBlocksManager gbm = FindAnyObjectByType<GuideBlocksManager>();
        if (gbm == null)
        {
            EditorGUILayout.HelpBox("GuideBlocksManager not found in scene.", MessageType.Warning);
            if (GUILayout.Button("Create GuideBlocksManager GameObject"))
            {
                GameObject gbmObj = new GameObject("GuideBlocksManager");
                gbmObj.AddComponent<GuideBlocksManager>();
                Selection.activeGameObject = gbmObj;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("GuideBlocksManager found ✓", MessageType.Info);
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Common Issues:");
        GUILayout.Label("1. Missing Tilemap references");
        GUILayout.Label("2. Empty tetrominoTiles array");
        GUILayout.Label("3. Guide board outside camera view");
        GUILayout.Label("4. Input system not set up");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run Quick Test"))
        {
            RunQuickTest();
        }
    }
    
    void RunQuickTest()
    {
        Debug.Log("=== Running Guide System Quick Test ===");
        
        // Check scripts
        bool allScriptsExist = true;
        
        string[] scriptPaths = {
            "Assets/Scripts/GameManager.cs",
            "Assets/Scripts/GuideBlocksManager.cs",
            "Assets/Scripts/Tetromino.cs"
        };
        
        foreach (string path in scriptPaths)
        {
            if (System.IO.File.Exists(path))
            {
                Debug.Log($"✓ {System.IO.Path.GetFileName(path)} found");
            }
            else
            {
                Debug.LogError($"✗ {System.IO.Path.GetFileName(path)} missing");
                allScriptsExist = false;
            }
        }
        
        if (allScriptsExist)
        {
            Debug.Log("All required scripts are present ✓");
        }
        
        // Check scene objects
        GameManager gm = FindAnyObjectByType<GameManager>();
        GuideBlocksManager gbm = FindObjectOfType<GuideBlocksManager>();
        
        if (gm != null && gbm != null)
        {
            Debug.Log("Both managers found in scene ✓");
            
            // Check if they're linked
            if (gm.guideManager == gbm)
            {
                Debug.Log("GuideManager reference is set ✓");
            }
            else
            {
                Debug.LogWarning("GuideManager reference not set in GameManager");
            }
        }
        
        Debug.Log("=== Quick Test Complete ===");
    }
}