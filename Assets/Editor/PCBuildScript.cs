using UnityEditor;
using UnityEngine;
using System.IO;

public class PCBuildScript
{
    [MenuItem("Build/PC Build")]
    public static void BuildPC()
    {
        // Set build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "../build/PC/FallingBlocks.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        // Create build directory if it doesn't exist
        string buildDir = Path.GetDirectoryName(buildPlayerOptions.locationPathName);
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        // Build the player
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        Debug.Log("PC Build Complete: " + buildPlayerOptions.locationPathName);
    }

    [MenuItem("Build/PC Build (Development)")]
    public static void BuildPCDevelopment()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "../build/PC/FallingBlocks_Dev.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;

        string buildDir = Path.GetDirectoryName(buildPlayerOptions.locationPathName);
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        Debug.Log("PC Development Build Complete: " + buildPlayerOptions.locationPathName);
    }
}