using Nebula.Compat;
using Nebula.Game.Statistics;
using Nebula.Roles.Abilities;
using Nebula.Roles.Modifier;
using Nebula.VoiceChat;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public class Yandere : DefinedRoleTemplate, HasCitation, DefinedRole
{
    static public RoleTeam MyTeam = new Team("teams.yandere", new(200, 22, 115), TeamRevealType.OnlyMe);

    private Yandere() : base("yandere", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption, ProtectTimeAfterMeetingOption, CheckMaxDistanceOption, MaxStandableTimeOption]) { }

    Citation? HasCitation.Citaion => RemakeInit.Citations.ExtremeRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments);

    static private IRelativeCoolDownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.yandere.killCoolDown", CoolDownType.Immediate, (0.5f, 60f, 0.5f), 1.5f, (-40f, 40f, 0.5f), -23.5f, (0.0625f, 2f, 0.0625f), 0.0625f);
    static public FloatConfiguration ProtectTimeAfterMeetingOption = NebulaAPI.Configurations.Configuration("options.role.yandere.protectTimeAfterMeeting", (0f, 30f, 2.5f), 7.5f, FloatConfigurationDecorator.Second);
    static public FloatConfiguration CheckMaxDistanceOption = NebulaAPI.Configurations.Configuration("options.role.yandere.checkMaxDistance", (0.25f, 5f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static public FloatConfiguration MaxStandableTimeOption = NebulaAPI.Configurations.Configuration("options.role.yandere.maxStandableTime", (2.5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);

    static public Yandere MyRole = new Yandere();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player, int[] arguments) : base(player)
        {
            Target = Helpers.GetPlayer((byte)arguments.Get(0, System.Random.Shared.Next(0, NebulaGameManager.Instance?.AllPlayersNum ?? 1))).GetModInfo();
        }
        private Virial.Game.Player? Target = null;
        List<TrackingArrowAbility> arrows = new();

        bool RuntimeRole.CanUseVent => true;
        int[]? RuntimeAssignable.RoleArguments => [Target?.PlayerId ?? 0];
        private float[] players = new float[24];
        float protectTime = 0f;

        [OnlyMyPlayer]
        void CheckExtraWin(PlayerCheckExtraWinEvent ev) => ev.SetWin(ev.WinnersMask.Test(Target) | ev.GameEnd == RemakeInit.GameEnd.YandereWin);

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                for(int i = 0;i < NebulaGameManager.Instance?.AllPlayersNum; i++)
                {
                    players[i] = 0f;
                }
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, p => !p.IsDead && p.PlayerId != Target!.PlayerId && players[p.PlayerId] >= MaxStandableTimeOption));
                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton. CoolDownTimer = Bind(new Timer(KillCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());

                protectTime = 10f;
            }
        }

        private bool check(Virial.Game.Player p) => !p.IsDead &&
                p.PlayerId != MyPlayer.PlayerId &&
                p.PlayerId != Target!.PlayerId;

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev) => protectTime = ProtectTimeAfterMeetingOption;

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if ((Target?.IsDead ?? true) && !MyPlayer.IsDead)
                MyPlayer.Suicide(PlayerState.Suicide, EventDetail.Kill, KillParameter.NormalKill);
            else if(!(Target?.IsDead ?? true) && !MyPlayer.IsDead && NebulaGameManager.Instance?.AllPlayerInfo.Count(p => !p.IsDead) <= 3)
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.VultureWin, 1 << Target.PlayerId | 1 << MyPlayer.PlayerId);

            var targets = NebulaGameManager.Instance?.AllPlayerInfo.Where(p => 
                check(p) &&
                Vector2.Distance(Target!.TruePosition, p.TruePosition) < CheckMaxDistanceOption);
            if (protectTime > 0f) protectTime -= Time.deltaTime;
            else if (targets != null)
            {
                targets.Do(p =>
                {
                    players[p.PlayerId] += Time.deltaTime;
                    //Debug.Log(players[p.PlayerId]);
                    if (players[p.PlayerId] > MaxStandableTimeOption && !arrows.Any(arrow => arrow.MyPlayer.PlayerId == p.PlayerId))
                        arrows.Add(Bind(new TrackingArrowAbility(p, 2.5f, MyTeam.UnityColor)).Register());
                });
            }
            arrows.DoIf(a => !check(a.MyPlayer), a =>
            {
                a.ReleaseIt();
                arrows.Remove(a);
            });
        }

        [Local]
        void DecorateSidekickColor(PlayerDecorateNameEvent ev)
        {
            if (ev.Player.PlayerId == Target!.PlayerId) ev.Color = MyTeam.Color;
        }
    }
}