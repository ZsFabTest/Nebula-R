using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public class Apocalypse : DefinedRoleTemplate, HasCitation, DefinedRole
{
    static public RoleTeam MyTeam = new Team("teams.apocalypse", new(127, 26, 140), TeamRevealType.OnlyMe);

    private Apocalypse() : base("apocalypse", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [OriginKillCoolDownOption, CoolDownReductionOption]) { }

    Citation? HasCitation.Citaion => Citations.TownOfUsR;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments);

    static private IRelativeCoolDownConfiguration OriginKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.apocalypse.originKillCoolDown", CoolDownType.Immediate, (2.5f, 60f, 2.5f), 30f, (-40f, 40f, 0.5f), 5f, (0.125f, 2f, 0.125f), 1.125f);
    static private FloatConfiguration CoolDownReductionOption = NebulaAPI.Configurations.Configuration("options.role.apocalypse.coolDownReduction", (2.5f, 10f, 0.5f), 5f, FloatConfigurationDecorator.Second);

    static public Apocalypse MyRole = new Apocalypse();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player, int[] arguments) : base(player)
        {
            killCoolDown = Math.Min(arguments.Get(0, int.MaxValue) / 100f, OriginKillCoolDownOption.CoolDown);
        }
        private float killCoolDown = float.MaxValue;

        bool RuntimeRole.CanUseVent => true;
        int[]? RuntimeAssignable.RoleArguments => [((int)killCoolDown * 100)];

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var killCoolDownTimer = Bind(new Timer(killCoolDown).SetAsKillCoolDown().Start());
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, p => ObjectTrackers.StandardPredicate(p)));
                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    killCoolDownTimer.SetRange(0f, Math.Max(killCoolDown -= CoolDownReductionOption, 0f));
                    if (killCoolDown <= 0f && CoolDownReductionOption <= 5f && OriginKillCoolDownOption.CoolDown >= 25f) new StaticAchievementToken("apocalypse.challenge");
                    button.StartCoolDown();
                };
                killButton. CoolDownTimer = killCoolDownTimer;
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
            }
        }

        [Local]
        void BlockWin(PlayerBlockWinEvent ev) => ev.SetBlockedIf(ev.GameEnd == NebulaGameEnd.ApocalypseWin && MyPlayer.IsDead);
    }
}