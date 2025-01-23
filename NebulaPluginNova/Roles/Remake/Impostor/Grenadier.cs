using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;

namespace Nebula.Roles.Impostor;

public class Grenadier : DefinedSingleAbilityRoleTemplate<Grenadier.Ability>, HasCitation, DefinedRole
{
    private Grenadier() : base("grenadier", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [FlashCoolDownOption, FlashDurationOption, FlashRadiusOption]) { }

    Citation? HasCitation.Citaion => RemakeInit.Citations.TownOfUsR;
    bool DefinedRole.IsJackalizable => true;

    //RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static FloatConfiguration FlashCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashCoolDown", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration FlashDurationOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashDuration", (1f, 20f, 0.5f), 7.5f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration FlashRadiusOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashRadius", (1f, 20f, 0.125f), 10f, FloatConfigurationDecorator.Ratio);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    static public Grenadier MyRole = new Grenadier();
    [NebulaRPCHolder]
    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Remake.GrenadierFlashButton.png", 115f);

        public Ability(GamePlayer player) : base(player) 
        {
            if (AmOwner)
            {
                var button = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = button => MyPlayer.VanillaPlayer.CanMove;
                button.Visibility = button => !MyPlayer.IsDead;
                button.OnClick = button =>
                {
                    RpcGrenadierFlash.Invoke((MyPlayer.PlayerId, MyPlayer.IsImpostor));
                    button.ActivateEffect();
                };
                button.OnEffectEnd = button => button.StartCoolDown();
                button.CoolDownTimer = Bind(new Timer(FlashCoolDownOption).SetAsAbilityCoolDown().Start());
                button.EffectTimer = Bind(new Timer(FlashDurationOption));
                button.SetLabel("flash");
            }
        }

        private static void Flash(bool flag)
        {
            if ((flag && PlayerControl.LocalPlayer.GetModInfo()!.IsImpostor)
                || (!flag && PlayerControl.LocalPlayer.GetModInfo()!.Role.Role.Team == Neutral.Jackal.MyTeam))
                AmongUsUtil.PlayCustomFlash(Color.white, 0.2f, 0.2f, 0.5f, FlashDurationOption);
            else AmongUsUtil.PlayCustomFlash(Color.white, 0.2f, 0.2f, 1f, FlashDurationOption);
        }

        private static readonly RemoteProcess<(byte, bool)> RpcGrenadierFlash = new(
            "GrenadierFlash",
            (message, _) =>
            {
                var Grenadier = Helpers.GetPlayer(message.Item1)!;
                if (Vector2.Distance(
                    PlayerControl.LocalPlayer.transform.position,
                    Grenadier.GetTruePosition()) < FlashRadiusOption)
                {
                    Flash(message.Item2);
                }
            });
    }
}