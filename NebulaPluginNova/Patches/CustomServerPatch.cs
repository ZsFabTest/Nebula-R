using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula.Behaviour;
using UnityEngine.Events;

namespace Nebula.Patches;

[HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.OnEnable))]
public static class RegionMenuOnEnablePatch
{
    public static void Postfix(RegionMenu __instance)
    {
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
    }
}

