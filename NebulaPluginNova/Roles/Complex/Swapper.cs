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
    internal static bool WillSwap = false;
    internal static byte targetId1, targetId2;

    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.SwapIcon.png", 115f);
    static public void OnMeetingStart(int leftSwap, Action ExtraAction)
    {
        //Debug.LogError("SetUpSwapSystem");
        temp_target = null;
        WillSwap = false;
        targetId1 = byte.MaxValue;
        targetId2 = byte.MaxValue;
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(targetSprite,
            state => {
                if (WillSwap) return;
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
                    targetId1 = temp_target.PlayerId;
                    targetId2 = state.MyPlayer.PlayerId;
                    WillSwap = true;
                    temp_target = null;
                    leftSwap--;
                    ExtraAction.Invoke();
                    RpcSetSwap.Invoke((targetId1, targetId2));
                }
            },
            p => !p.MyPlayer.IsDead && leftSwap > 0
                && (!WillSwap || p.MyPlayer.PlayerId == targetId1 || p.MyPlayer.PlayerId == targetId2)
                && !PlayerControl.LocalPlayer.Data.IsDead
            ));
    }

    public static void OnModCalcuateVotes(ModCalcuateVotesEvent ev)
    {
        if(WillSwap && !(NebulaAPI.CurrentGame?.LocalPlayer.IsDead ?? true))
        {
            if (!ev.VoteResult.ContainsKey(targetId1)) ev.VoteResult[targetId1] = 0;
            if (!ev.VoteResult.ContainsKey(targetId2)) ev.VoteResult[targetId2] = 0;
            (ev.VoteResult[targetId1], ev.VoteResult[targetId2]) = (ev.VoteResult[targetId2], ev.VoteResult[targetId1]);
        }
    }

    public static void OnPopulateResult(MeetingPopulateResultEvent ev)
    {
        if (WillSwap && !(NebulaAPI.CurrentGame?.LocalPlayer.IsDead ?? true))
        {
            PlayerVoteArea? swapped1 = ev.Instance.playerStates.FirstOrDefault(area => area.TargetPlayerId == targetId1);
            PlayerVoteArea? swapped2 = ev.Instance.playerStates.FirstOrDefault(area => area.TargetPlayerId == targetId2);
            if (swapped1 == null || swapped2 == null) return;
            ev.Instance.StartCoroutine(Effects.Slide3D(swapped1.transform, swapped1.transform.localPosition, swapped2.transform.localPosition, 1.5f));
            ev.Instance.StartCoroutine(Effects.Slide3D(swapped2.transform, swapped2.transform.localPosition, swapped1.transform.localPosition, 1.5f));
        }
    }

    public static void OnExiled(Virial.Game.Player MyPlayer, Virial.Game.Player Exiled)
    {
        if (MyPlayer.AmOwner)
        {
            if (MyPlayer.PlayerId == Exiled.PlayerId) new StaticAchievementToken("swapper.another");
            if (WillSwap && Exiled.PlayerId == targetId1 || Exiled.PlayerId == targetId2) new StaticAchievementToken("swapper.common");
        }
        if (WillSwap && Exiled.AmOwner) new StaticAchievementToken("swapper.challenge");
    }

    private static readonly RemoteProcess<(byte, byte)> RpcSetSwap = new(
        "SetSwap",
        (message, isCalledByMe) =>
        {
            if (isCalledByMe) return;
            WillSwap = true;
            targetId1 = message.Item1;
            targetId2 = message.Item2;
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

        [Local]
        void OnModCalculateVotes(ModCalcuateVotesEvent ev) => SwapSystem.OnModCalcuateVotes(ev);
        [Local]
        void OnPopulateResult(MeetingPopulateResultEvent ev) => SwapSystem.OnPopulateResult(ev);

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += $" ({leftSwap})".Color(MyNiceRole.RoleColor.ToUnityColor());
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

        [Local]
        void OnModCalculateVotes(ModCalcuateVotesEvent ev) => SwapSystem.OnModCalcuateVotes(ev);
        [Local]
        void OnPopulateResult(MeetingPopulateResultEvent ev) => SwapSystem.OnPopulateResult(ev);

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += $" ({leftSwap})".Color(MyNiceRole.RoleColor.ToUnityColor());
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev) => SwapSystem.OnExiled(MyPlayer, ev.Player);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }
}
