using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
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
[NebulaRPCHolder]
public static class AddChat
{
    internal static int Cannel = 0x1;
    private static TMPro.TextMeshPro message = null!;
    private static Dictionary<byte, int> cannels = null!;
    public static void Initialize()
    {
        if (!GeneralConfigurations.UseBubbleChatOption) return;
        CleanUp();
        Cannel = 0x1;
        message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
        new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(4f, 0.72f) }.Reflect(message);
        message.transform.localPosition = new Vector3(0.75f, 2.5f, -5f);
        message.gameObject.SetActive(true);
        message.text = GetCannelName();
        cannels = new();
        HudManager.Instance.Chat.SetVisible(false);
    }

    public static void CleanUp()
    {
        if (message != null) message.gameObject.SetActive(false);
        message = null!;
        if (cannels != null) cannels.Clear();
        cannels = null!;
        Cannel = 0x1;
    }

    private static bool GetExtraMessageChecker(Virial.Game.Player sourcePlayer)
    {
        if (sourcePlayer == null) return false;
        int sourcePlayerCannel = int.MaxValue;
        if (cannels.TryGetValue(sourcePlayer.PlayerId, out sourcePlayerCannel) && sourcePlayerCannel != Cannel) return false;
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

    private static string GetCannelName()
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
        if (!CheckEnableChat())
        {
            Cannel = 0x1;
            RpcSetCannel.Invoke((PlayerControl.LocalPlayer.PlayerId, Cannel));
            HudManager.Instance.Chat.SetVisible(false);
            if (message != null) message.text = GetCannelName();
        }

        if (NebulaInput.GetInput(Virial.Compat.VirtualKeyInput.ChangeChatCannel).KeyDownForAction)
        {
            do
            {
                Cannel <<= 1;
                if (Cannel == (int)ChatCannel.TurnBack) Cannel = 0x1;
            } while (!CheckEnableChat());
            if (message != null) message.text = GetCannelName();
            Debug.Log(Cannel);
            HudManager.Instance.Chat.SetVisible(false);
            RpcSetCannel.Invoke((PlayerControl.LocalPlayer.PlayerId, Cannel));
        }
    }

    private static bool CheckEnableChat()
    {
        var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
        if (MyPlayer == null) return false;

        switch (Cannel)
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

    private readonly static RemoteProcess<(byte playerId, int cannel)> RpcSetCannel = new("SetCannel", (message, _) =>
    {
        if (cannels != null) cannels[message.playerId] = message.cannel;
    });
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EnableChat
{
    public static void Postfix(HudManager __instance)
    {
        if (!GeneralConfigurations.UseBubbleChatOption) return;
        
        if (AddChat.Cannel != (int)ChatCannel.Default && !__instance.Chat.isActiveAndEnabled)
            __instance.Chat.SetVisible(true);
    }
}