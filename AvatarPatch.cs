using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

class AvatarOffset
{
    public float scale = 1;
    public float height = 1;
}

public class AvatarPatch
{
    const string assetBundleFileName = "head";
    const string steamIdFileName = "steamid.txt";
    const string headContainerTransformPath = "head_model/default";

    public static void NetworkInstantiatePostfix(Avatar __instance)
    {
        Debug.Log("[CustomAvatar] NetworkInstantiate.Postfix");

        bool insertOwnHead = CustomAvatarPlugin.InsertOwnHead.Value;

        if (__instance.isMe && !insertOwnHead)
        {
            Debug.Log("[CustomAvatar] Skipping because it is me");
            return;
        }
        
        InsertCustomHead(__instance);
    }

    public static string StripNonAlpha(string input)
    {
        return Regex.Replace(input, "[^a-zA-Z]", "");
    }

    static Transform GetImmediateChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    static Renderer DeleteExistingHeadAndReturn(Avatar __instance)
    {
        Debug.Log($"[CustomAvatar] Deleting existing head");

        var headContainer = __instance.head.Find(headContainerTransformPath);

        var oldHeadRenderer = headContainer.GetComponent<Renderer>();

        GameObject.Destroy(oldHeadRenderer);

        foreach (Transform child in headContainer)
        {
            GameObject.Destroy(child.gameObject);
        }

        Debug.Log("[CustomAvatar] Delete success");

        return oldHeadRenderer;
    }

    static Vector3 SetWorldScale(Transform transform, Vector3 desiredWorldScale)
    {
        var currentWorldScale = transform.lossyScale;
        var parentWorldScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        var newLocalScale = new Vector3(
            desiredWorldScale.x / parentWorldScale.x,
            desiredWorldScale.y / parentWorldScale.y,
            desiredWorldScale.z / parentWorldScale.z
        );

        transform.localScale = newLocalScale;

        return newLocalScale;
    }

