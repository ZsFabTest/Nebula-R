﻿using BepInEx;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula.Behaviour;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nebula.Patches;

[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
[HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
public static class RegionMenuOpenPatch
{
    private static StringDataEntry SaveIp = null!;
    private static IntegerDataEntry SavePort = null!;

    private static TextField? ipField = null;
    private static TextField? portField = null;

    public static IRegionInfo[] defaultRegions = null!;

    private static DataSaver customServerData = new DataSaver("CustomServer");
    private static StaticHttpRegionInfo CustomRegion = null!;

    public static void UpdateRegions()
    {
        ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
        IRegionInfo[] regions = defaultRegions;

        //var CustomRegion = new DnsRegionInfo(SaveIp.Value, "Custom", StringNames.NoTranslation, SaveIp.Value, (ushort)SavePort.Value, false);

        CustomRegion = new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, SaveIp.Value,
            new ServerInfo[] { new ServerInfo("Custom", SaveIp.Value, (ushort)SavePort.Value, false) });

        regions = regions.Concat(new IRegionInfo[] { CustomRegion.Cast<IRegionInfo>() }).ToArray();
        //マージ時、DefaultRegionsに含まれている要素のほうが優先される(重複時に生き残る方)
        ServerManager.DefaultRegions = regions;
        serverManager.LoadServers();

    }

    static RegionMenuOpenPatch()
    {
        SaveIp = new StringDataEntry("ServerIp", customServerData, "");
        SavePort = new IntegerDataEntry("ServerPort", customServerData, 22023);

        defaultRegions = ServerManager.DefaultRegions;
        UpdateRegions();
    }

    private static void ChooseOption(RegionMenu __instance, IRegionInfo region)
    {

        DestroyableSingleton<ServerManager>.Instance.SetRegion(region);
        __instance.RegionText.text = DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(region.TranslateName, region.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[0]));
    }

    public static void Postfix(RegionMenu __instance)
    {
        if (!__instance.TryCast<RegionMenu>()) return;

        if (!ipField)
        {
            Reference<TextField> ipRef = new();
            var widget = new MetaWidgetOld.TextInput(1, 2f, new(2.8f, 0.3f))
            {
                TextFieldRef = ipRef,
                TextPredicate = (c) => ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9') || c is '?' or '!' or ',' or '.' or '/' or ':',
                Hint = "Server IP".Color(Color.gray),
                DefaultText = SaveIp.Value
            };
            widget.Generate(__instance.gameObject, new Vector3(-2.5f, -1.2f, -100f), out _);

            ipField = ipRef.Value!;
            ipField.LostFocusAction = (text) =>
            {
                while (text.EndsWith('/')) text = text.Substring(0, text.Length - 1);
                ipField.SetText(text);
                SaveIp.Value = text;
                UpdateRegions();
                ChooseOption(__instance, CustomRegion.Cast<IRegionInfo>());
            };
        }

        if (!portField)
        {
            Reference<TextField> portRef = new();
            var widget = new MetaWidgetOld.TextInput(1, 2f, new(2.8f, 0.3f))
            {
                TextFieldRef = portRef,
                TextPredicate = (c) => '0' <= c && c <= '9',
                Hint = "Server Port".Color(Color.gray),
                DefaultText = SavePort.Value.ToString()
            };
            widget.Generate(__instance.gameObject, new Vector3(-2.5f, -1.8f, -100f), out _);

            portField = portRef.Value!;
            portField.LostFocusAction = (text) =>
            {
                SavePort.Value = ushort.TryParse(text, out var port) ? port : (ushort)22023;
                UpdateRegions();
                ChooseOption(__instance, CustomRegion.Cast<IRegionInfo>());
            };
        }
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
                //入力中のカスタムサーバーの情報を確定させたうえでサーバーを選択する
                TextField.ChangeFocus(null);

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
        RegionMenuScroller.SetYBoundsMax(__instance.ButtonPool.poolSize * 0.25f);
        RegionMenuScroller.allowY = true;
        RegionMenuScroller.allowX = false;
    }
}


[HarmonyPatch(typeof(ServerManager), nameof(ServerManager.LoadServers))]
public static class LoadServersPatch
{
    public static void Prefix(ServerManager __instance)
    {
        //Debug.LogError(__instance.serverInfoFileJson);
        try
        {
            if (ClientOption.AllOptions[ClientOption.ClientOptionType.UseStandaloneServerList].Value == 1)
                __instance.serverInfoFileJson = Path.Combine(Paths.GameRootPath, "RegionInfo", "regionInfo.json");
            else
                __instance.serverInfoFileJson = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\..\\LocalLow\\Innersloth\\Among Us\\regionInfo.json";
        }
        catch { }
        // C:\Users\a1234\AppData\LocalLow\Innersloth\Among Us\regionInfo.json
        // __instance.serverInfoFileJson = ClientOption.AllOptions[ClientOption.ClientOptionType.UseStandaloneServerList].Value == 1 ?
    }
}
