using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using System.Linq.Expressions;
using UnityEngine.Events;
using System.Reflection;
using Cpp2IL.Core.Extensions;

namespace AUServerTool;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public class AUServerToolPlugin : BasePlugin
{
    public const string AmongUsVersion = "2024.8.13s";
    public const string PluginGuid = "com.github.zsfabtest.aust";
    public const string PluginName = "Among Us Server Manage Tool";
    public const string PluginVersion = "1.0.0";

    public override void Load()
    {
        new Harmony(PluginGuid).PatchAll();
        if (!File.Exists(Path.Combine(Paths.GameRootPath, "RegionInfo") + "/regionInfo.json"))
        {
            var bytes = Assembly.GetExecutingAssembly().GetManifestResourceStream("AUST.Resources.regionInfo.json")!.ReadBytes();
            if (!Directory.Exists(Path.Combine(Paths.GameRootPath, "RegionInfo")))
                Directory.CreateDirectory(Path.Combine(Paths.GameRootPath, "RegionInfo"));
            var fileStream = File.Create(Path.Combine(Paths.GameRootPath, "RegionInfo") + "/regionInfo.json");
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Close();
        }
    }
}

public static class Il2CppHelpers
{
    private static class CastHelper<T> where T : Il2CppObjectBase
    {
        public static Func<IntPtr, T> Cast;
        static CastHelper()
        {
            var constructor = typeof(T).GetConstructor(new[] { typeof(IntPtr) });
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            Cast = lambda.Compile();
        }
    }

    public static T CastFast<T>(this Il2CppObjectBase obj) where T : Il2CppObjectBase
    {
        if (obj is T casted) return casted;
        return obj.Pointer.CastFast<T>();
    }

    public static T CastFast<T>(this IntPtr ptr) where T : Il2CppObjectBase
    {
        return CastHelper<T>.Cast(ptr);
    }
}

[HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.OnEnable))]
public static class RegionMenuOnEnablePatch
{
    public static void Postfix(RegionMenu __instance)
    {
        DestroyableSingleton<ServerManager>.Instance.LoadServers();
        foreach (var button in __instance.ButtonPool.activeChildren)
        {
            var serverButton = button.CastFast<ServerListButton>();
            serverButton.Button.OnClick.RemoveAllListeners();
            serverButton.Button.OnClick.AddListener((UnityAction)(() =>
            {
                var region = ServerManager.Instance.AvailableRegions.FirstOrDefault(region => region.Name.Equals(serverButton.textTranslator.defaultStr));
                if (region != null) __instance.ChooseOption(region);
            }));

        }

        Transform back = __instance.ButtonPool.transform.FindChild("Backdrop");
        back.transform.localScale *= 10f;
        var oldScroller = __instance.ButtonPool.transform.parent.gameObject.GetComponent<Scroller>;
        if (oldScroller != null) oldScroller = null;
        Scroller RegionMenuScroller = __instance.ButtonPool.transform.parent.gameObject.AddComponent<Scroller>();
        RegionMenuScroller.Inner = __instance.ButtonPool.transform;
        RegionMenuScroller.MouseMustBeOverToScroll = true;
        RegionMenuScroller.ClickMask = back.GetComponent<BoxCollider2D>();
        RegionMenuScroller.ScrollWheelSpeed = 0.5f;
        RegionMenuScroller.SetYBoundsMin(0f);
        RegionMenuScroller.SetYBoundsMax(__instance.ButtonPool.poolSize * 0.222f);
        RegionMenuScroller.allowY = true;
        RegionMenuScroller.allowX = false;
    }
}

[HarmonyPatch(typeof(ServerManager), nameof(ServerManager.LoadServers))]
public static class LoadServersPatch
{
    public static void Prefix(ServerManager __instance)
    {
        __instance.serverInfoFileJson = Path.Combine(Paths.GameRootPath, "RegionInfo", "regionInfo.json");
    }
}
