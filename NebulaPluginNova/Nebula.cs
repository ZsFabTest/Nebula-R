global using BepInEx.Unity.IL2CPP.Utils.Collections;
global using Il2CppInterop.Runtime;
global using Nebula.Extensions;
global using Nebula.Utilities;
global using Nebula.Game;
global using Nebula.Player;
global using Nebula.Modules;
global using Nebula.Configuration;
global using UnityEngine;
global using Nebula.Modules.ScriptComponents;
global using System.Collections;
global using HarmonyLib;
global using Virial.Attributes;
global using Virial.Helpers;
global using Timer = Nebula.Modules.ScriptComponents.Timer;
global using Color = UnityEngine.Color;
global using GUIWidget = Virial.Media.GUIWidget;
global using GUI = Nebula.Modules.GUIWidget.NebulaGUIWidgetEngine;
global using Image = Virial.Media.Image;
global using GamePlayer = Virial.Game.Player;

using BepInEx;
using BepInEx.Unity.IL2CPP;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using Virial;
using Cpp2IL.Core.Extensions;
using System.Reflection;
using System.Reflection.Metadata;

[assembly: System.Reflection.AssemblyFileVersionAttribute(Nebula.NebulaPlugin.PluginEpochStr + "."  + Nebula.NebulaPlugin.PluginBuildNumStr)]

namespace Nebula;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class NebulaPlugin : BasePlugin
{
    public const string AmongUsVersion = "2024.9.4s";
    public const string PluginGuid = "com.github.zsfabtest.nebular";
    public const string PluginName = "NebulaOnTheShip-Remake";
    public const string PluginVersion = "2.0.3";

    //public const string VisualVersion = "v2.0.3";
    public const string VisualVersion = "Snapshot 24w41a";

    public const string PluginEpochStr = "104";
    public const string PluginBuildNumStr = "1133";
    public static readonly int PluginEpoch = int.Parse(PluginEpochStr);
    public static readonly int PluginBuildNum = int.Parse(PluginBuildNumStr);
    public const bool GuardVanillaLangData = false;

    static public HttpClient HttpClient
    {
        get
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Nebula Updater");
            }
            return httpClient;
        }
    }
    static private HttpClient? httpClient = null;

    
    public static new NebulaLog Log { get; private set; } = new();


    public static string GetNebulaVersionString()
    {
        return "NoS-R " + VisualVersion;
    }

    static public Harmony Harmony = new Harmony(PluginGuid);


    public bool IsPreferential => Log.IsPreferential;
    public static NebulaPlugin MyPlugin { get; private set; } = null!;
    public static BasePlugin LoaderPlugin = null!;

    public override void Load()
    {
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.API.NAudio.Core.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.API.NAudio.Wasapi.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.API.NAudio.WinMM.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.API.OpusDotNet.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.API.NebulaAPI.dll")!.ReadBytes());

        CheckRegionInfo();

        Harmony.PatchAll();

        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)((scene, loadMode) =>
        {
            new GameObject("NebulaManager").AddComponent<NebulaManager>();
        });
        SetUpNebulaImpl();

        Debug.Log("Listeners:");
        foreach (var listener in BepInEx.Logging.Logger.Listeners) Debug.Log(listener.GetType().Name);
        Debug.Log("Sources:");
        foreach (var source in BepInEx.Logging.Logger.Sources) Debug.Log(source.SourceName);
    }

    static private void SetUpNebulaImpl()
    {
        NebulaAPI.instance = new NebulaImpl();
    }

    private static void CheckRegionInfo()
    {
        // if(!File.Exists(Path.Combine(Paths.GameRootPath, "RegionInfo") + "/regionInfo.json"))
        // {
            var bytes = StreamHelper.OpenFromResource("Nebula.Resources.RegionInfo.regionInfo.json")!.ReadBytes();
            if (!Directory.Exists(Path.Combine(Paths.GameRootPath, "RegionInfo")))
                Directory.CreateDirectory(Path.Combine(Paths.GameRootPath, "RegionInfo"));
            var fileStream = File.Create(Path.Combine(Paths.GameRootPath, "RegionInfo") + "/regionInfo.json");
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Close();
        // }
    }
}


[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
public static class AmongUsClientAwakePatch
{
    public static bool IsFirstFlag = true;
    
    public static void Postfix(AmongUsClient __instance)
    {
        if (!IsFirstFlag) return;
        IsFirstFlag = false;

        Language.OnChangeLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);

        __instance.StartCoroutine(VanillaAsset.CoLoadAssetOnTitle().WrapToIl2Cpp());


    }
}

[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
public static class AmBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}