using Nebula.Compat;
using Nebula.Game.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;

namespace Nebula.Roles.Neutral;

public class Pavlov : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Virial.Color MyColor = new(236, 182, 91);
    static public RoleTeam MyTeam = new Team("teams.pavlov", MyColor, TeamRevealType.Teams);
    public Pavlov() : base("pavlov", MyColor, RoleCategory.NeutralRole, MyTeam, [MaxFeedCountOption, FeedCoolingDownOption]) { }
    Citation? HasCitation.Citaion => Citations.SuperNewRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    private static IntegerConfiguration MaxFeedCountOption = NebulaAPI.Configurations.Configuration("options.role.pavlov.maxFeedCount", (1, 5), 3);
    private static FloatConfiguration FeedCoolingDownOption = NebulaAPI.Configurations.Configuration("options.role.pavlov.feedCoolingDown", (5f, 60f, 2.5f), 15f);

    public static Pavlov MyRole = new Pavlov();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            pavlovTeamId = (byte)arguments.Get(0, MyPlayer.PlayerId);
            leftFeedCount = arguments.Get(1, MaxFeedCountOption);
            hasDog = (byte)arguments.Get(2, byte.MaxValue);
        }
        public byte pavlovTeamId { get; private init; }
        public int leftFeedCount { get; private set; }
        private byte hasDog { get; set; }
        int[]? RuntimeAssignable.RoleArguments => [pavlovTeamId, leftFeedCount, hasDog];
        private static Image feedButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.AppointButton.png", 115f);

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p) && !IsSameTeam(p), MyColor.ToUnityColor()));
                var button = Bind(new ModAbilityButton(isLeftSideButton: true).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility));

                button.SetSprite(feedButtonSprite.GetSprite());
                button.Availability = button => myTracker.CurrentTarget != null && MyPlayer.CanMove && leftFeedCount > 0;
                button.Visibility = button => !MyPlayer.IsDead && hasDog == byte.MaxValue;
                button.OnClick = button =>
                {
                    new StaticAchievementToken("pavlov.common1");
                    var target = myTracker.CurrentTarget;
                    target!.Unbox().RpcInvokerSetRole(PavlovsDog.MyRole, [pavlovTeamId, 0]).InvokeSingle();
                    hasDog = target!.PlayerId;
                    if (--leftFeedCount <= 0) new StaticAchievementToken("pavlov.common2");
                    var leftText = button.ShowUsesIcon(3);
                    leftText.text = leftFeedCount.ToString();
                    button.StartCoolDown();
                };
                var leftText = button.ShowUsesIcon(3);
                leftText.text = leftFeedCount.ToString();
                button.CoolDownTimer = Bind(new Timer(FeedCoolingDownOption).Start());
                button.SetLabel("feed");
            }
        }

        void RuntimeAssignable.OnInactivated()
        {
            PavlovsDog.Instance.RpcSetOwnerDied.Invoke(hasDog);
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.PavlovWin 
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && IsSameTeam(p)));

        public bool IsSameTeam(Virial.Game.Player player)
        {
            return (player.Role is PavlovsDog.Instance dog && dog.pavlovTeamId == pavlovTeamId) ||
                (player.Role is Instance owner && owner.pavlovTeamId == pavlovTeamId);
        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (hasDog == byte.MaxValue) return;
            if (Helpers.GetPlayer(hasDog).GetModInfo()?.IsDead ?? true || Helpers.GetPlayer(hasDog).GetModInfo()?.Role.Role != PavlovsDog.MyRole)
            {
                hasDog = byte.MaxValue;
            }
        }

        [Local]
        void OnGameEnd(PlayerCheckWinEvent ev)
        {
            if (ev.IsWin && leftFeedCount <= 0) new StaticAchievementToken("pavlov.challenge");
        }

        [Local]
        void OnDied(PlayerDieEvent ev)
        {
            if (ev.Player.PlayerId == MyPlayer.PlayerId)
                PavlovsDog.Instance.RpcSetOwnerDied.Invoke(hasDog);
        }

        [Local]
        void DecorateSidekickColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(ev.Player)) ev.Color = MyRole.RoleColor;
        }

        [OnlyMyPlayer]
        void DecorateJackalColor(PlayerDecorateNameEvent ev)
        {
            var myInfo = PlayerControl.LocalPlayer.GetModInfo();
            if (myInfo == null) return;

            if (IsSameTeam(myInfo)) ev.Color = MyRole.RoleColor;
        }
    }
}

