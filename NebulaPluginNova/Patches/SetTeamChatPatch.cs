/*
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
        if (!GeneralConfigurations.UseBubbleChatOption || !hasChat()) return;

        if (!__instance.Chat.isActiveAndEnabled)
            __instance.Chat.SetVisible(true);
        //else if (!hasChat() && MeetingHud.Instance == null && LobbyBehaviour.Instance == null) __instance.Chat.SetVisible(false);
    }
}

*/

using AmongUs.Data;
using Nebula.Compat;
using Nebula.Roles.Modifier;
using UnityEngine;
using Virial.Assignable;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;

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

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChatNote))]
public static class ChatControllerAddChatNotePatch
{
    public static bool Prefix([HarmonyArgument(0)] NetworkedPlayerInfo srcPlayer, [HarmonyArgument(1)] ChatNoteTypes noteType)
    {
        return noteType != ChatNoteTypes.DidVote;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class AddChat
{
    private static bool GetExtraMessageChecker(Virial.Game.Player sourcePlayer)
    {
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null) return false;
        return MyPlayer.TryGetModifier<Lover.Instance>(out var LoverModifier) && (sourcePlayer?.TryGetModifier<Lover.Instance>(out _) ?? false) && LoverModifier.MyLover?.PlayerId == sourcePlayer.PlayerId;
    }

    private static void CreateChatBubble(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        ChatBubble chatBubble = __instance.GetPooledBubble();
        try
        {
            chatBubble.transform.SetParent(__instance.scroller.Inner);
            chatBubble.transform.localScale = Vector3.one;
            bool isSamePlayer = sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId;
            if (isSamePlayer)
            {
                chatBubble.SetRight();
            }
            else
            {
                chatBubble.SetLeft();
            }

            Color seeColor = (sourcePlayer.GetModInfo().IsImpostor && PlayerControl.LocalPlayer.GetModInfo().IsImpostor) ? NebulaTeams.ImpostorTeam.Color.ToUnityColor() : Color.white;

            bool didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);

            chatBubble.SetCosmetics(sourcePlayer.Data);

            __instance.SetChatBubbleName(
                chatBubble, sourcePlayer.Data, sourcePlayer.GetModInfo().IsDead,
                didVote, seeColor, null);
            chatBubble.SetText(chatText);

            chatBubble.AlignChildren();
            __instance.AlignAllBubbles();

            if (!__instance.IsOpenOrOpening && __instance.notificationRoutine == null)
            {
                __instance.notificationRoutine = __instance.StartCoroutine(__instance.BounceDot());
            }
            if (!isSamePlayer)
            {
                SoundManager.Instance.PlaySound(
                    __instance.messageSound, false, 1f).pitch = 0.5f + (float)sourcePlayer.PlayerId / 15f;
                __instance.chatNotification.SetUp(sourcePlayer, chatText);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            Debug.LogError(ex.StackTrace);
            __instance.chatBubblePool.Reclaim(chatBubble);
        }
    }

    private static void CheckIfNeedToAdd(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        Debug.LogError(1);
        var questEvent = new PlayerAddChatEvent(sourcePlayer.GetModInfo()!, chatText, false);
        GameOperatorManager.Instance?.Run(questEvent);
        if (questEvent.isVanillaShow)
        {
            CreateChatBubble(__instance, sourcePlayer, questEvent.chatText);
        }
    }

    public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] ref string chatText)
    {
        //if (AssassinSystem.isAssassinMeeting && (!PlayerControl.LocalPlayer.GetModInfo()!.IsImpostor || !(sourcePlayer.GetModInfo()?.IsImpostor ?? false))) return false;
        if (__instance != HudManager.Instance.Chat || !GeneralConfigurations.UseBubbleChatOption) return true;
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null || sourcePlayer.GetModInfo() == null) return true;
        bool shouldSeeMessage = (NebulaGameManager.Instance?.CanSeeAllInfo ?? false) || sourcePlayer.PlayerId == MyPlayer.PlayerId;

        shouldSeeMessage = GetExtraMessageChecker(sourcePlayer.GetModInfo()!) || shouldSeeMessage;
        if (!MyPlayer.IsDead && sourcePlayer.Data.IsDead) CheckIfNeedToAdd(__instance, sourcePlayer, chatText);
        if (DateTime.UtcNow - MeetingStart.MeetingStartTime < TimeSpan.FromSeconds(1))
        {
            return shouldSeeMessage;
            //return false;
        }
        //return false;
        return MeetingHud.Instance != null || LobbyBehaviour.Instance != null || shouldSeeMessage;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EnableChat
{
    public static void Postfix(HudManager __instance)
    {
        /*
        if (AmongUsUtil.InMeeting && AssassinSystem.isAssassinMeeting && !PlayerControl.LocalPlayer.GetModInfo()!.IsImpostor)
        {
            __instance.Chat.SetVisible(false);
            return;
        }
        */
        if (!GeneralConfigurations.UseBubbleChatOption || !(PlayerControl.LocalPlayer.GetModInfo()?.TryGetModifier<Lover.Instance>(out _) ?? false)) return;

        if (!__instance.Chat.isActiveAndEnabled)
            __instance.Chat.SetVisible(true);
        //else if (!hasChat() && MeetingHud.Instance == null && LobbyBehaviour.Instance == null) __instance.Chat.SetVisible(false);
    }
}