using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class IronWolfHeadTool : EditorWindow
{
    const string headsPath = "Assets/Heads";
    const string outputPath = "Assets/AssetBundles";
    const string assetBundleName = "head";

    [MenuItem("Tools/IronWolf Head Tool")]
    public static void ShowWindow()
    {
        GetWindow<IronWolfHeadTool>("IronWolf Head Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("IronWolf Head Tool", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        });

        GUILayout.Label("");
        GUILayout.Label($"Create AssetBundles for the IronWolf VR Custom Avatars mod: https://github.com/imagitama/ironwolf-custom-avatars-mod");

        GUILayout.Label("");
        GUILayout.Label($"Step 1: Follow README to export your FBX and place it into '{headsPath}/your_steam_username/whatever.fbx' (use any name)");

        GUILayout.Label("");
        GUILayout.Label("Step 2: Place FBX mesh into your scene and customise with materials, textures and sub-meshes");

        GUILayout.Label("");
        GUILayout.Label($"Step 3: Create a Unity prefab inside '{headsPath}/your_steam_username/whatever.prefab' (use any name)");
        
        GUILayout.Label("");
        GUILayout.Label("Step 4: Click the button to automatically create AssetBundles using the first prefab it can find");

        GUILayout.Label("");
        GUILayout.Label($"Outputs to '{outputPath}'");
        
        GUILayout.Label("");

        var paths = GetPathsToBundleSourceDirs();

        if (paths.Length == 0) {
            GUILayout.Label("Nothing detected");
        }

        foreach (var dirPath in paths) {
            var dirName = Path.GetFileName(dirPath);

            GUILayout.Label("Found: " + dirName);
        }
        
        GUILayout.Label("");
        
        if (GUILayout.Button("Create AssetBundles"))
        {
            BuildBundlesForUsers();
        }
        
        GUILayout.Label("");

        if (GUILayout.Button("Open AssetBundles Folder")) {
            var outputPathAbsolute = Path.Combine(Application.dataPath, "../", outputPath);
            outputPathAbsolute = outputPathAbsolute.Replace("/", "\\");
            System.Diagnostics.Process.Start("explorer.exe", "\"" + outputPathAbsolute + "\"");
        }
    }

    static string[] GetPathsToBundleSourceDirs() {
        if (!Directory.Exists("Assets/Heads")) {
            return new string[0];
        }
        return Directory.GetDirectories("Assets/Heads");
    }

    static void BuildBundlesForUsers() {
        foreach (var dirPath in GetPathsToBundleSourceDirs()) {
            var dirName = Path.GetFileName(dirPath);
            BuildBundlesForUser(dirName);
        }
    }

    private static void BuildBundlesForUser(string username)
    {
        Debug.Log($"Building bundle '{username}'...");

        string bundleDir = Path.Combine(headsPath, username);

        if (!Directory.Exists(bundleDir))
        {
            Debug.LogError($"Directory not found: {bundleDir}");
            return;
        }

        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        string[] assetPaths = Directory.GetFiles(bundleDir, "*", SearchOption.AllDirectories);
        List<string> validAssets = new List<string>();

        foreach (var assetPath in assetPaths)
        {
            if (!assetPath.EndsWith(".meta") && !Directory.Exists(assetPath))
            {
                Debug.Log($"Adding asset '{assetPath}'");
                validAssets.Add(assetPath.Replace("\\", "/"));
            }
        }

        if (validAssets.Count == 0) {
            Debug.Log("No valid assets found");
            return;
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = assetBundleName.ToLower(),
            assetNames = validAssets.ToArray()
        };

        Debug.Log($"Adding build '{build.assetBundleName}'");

        builds.Add(build);

        string userOutputPath = Path.Combine(outputPath, username);

        if (!Directory.Exists(userOutputPath)) {
            Directory.CreateDirectory(userOutputPath);
        }

        BuildPipeline.BuildAssetBundles(userOutputPath, builds.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        Debug.Log("AssetBundles built successfully.");
    }
}