public class PavlovsDog : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public PavlovsDog() : base("pavlovsDog", Pavlov.MyColor, RoleCategory.NeutralRole, Pavlov.MyTeam, [CanKillHidingPlayerOption, KillCoolDownOption, KillCoolDownWhenOwnerDiedOption, SuicideTimeOption, ResetSuicideTimerAfterMeetingOption], false, optionHolderPredicate: () => ((DefinedRole)Pavlov.MyRole).IsSpawnable)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Pavlov.MyRole.ConfigurationHolder!]);
        ConfigurationHolder!.Title = ConfigurationHolder.Title.WithComparison("role.pavlov.pavlovsDog.name");
    }
    Citation? HasCitation.Citaion => Citations.SuperNewRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    private static BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.pavlovsDog.canKillHidingPlayer", false);
    private static IRelativeCoolDownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.pavlovsDog.killCoolDown", CoolDownType.Immediate, (2.5f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), 0f, (0.125f, 2f, 0.125f), 1f);
    private static IRelativeCoolDownConfiguration KillCoolDownWhenOwnerDiedOption = NebulaAPI.Configurations.KillConfiguration("options.role.pavlovsDog.killCoolDownWhenOwnerDied", CoolDownType.Ratio, (2.5f, 60f, 2.5f), 17.5f, (-40f, 40f, 2.5f), -7.5f, (0.125f, 2f, 0.125f), 0.625f);
    private static FloatConfiguration SuicideTimeOption = NebulaAPI.Configurations.Configuration("options.role.pavlovsDog.suicideTime", (2.5f, 60f, 2.5f), 40f, FloatConfigurationDecorator.Second);
    private static BoolConfiguration ResetSuicideTimerAfterMeetingOption = NebulaAPI.Configurations.Configuration("options.role.pavlovsDog.resetSuicideTimerAfterMeeting", true);

    public static PavlovsDog MyRole = new PavlovsDog();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            pavlovTeamId = (byte)arguments.Get(0, MyPlayer.PlayerId);
            hasMad = arguments.Get(1, 0);

            // hasMad = 1;
        }
        public byte pavlovTeamId { get; private init; }
        public int hasMad { get; private set; }
        int[]? RuntimeAssignable.RoleArguments => [pavlovTeamId, hasMad];
        ModAbilityButton? button = null!;
        private Timer? killTimer = null!;
        private Timer? suicideTimer = null!;
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SerialKillerSuicideButton.png", 100f);
        private int killCount = 0;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                killCount = 0;
                suicideTimer = Bind(new Timer(SuicideTimeOption).Reset());

                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p) && !IsSameTeam(p), Pavlov.MyColor.ToUnityColor(), CanKillHidingPlayerOption));
                button = Bind(new ModAbilityButton(isArrangedAsKillButton: true).KeyBind(Virial.Compat.VirtualKeyInput.Kill));
                button.Availability = button => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                button.Visibility = button => !MyPlayer.IsDead;
                button.OnClick = button =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);

                    button.StartCoolDown();
                };
                killTimer = Bind(new Timer(KillCoolDownOption.CoolDown).SetAsKillCoolDown());
                button.CoolDownTimer = Bind(killTimer.Start());
                button.SetLabel("kill");
                button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);

                var suicideButton = Bind(new ModAbilityButton(true));
                suicideButton.SetSprite(buttonSprite.GetSprite());
                suicideButton.Availability = (button) => true;
                suicideButton.Visibility = (button) => !MyPlayer.IsDead && hasMad > 0;
                suicideButton.CoolDownTimer = Bind(suicideTimer);
                suicideButton.SetLabel("serialKillerSuicide");
            }
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.PavlovWin
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && IsSameTeam(p)));

        private bool IsSameTeam(Virial.Game.Player player)
        {
            return (player.Role is Instance dog && dog.pavlovTeamId == pavlovTeamId) ||
                (player.Role is Pavlov.Instance owner && owner.pavlovTeamId == pavlovTeamId);
        }

        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (hasMad > 0 && !MyPlayer.IsDead && !suicideTimer!.IsProgressing && !AmongUsUtil.InMeeting)
            {
                MyPlayer.Suicide(PlayerState.Suicide, null, Virial.Game.KillParameter.NormalKill);
                suicideTimer.Pause().Reset();
                new StaticAchievementToken("pavlovsDog.another1");
            }
            else if (hasMad > 0 && !MyPlayer.IsDead && suicideTimer!.IsProgressing && !AmongUsUtil.InMeeting)
            {
                suicideTimer.Resume();
            }
            else if (MyPlayer.IsDead)
            {
                suicideTimer!.Pause().Reset();
            }

            if (hasMad > 0) return;
            if (!NebulaGameManager.Instance!.AllPlayerInfo().Any((p) => !p.IsDead && IsSameTeam(p) && p.Role.Role == Pavlov.MyRole))
                RpcSetOwnerDied.Invoke(MyPlayer.PlayerId);
        }


        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            suicideTimer!.Pause();
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (hasMad > 0)
            {
                if (ResetSuicideTimerAfterMeetingOption)
                    suicideTimer!.Start();
                else
                    suicideTimer!.Resume();
            }
        }

        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (hasMad > 0 && ev.Player.MyKiller?.PlayerId == MyPlayer.PlayerId && ev.Player.PlayerState == PlayerStates.Dead)
            {
                suicideTimer!.Start();
                if (++killCount >= 3) new StaticAchievementToken("pavlovsDog.challenge");
            }
        }

        private void SetMad()
        {
            hasMad = 1;
            killTimer?.SetRange(0f, KillCoolDownWhenOwnerDiedOption.CoolDown);
            killTimer?.SetTime(0f);
        }

        public static readonly RemoteProcess<byte> RpcSetOwnerDied = new(
            "SetOwnerDied",
            (message, _) => {
                var MyPlayer = PlayerControl.LocalPlayer.GetModInfo();
                if (MyPlayer == null) return;
                if (MyPlayer.PlayerId == message)
                    (MyPlayer.Role as Instance)?.SetMad();
            }
        );
    }
}