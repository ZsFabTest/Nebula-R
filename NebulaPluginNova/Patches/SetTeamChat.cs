using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Virial.Assignable;

namespace Nebula.Patches;

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
    private static bool GetExtraMessageChecker(Virial.Game.Player sourcePlayer, ref string chatText)
    {
        if (MeetingHud.Instance != null) return false;
        if (sourcePlayer == null) return false;
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo()!;
        var arguments = chatText.Split(' ');
        if (arguments.Get(0, "[NAC]") == "/tell")
        {
            if (!PlayerControl.AllPlayerControls.GetFastEnumerator().Any((p) => p.GetModInfo()?.Name == arguments.Get(1, "[NAP]")))
            {
                chatText = Language.Translate("chat.unknowPlayer");
                return false;
            }
            else if (arguments.Get(1, "[NAP]") == MyPlayer.Name || (NebulaGameManager.Instance?.CanSeeAllInfo ?? false))
            {
                chatText = $"{Language.Translate("chat.private")} to {arguments.Get(1,"[NAP]")}: {chatText.Split(' ',3).Get(2, "[NO MESSAGE]")}";
                return true;
            }
            return false;
        }

        switch (arguments.Get(0, "NAC"))
        {
            case "/impostor":
                if (!GeneralConfigurations.EnableImpostorCannelOption)
                {
                    chatText = Language.Translate("chat.impostorDisabled");
                    return false;
                }else if (sourcePlayer.Role.Role.Category != RoleCategory.ImpostorRole)
                {
                    chatText = Language.Translate("chat.notImpostor");
                    return false;
                }

                chatText = $"{Language.Translate("chat.impostor")}: {chatText.Split(' ', 2).Get(1, "[NO MESSAGE]")}";
                return MyPlayer.Role.Role.Category == RoleCategory.ImpostorRole && sourcePlayer.Role.Role.Category == RoleCategory.ImpostorRole;
            case "/jackal":
                if (!GeneralConfigurations.EnableJackalCannelOption)
                {
                    chatText = Language.Translate("chat.jackalDisabled");
                    return false;
                }else if (sourcePlayer.Role.Role.Team != NebulaTeams.JackalTeam && !sourcePlayer.TryGetModifier<SidekickModifier.Instance>(out _))
                {
                    chatText = Language.Translate("chat.notJackal");
                    return false;
                }

                chatText = $"{Language.Translate("chat.jackal")}: {chatText.Split(' ', 2).Get(1, "[NO MESSAGE]")}";
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
                else if (NebulaGameManager.Instance?.CanSeeAllInfo ?? false)
                    return true;
                else return false;
                    
                return ((Jackal.Instance)(jackal?.Role ?? null!))?.IsSameTeam(sourcePlayer) ?? false;
            case "/lover":
                if (!GeneralConfigurations.EnableJackalCannelOption)
                {
                    chatText = Language.Translate("chat.loverDisabled");
                    return false;
                }else if(!sourcePlayer.TryGetModifier<Lover.Instance>(out _))
                {
                    chatText = Language.Translate("chat.notLover");
                    return false;
                }

                chatText = $"{Language.Translate("chat.lover")}: {chatText.Split(' ', 2).Get(1, "[NO MESSAGE]")}";
                Lover.Instance LoverModifier = null!;
                return MyPlayer.TryGetModifier<Lover.Instance>(out LoverModifier!) && (sourcePlayer?.TryGetModifier<Lover.Instance>(out _) ?? false) && LoverModifier.MyLover?.PlayerId == sourcePlayer.PlayerId;
            case "/help":
                chatText = Language.Translate("chat.nac");
                return false;
            default:
                return false;
        }
    }

    public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] ref string chatText)
    {
        if (__instance != HudManager.Instance.Chat || !GeneralConfigurations.UseBubbleChatOption) return true;
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null) return true;
        bool shouldSeeMessage = (NebulaGameManager.Instance?.CanSeeAllInfo ?? false) || sourcePlayer.PlayerId == MyPlayer.PlayerId;

        shouldSeeMessage = GetExtraMessageChecker(sourcePlayer.GetModInfo() ?? null!, ref chatText) || shouldSeeMessage;
        if (DateTime.UtcNow - MeetingStart.MeetingStartTime < TimeSpan.FromSeconds(1))
        {
            return shouldSeeMessage;
        }
        return MeetingHud.Instance != null || LobbyBehaviour.Instance != null || shouldSeeMessage;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EnableChat
{
    private static bool hasChat()
    {
        return (GeneralConfigurations.EnableImpostorCannelOption && PlayerControl.LocalPlayer.GetModInfo()?.Role.Role.Category == RoleCategory.ImpostorRole)
            || (GeneralConfigurations.EnableJackalCannelOption && PlayerControl.LocalPlayer.GetModInfo()?.Role.Role.Team == NebulaTeams.JackalTeam)
            || (GeneralConfigurations.EnableJackalCannelOption && (PlayerControl.LocalPlayer.GetModInfo()?.TryGetModifier<SidekickModifier.Instance>(out _) ?? false))
            || (GeneralConfigurations.EnableLoverCannelOption && (PlayerControl.LocalPlayer.GetModInfo()?.TryGetModifier<Lover.Instance>(out _) ?? false));
    }

    public static void Postfix(HudManager __instance)
    {
        if (!GeneralConfigurations.UseBubbleChatOption) return;

        if (hasChat() && !__instance.Chat.isActiveAndEnabled)
            __instance.Chat.SetVisible(true);
        else if (!hasChat() && MeetingHud.Instance == null && LobbyBehaviour.Instance == null) __instance.Chat.SetVisible(false);
    }
}