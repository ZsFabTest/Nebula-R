using Epic.OnlineServices.Presence;
using Il2CppSystem.Reflection;
using Nebula.Behaviour;
using Nebula.Compat;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;

namespace Nebula.Roles.Complex;

[NebulaRPCHolder]
static internal class SwapSystem
{
    static Virial.Game.Player? temp_target = null;
    internal static List<(byte, byte)> SwapInfos = new();
    static bool hasSwapped = false;

    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.SwapIcon.png", 115f);
    static public void OnMeetingStart(int leftSwap, Action ExtraAction)
    {
        //Debug.LogError("SetUpSwapSystem");
        temp_target = null;
        hasSwapped = false;
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(targetSprite,
            state => {
                if (hasSwapped) return;
                if (temp_target == null)
                {
                    temp_target = state.MyPlayer;
                    state.MyButton.renderer.color = Color.green;
                }
                else if (temp_target.PlayerId == state.MyPlayer.PlayerId)
                {
                    temp_target = null;
                    state.MyButton.renderer.color = Color.white;
                }
                else
                {
                    state.MyButton.renderer.color = Color.green;
                    RpcSetSwap.Invoke((temp_target.PlayerId, state.MyPlayer.PlayerId));
                    hasSwapped = true;
                    temp_target = null;
                    leftSwap--;
                    ExtraAction.Invoke();
                }
            },
            p => !p.MyPlayer.IsDead && leftSwap > 0
                && (!hasSwapped || SwapInfos.Any((info) => info.Item1 == p.MyPlayer.PlayerId || info.Item2 == p.MyPlayer.PlayerId))
                && !PlayerControl.LocalPlayer.Data.IsDead
            ));
    }

    public static void OnExiled(Virial.Game.Player MyPlayer, Virial.Game.Player Exiled)
    {
        if (SwapInfos.Any((info) => info.Item1 == Exiled.PlayerId || info.Item2 == Exiled.PlayerId)) new StaticAchievementToken("swapper.common");
        if (Exiled.AmOwner) new StaticAchievementToken("swapper.challenge");
    }

    private static readonly RemoteProcess<(byte, byte)> RpcSetSwap = new(
    "SetSwap",
        (message, _) =>
        {
            SwapInfos.Add(message);
        });
}

public class Swapper : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public bool IsEvil { get; private set; }

    Citation? HasCitation.Citaion => Citations.TheOtherRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => IsEvil ? new EvilInstance(player, arguments) : new NiceInstance(player, arguments);

    // Options
    private static IntegerConfiguration NumOfSwapOption = NebulaAPI.Configurations.Configuration("options.role.swapper.numOfSwap", (1, 10), 5);
    private static IntegerConfiguration EvilNumOfSwapOption = NebulaAPI.Configurations.Configuration("options.role.swapper.numOfSwap", (1, 10), 2);
    private static BoolConfiguration CanCallEmergencyMeetingOption = NebulaAPI.Configurations.Configuration("options.role.swapper.canCallEmergencyMeeting", false);

    private Swapper(bool isEvil) : base(
        isEvil ? "evilSwapper" : "niceSwapper",
        isEvil ? new(Palette.ImpostorRed) : new(128, 36, 52),
        isEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole,
        isEvil ? Impostor.Impostor.MyTeam : Crewmate.Crewmate.MyTeam,
        [isEvil ? EvilNumOfSwapOption : NumOfSwapOption, CanCallEmergencyMeetingOption])
    {
        IsEvil = isEvil;
        ConfigurationHolder?.AddTags(ConfigurationTags.TagChaotic);
        ConfigurationHolder?.ScheduleAddRelated(() => [isEvil ? MyNiceRole.ConfigurationHolder! : MyEvilRole.ConfigurationHolder!]);
    }

    public static Swapper MyNiceRole = new(false);
    public static Swapper MyEvilRole = new(true);

    public class NiceInstance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyNiceRole;
        public NiceInstance(Virial.Game.Player player, int[] arguments) : base(player) 
        {
            // 调整初始换票数量
            leftSwap = arguments.Get(0, NumOfSwapOption);
        }
        private int leftSwap = 0;
        int[]? RuntimeAssignable.RoleArguments => [leftSwap];

        void RuntimeAssignable.OnActivated() { }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => SwapSystem.OnMeetingStart(leftSwap, () => leftSwap--);

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner) name += $" ({leftSwap})".Color(MyNiceRole.RoleColor.ToUnityColor());
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev) => SwapSystem.OnExiled(MyPlayer, ev.Player);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }

    public class EvilInstance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyEvilRole;
        public EvilInstance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            // 调整初始换票数量
            leftSwap = arguments.Get(0, NumOfSwapOption);
        }
        private int leftSwap = 0;
        int[]? RuntimeAssignable.RoleArguments => [leftSwap];

        void RuntimeAssignable.OnActivated() { }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => SwapSystem.OnMeetingStart(leftSwap, () => leftSwap--);

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner) name += $" ({leftSwap})".Color(MyNiceRole.RoleColor.ToUnityColor());
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev) => SwapSystem.OnExiled(MyPlayer, ev.Player);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;

    }
}
