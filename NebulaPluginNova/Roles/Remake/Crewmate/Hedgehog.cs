using Nebula.Compat;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
namespace Nebula.Roles.Crewmate;

public class Hedgehog : DefinedRoleTemplate, DefinedRole
{
    public static Virial.Color MyColor = new(198, 97, 97);
    private Hedgehog() : base("hedgehog", MyColor, RoleCategory.CrewmateRole, Crewmate.MyTeam, [DefenceCoolDownOption, DefenceDurationOption]) { }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    internal static FloatConfiguration DefenceCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.hedgehog.defenceCoolDown", (2.5f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    internal static FloatConfiguration DefenceDurationOption = NebulaAPI.Configurations.Configuration("options.role.hedgehog.defenceDuration", (0.1f, 1f, 0.05f), 0.5f, FloatConfigurationDecorator.Second);

    public static Hedgehog MyRole = new Hedgehog();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }

        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Remake.DefendButton.png", 115f);
        public bool isDefending { get; private set; }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var button = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = (button) => MyPlayer.CanMove;
                button.Visibility = (button) => !MyPlayer.IsDead;
                button.OnClick = (button) => button.ActivateEffect();
                button.OnEffectStart = (button) => isDefending = true;
                button.OnEffectEnd = (button) => 
                {
                    isDefending = false;
                    button.StartCoolDown();
                };
                button.CoolDownTimer = Bind(new Timer(DefenceCoolDownOption).SetAsAbilityCoolDown().Start());
                button.EffectTimer = Bind(new Timer(DefenceDurationOption));
                button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                button.SetLabel("defend");

                isDefending = false;
            }
        }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if(isDefending)
            {
                MyPlayer.MurderPlayer(ev.Killer, PlayerStates.Dead, EventDetails.Kill, KillParameter.NormalKill);
                ev.Result = KillResult.Guard;
                isDefending = false;
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => isDefending = false;
    }
}
