using Nebula.Behaviour;
using Nebula.Modules;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public class Spectre : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static RoleTeam MyTeam = new Team("teams.spectre", new(185, 52, 197), TeamRevealType.OnlyMe);
    private Spectre() : base("spectre", new(163, 73, 164), RoleCategory.NeutralRole, MyTeam, [HideMaxCountOption, HideCoolDownOption, HideDurationOption, LongTaskCountOption, ShortTaskCountOption]) { }
    Citation? HasCitation.Citaion => Citations.TheOtherRolesGM;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments);

    private static IntegerConfiguration HideMaxCountOption = NebulaAPI.Configurations.Configuration("options.role.spectre.hideMaxCount", (0, 10), 3);
    private static FloatConfiguration HideCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.spectre.hideCoolDown", (2.5f, 60f, 2.5f), 27.5f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration HideDurationOption = NebulaAPI.Configurations.Configuration("options.role.spectre.hideDuration", (2.5f, 15f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    private static IntegerConfiguration LongTaskCountOption = NebulaAPI.Configurations.Configuration("options.role.spectre.LongTaskCount", (0, 5), 2);
    private static IntegerConfiguration ShortTaskCountOption = NebulaAPI.Configurations.Configuration("options.role.spectre.ShortTaskCount", (0, 5), 2);

    public static Spectre MyRole = new Spectre();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player, int[] arguments) : base(player) 
        {
            left = arguments.Get(0,HideMaxCountOption);
        }
        int left = 0;
        int[] RuntimeAssignable.RoleArguments => [left];
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SpectreButton.png", 115f);

        void RuntimeAssignable.OnActivated()
        {
            var button = Bind(new ModAbilityButton());
            button.SetSprite(buttonSprite.GetSprite());
            button.Availability = button => true;
            button.Visibility = button => !MyPlayer.IsDead && left > 0;
            button.OnClick = button => button.ActivateEffect();
            button.OnEffectStart = button => PlayerModInfo.RpcAttrModulator.Invoke(new(MyPlayer.PlayerId, new AttributeModulator(PlayerAttributes.Invisible, HideDurationOption, false, 100), true));
            button.OnEffectEnd = button =>
            {
                button.StartCoolDown();
                left--;
                button.ShowUsesIcon(2).text = left.ToString();
                new StaticAchievementToken("spectre.common");
            };
            button.ShowUsesIcon(2).text = left.ToString();
            button.CoolDownTimer = Bind(new Timer(HideCoolDownOption).SetAsAbilityCoolDown().Start());
            button.EffectTimer = Bind(new Timer(HideDurationOption));
            button.SetLabel("hide");

            MyPlayer.Tasks.Unbox().ReplaceTasksAndRecompute(ShortTaskCountOption, LongTaskCountOption, 0);
        }

        [Local]
        void OverwriteEnd(EndCriteriaMetEvent ev)
        {
            if (!MyPlayer.IsDead && 
                MyPlayer.Tasks.IsCompletedCurrentTasks &&
                ev.EndReason != Virial.Game.GameEndReason.Task &&
                ev.EndReason != Virial.Game.GameEndReason.Sabotage &&
                ev.GameEnd == NebulaGameEnd.CrewmateWin ||
                ev.GameEnd == NebulaGameEnd.ImpostorWin ||
                ev.GameEnd == NebulaGameEnd.JackalWin ||
                ev.GameEnd == NebulaGameEnd.PavlovWin ||
                ev.GameEnd == NebulaGameEnd.MoriartyWin)
            {
                ev.TryOverwriteEnd(NebulaGameEnd.SpectreWin, Virial.Game.GameEndReason.Special);
                new StaticAchievementToken("spectre.challenge");
            }
        }

        RoleTaskType RuntimeRole.TaskType => RoleTaskType.RoleTask;
    }
}