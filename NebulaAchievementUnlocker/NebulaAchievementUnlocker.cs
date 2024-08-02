using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Nebula.Modules;
using UnityEngine;

namespace NebulaAchievementUnlocker
{

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class NebulaAchievementUnlocker : BasePlugin
    {
        public const string AmongUsVersion = "2024.6.28s";
        public const string PluginGuid = "com.github.zsfabtest.nebulaachievementunlocker";
        public const string PluginName = "nau";
        public const string PluginVersion = "0.0.1";
        public static Harmony Harmony = new(PluginGuid); 

        public override void Load()
        {
            Harmony.PatchAll();
            Debug.Log("Unlocker has been loaded.");
        }
    }
}

[HarmonyPatch(typeof(ProgressRecord),nameof(ProgressRecord.IsCleared),MethodType.Getter)]
public class AchievementUnlocker
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
