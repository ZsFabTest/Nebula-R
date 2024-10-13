using Virial;
using Virial.Assignable;
using Virial.Configuration;

namespace Nebula.Roles.Impostor;

public class Grenadier : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private Grenadier() : base("grenadier", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [FlashCoolDownOption, FlashDurationOption, FlashRadiusOption]) { }

    Citation? HasCitation.Citaion => Citations.TownOfUsR;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static FloatConfiguration FlashCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashCoolDown", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration FlashDurationOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashDuration", (1f, 20f, 0.5f), 7.5f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration FlashRadiusOption = NebulaAPI.Configurations.Configuration("options.role.grenadier.flashRadius", (1f, 20f, 0.125f), 10f, FloatConfigurationDecorator.Ratio);

    static public Grenadier MyRole = new Grenadier();
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.GrenadierFlashButton.png", 115f);

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var button = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = button => MyPlayer.VanillaPlayer.CanMove;
                button.Visibility = button => !MyPlayer.IsDead;
                button.OnClick = button =>
                {
                    RpcGrenadierFlash.Invoke(MyPlayer.PlayerId);
                    button.ActivateEffect();
                };
                button.OnEffectEnd = button => button.StartCoolDown();
                button.CoolDownTimer = Bind(new Timer(FlashCoolDownOption).SetAsAbilityCoolDown().Start());
                button.EffectTimer = Bind(new Timer(FlashDurationOption));
                button.SetLabel("flash");
            }
        }

        private static void Flash()
        {
            if (PlayerControl.LocalPlayer.GetModInfo()!.IsImpostor)
                AmongUsUtil.PlayCustomFlash(Color.white, 0.2f, 0.2f, 0.5f, FlashDurationOption);
            else AmongUsUtil.PlayCustomFlash(Color.white, 0.2f, 0.2f, 1f, FlashDurationOption);
        }

        private static readonly RemoteProcess<byte> RpcGrenadierFlash = new(
            "GrenadierFlash",
            (message, _) =>
            {
                var Grenadier = Helpers.GetPlayer(message)!;
                if (Vector2.Distance(
                    PlayerControl.LocalPlayer.transform.position,
                    Grenadier.GetTruePosition()) < FlashRadiusOption)
                {
                    Flash();
                }
            });
    }
}