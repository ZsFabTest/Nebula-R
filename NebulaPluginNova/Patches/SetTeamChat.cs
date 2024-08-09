using JetBrains.Annotations;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using UnityEngine;
using Virial.Assignable;

namespace Nebula.Patches;

enum ChatCannel
{
    Default = 0x1,
    Impostor = 0x2,
    Jackal = 0x4,
    Lover = 0x8,
    TurnBack = 0x10
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public class MeetingStart
{
    public static DateTime MeetingStartTime = DateTime.MinValue;
    public static void Prefix(MeetingHud __instance)
    {
        MeetingStartTime = DateTime.UtcNow;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class AddChat
{
    public static int Cannel = 0x1;
    private static TMPro.TextMeshPro message = null!;
    public static void Initialize()
    {
        if (!GeneralConfigurations.UseBubbleChatOption) return;
        CleanUp();
        Cannel = 0x1;
        message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
        new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(4f, 0.72f) }.Reflect(message);
        message.transform.localPosition = new Vector3(0.5f, 2.5f, -5f);
        message.gameObject.SetActive(true);
        message.text = GetCannelName();
    }

    public static void CleanUp()
    {
        if(message != null) message.gameObject.SetActive(false);
        message = null!;
    }

    public static bool GetExtraMessageChecker(Virial.Game.Player sourcePlayer)
    {
        if (sourcePlayer == null) return false;
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo()!;
        switch (Cannel)
        {
            case (int)ChatCannel.Impostor:
                return MyPlayer.Role.Role.Category == RoleCategory.ImpostorRole && sourcePlayer.Role.Role.Category == RoleCategory.ImpostorRole;
            case (int)ChatCannel.Jackal:
                SidekickModifier.Instance sidekickModifier = null!;
                Virial.Game.Player jackal = null!;
                if (MyPlayer.TryGetModifier<SidekickModifier.Instance>(out sidekickModifier!))
                    jackal = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault((p) => p.GetModInfo()?.Role is Jackal.Instance && ((Jackal.Instance)p.GetModInfo()!.Role).IsSameTeam(MyPlayer)).GetModInfo() ?? null!;
                else if (MyPlayer.Role is Jackal.Instance)
                    return ((Jackal.Instance)MyPlayer.Role).IsSameTeam(sourcePlayer);
                else if (MyPlayer.Role is Sidekick.Instance)
                    jackal = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault((p) => p.GetModInfo()?.Role is Jackal.Instance && ((Jackal.Instance)p.GetModInfo()!.Role).IsSameTeam(MyPlayer)).GetModInfo() ?? null!;
                else if (MyPlayer.Role is SchrödingersCat.InstanceJackal)
                    return true;
                else return false;
                return ((Jackal.Instance)(jackal?.Role ?? null!))?.IsSameTeam(sourcePlayer) ?? false;
            case (int)ChatCannel.Lover:
                Lover.Instance LoverModifier = null!;
                return MyPlayer.TryGetModifier<Lover.Instance>(out LoverModifier!) && (sourcePlayer?.TryGetModifier<Lover.Instance>(out _) ?? false) && LoverModifier.MyLover?.PlayerId == sourcePlayer.PlayerId;
            default:
                return false;
        }
    }

    public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer)
    {
        if (__instance != HudManager.Instance.Chat || !GeneralConfigurations.UseBubbleChatOption) return true;
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null) return true;
        bool shouldSeeMessage = MyPlayer.IsDead || sourcePlayer.PlayerId == MyPlayer.PlayerId;

        shouldSeeMessage = shouldSeeMessage || GetExtraMessageChecker(sourcePlayer.GetModInfo() ?? null!);
        if (DateTime.UtcNow - MeetingStart.MeetingStartTime < TimeSpan.FromSeconds(1))
        {
            return shouldSeeMessage;
        }
        return MeetingHud.Instance != null || LobbyBehaviour.Instance != null || shouldSeeMessage;
    }

    public static string GetCannelName()
    {
        switch (Cannel)
        {
            case (int)ChatCannel.Default:
                return Language.Translate("chat.default");
            case (int)ChatCannel.Impostor:
                return Language.Translate("chat.impostor");
            case (int)ChatCannel.Jackal:
                return Language.Translate("chat.jackal");
            case (int)ChatCannel.Lover:
                return Language.Translate("chat.lover");
            default:
                return Language.Translate("chat.default");
        }
    }

    public static void Update()
    {
        if (NebulaInput.GetInput(Virial.Compat.VirtualKeyInput.ChangeChatCannel).KeyDownForAction)
        {
            do
            {
                Cannel <<= 1;
                if (Cannel == (int)ChatCannel.TurnBack) Cannel = 0x1;
            } while (!EnableChat.CheckEnableChat());
            if (message != null) message.text = GetCannelName();
            Debug.Log(Cannel);
            HudManager.Instance.Chat.SetVisible(false);
        }
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EnableChat
{
    public static bool CheckEnableChat()
    {
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null) return false;

        switch (AddChat.Cannel)
        {
            case (int)ChatCannel.Default:
                return true;
            case (int)ChatCannel.Impostor:
                return MyPlayer.Role.Role.Category == RoleCategory.ImpostorRole && GeneralConfigurations.EnableImpostorCannelOption;
            case (int)ChatCannel.Jackal:
                return (MyPlayer.Role.Role.Team == Jackal.MyTeam || MyPlayer.TryGetModifier<SidekickModifier.Instance>(out _)) && GeneralConfigurations.EnableJackalCannelOption;
            case (int)ChatCannel.Lover:
                return MyPlayer.TryGetModifier<Lover.Instance>(out _) && GeneralConfigurations.EnableLoverCannelOption;
            default:
                return true;
        }
    }

    public static void Postfix(HudManager __instance)
    {
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo()!;

        if (AddChat.Cannel != (int)ChatCannel.Default && CheckEnableChat() && !__instance.Chat.isActiveAndEnabled)
            __instance.Chat.SetVisible(true);
    }
}