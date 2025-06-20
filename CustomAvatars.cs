using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

[BepInPlugin("com.imagitama.customavatars", "CustomAvatars", "1.0.0")]
public class CustomAvatarPlugin : BaseUnityPlugin
{
    public static ConfigEntry<bool> InsertOwnHead;

    private void Awake()
    {
        InsertOwnHead = Config.Bind("General", "InsertOwnHead", false, "Insert your own head for testing");
        
        Debug.Log("[CustomAvatar] Patching...");

        var harmony = new Harmony("com.imagitama.customavatars");
        
        var patch = new AvatarPatch();

        var originalNetworkInstantiate = AccessTools.Method(typeof(Avatar), "NetworkInstantiate");
        var postfix = typeof(AvatarPatch).GetMethod("NetworkInstantiatePostfix");
        harmony.Patch(originalNetworkInstantiate, postfix: new HarmonyMethod(postfix));

        Debug.Log("[CustomAvatar] Avatar patch applied");
    }
}