    static void PrintAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Debug.Log(GetRelativePath(child, parent));
            PrintAllChildren(child);
        }
    }

    static string GetRelativePath(Transform target, Transform root)
    {
        if (target == root)
            return root.name;

        var path = target.name;
        var current = target.parent;

        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return root.name + "/" + path;
    }

    static void InsertCustomHead(Avatar __instance)
    {
        var oldHeadRenderer = DeleteExistingHeadAndReturn(__instance);

        string userName = __instance.player.NickName;
        string steamId = __instance.player.UserId; // empty when offline
        string cleanedName = StripNonAlpha(userName);

        Debug.Log($"[CustomAvatar] Inserting custom avatar for player {userName} ({steamId})...");

        string avatarsRoot = Path.Combine(Application.dataPath, "../Avatars");

        if (!Directory.Exists(avatarsRoot))
        {
            Debug.Log($"[CustomAvatar] Avatars directory does not exist, creating...");

            Directory.CreateDirectory(avatarsRoot);

            Debug.Log("[CustomAvatar] All done");
            return;
        }

        string targetPath = null;

        if (!string.IsNullOrEmpty(steamId))
        {
            foreach (string dir in Directory.GetDirectories(avatarsRoot))
            {
                string steamIdFile = Path.Combine(dir, steamIdFileName);
                if (File.Exists(steamIdFile))
                {
                    string contents = File.ReadAllText(steamIdFile).Trim();

                    if (contents.Contains(steamId))
                    {
                        targetPath = Path.Combine(dir, assetBundleFileName);
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.Log("[CustomAvatar] Steam ID is empty");
        }

        if (targetPath == null)
        {
            Debug.Log($"[CustomAvatar] Could not find an avatar directory containing steam ID - switching to name");

            string cleanedNameDir = Path.Combine(avatarsRoot, cleanedName);

            if (!Directory.Exists(cleanedNameDir))
            {
                Debug.Log($"[CustomAvatar] Creating empty directory with steam ID for later usage: {cleanedNameDir}");

                Directory.CreateDirectory(cleanedNameDir);

                if (!string.IsNullOrEmpty(steamId))
                {
                    File.WriteAllText(Path.Combine(cleanedNameDir, steamIdFileName), steamId);
                }
                else
                {
                    Debug.Log("[CustomAvatar] Cannot store steam ID - it is empty");
                }
                
                Debug.Log("[CustomAvatar] All done");
                return;
            }

            Debug.Log($"[CustomAvatar] Found a (cleaned) steam name match");

            if (!string.IsNullOrEmpty(steamId))
            {
                Debug.Log("[CustomAvatar] Storing their steam ID for later...");

                File.WriteAllText(Path.Combine(cleanedNameDir, steamIdFileName), steamId);
            }
            else
            {
                Debug.Log("[CustomAvatar] Cannot store steam ID - it is empty");
            }

            targetPath = Path.Combine(cleanedNameDir, assetBundleFileName);
        }

        Debug.Log("[CustomAvatar] Attempting to load AssetBundle from: " + targetPath);

        if (File.Exists(targetPath))
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(targetPath);
            if (bundle != null)
            {
                Debug.Log("[CustomAvatar] AssetBundle successfully loaded:");

                string[] assets = bundle.GetAllAssetNames();
                foreach (string assetName in assets)
                {
                    Debug.Log("[CustomAvatar]  - " + assetName);
                }

                Debug.Log("[CustomAvatar] Searching for the first prefab inside the AssetBundle...");

                string prefabPath = Array.Find(assets, a => a.EndsWith(".prefab"));

                if (prefabPath != null)
                {
                    GameObject loadedPrefab = bundle.LoadAsset<GameObject>(prefabPath);

                    if (loadedPrefab != null)
                    {
                        Debug.Log("[CustomAvatar] Inserting prefab...");

                        var headContainer = __instance.head.Find(headContainerTransformPath);

                        GameObject newGameObject = UnityEngine.Object.Instantiate(loadedPrefab, headContainer, false);
                        newGameObject.name = "InjectedHeadPrefab";

                        AvatarOffset offset = LoadOffset(Path.GetDirectoryName(targetPath));

                        float xPos = offset.height - 1f;
                        Vector3 newScale = Vector3.one * offset.scale;
                        
                        newGameObject.transform.localPosition = new Vector3(xPos, 0, 0);
                        newGameObject.transform.localRotation = Quaternion.Euler(0, -90f, 270f);

                        var appliedLocalScale = SetWorldScale(newGameObject.transform, newScale);
                        
                        Debug.Log($"[CustomAvatar] Height {offset.height} => {xPos}, scale {offset.scale} => {newScale} => {appliedLocalScale}");

                        if (__instance.isMe)
                        {
                            Debug.Log($"[CustomAvatar] Is me so forcing switch around");

                            Transform headWrapper = __instance.head.Find("head_model");

                            headWrapper.position = new Vector3(headWrapper.position.x, headWrapper.position.y, headWrapper.position.z + 1f);
                            headWrapper.rotation = Quaternion.Euler(0, 270f, 0);
                        }

                        StoreRenderersAndMaterialsWithAvatar(__instance, newGameObject, oldHeadRenderer);

                        Debug.Log("[CustomAvatar] Inserted prefab under head: " + newGameObject.name);

                        Debug.Log("[CustomAvatar] Avatar done!");
                    }
                    else
                    {
                        Debug.LogWarning("[CustomAvatar] Prefab not found or failed to load: " + prefabPath);
                    }
                }
                else
                {
                    Debug.LogWarning("[CustomAvatar] No prefab found in asset bundle");
                }

                bundle.Unload(false);

                Debug.Log("[CustomAvatar] AssetBundle unloaded");
            }
            else
            {
                Debug.LogWarning("[CustomAvatar] Failed to load AssetBundle (null)");
            }
        }
        else
        {
            Debug.LogWarning("[CustomAvatar] AssetBundle not found at: " + targetPath);
        }
    }

    static void StoreRenderersAndMaterialsWithAvatar(Avatar __instance, GameObject newGameObject, Renderer oldRendererToRemove)
    {
        Debug.Log("[CustomAvatar] Updating renderers and materials...");

        var newRenderers = newGameObject.GetComponentsInChildren<Renderer>(true);

        Debug.Log($"[CustomAvatar] Storing {newRenderers.Length} new renderers...");

        var rendererList = new List<Renderer>(__instance.renderers);
        rendererList.RemoveAll(r => r == null || r == oldRendererToRemove);
        rendererList.AddRange(newRenderers);
        
        Debug.Log($"[CustomAvatar] All renderers:");

        foreach (var renderer in rendererList) {
            Debug.Log(renderer);
        }

        // note: cannot replace as other important meshes like hands are here
        __instance.renderers = rendererList.ToArray();
        
        Debug.Log($"[CustomAvatar] Stored {rendererList.Count} total renderers");

        Debug.Log($"[CustomAvatar] Storing materials...");
        
        var materialsField = typeof(Avatar).GetField("materials", BindingFlags.Instance | BindingFlags.NonPublic);
        var renderQueuesField = typeof(Avatar).GetField("renderQueues", BindingFlags.Instance | BindingFlags.NonPublic);

        if (materialsField?.GetValue(__instance) is List<Material> materialsList &&
            renderQueuesField?.GetValue(__instance) is Dictionary<Material, int> renderQueues)
        {
            if (oldRendererToRemove != null)
            {
                Debug.Log($"[CustomAvatar] Removing old renderer materials...");

                foreach (var mat in oldRendererToRemove.sharedMaterials)
                {
                    Debug.Log(mat);
                    materialsList.Remove(mat);
                }

                Debug.Log($"[CustomAvatar] Done");
            }
            else
            {
                Debug.LogWarning($"[CustomAvatar] No old renderer to remove");
            }

            foreach (var renderer in newRenderers)
            {
                Debug.Log($"[CustomAvatar] Adding materials for renderer '{renderer.gameObject.name}'...");

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (!materialsList.Contains(mat))
                    {
                        Debug.Log(mat);
                        materialsList.Add(mat);
                    }

                    renderQueues[mat] = mat.renderQueue;
                }
                
                Debug.Log($"[CustomAvatar] Done");
            }
        }
        else
        {
            Debug.LogWarning("[CustomAvatar] Failed to access private material fields");
        }

        Debug.Log("[CustomAvatar] Store complete");
    }

    static AvatarOffset LoadOffset(string avatarDirectory)
    {
        string configPath = Path.Combine(avatarDirectory, "offset.txt");

        var offset = new AvatarOffset();

        if (!File.Exists(configPath)) {
            Debug.Log("[CustomAvatar] Offset file does not exist, creating with defaults...");

            File.WriteAllText(configPath, "scale=1\nheight=1");
            return offset;
        }

        Debug.Log("[CustomAvatar] Loading offset from " + configPath);

        try
        {
            foreach (var line in File.ReadAllLines(configPath))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim().ToLower();
                if (!float.TryParse(parts[1].Trim(), out float value)) continue;

                switch (key)
                {
                    case "scale": offset.scale = value; break;
                    case "height": offset.height = value; break;
                    default:
                        Debug.LogWarning($"[CustomAvatar] Unknown offset key '{key}'");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CustomAvatar] Failed to parse offset.txt: {ex.Message}");
        }

        return offset;
    }
}
