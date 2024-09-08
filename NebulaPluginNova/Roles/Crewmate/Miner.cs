using Nebula.Compat;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Crewmate;

public class MinerModifier : DefinedModifierTemplate, DefinedModifier
{
    private MinerModifier() : base("minerModifier", Miner.MyColor, withConfigurationHolder: false) { }

    bool DefinedAssignable.ShowOnHelpScreen => false;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    public static MinerModifier MyRole = new MinerModifier();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player) 
        {
            ventId = new(arguments);
        }
        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LayingMinesButton.png", 115f);
        List<int> ventId = new();
        int mineNum = Miner.NumOfMinesOption;
        int killNum = 0;
        int[]? RuntimeAssignable.RoleArguments => ventId.ToArray();

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                killNum = 0;
                var myTracker = Bind(ObjectTrackers.ForVents(null, MyPlayer, (v) => !ventId.Contains(v.Id), Miner.MyColor.ToUnityColor()));
                var button = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = (button) => MyPlayer.CanMove && myTracker.CurrentTarget != null && mineNum > 0;
                button.Visibility = (button) => !MyPlayer.IsDead && MyPlayer.Role.Role == Miner.MyRole;
                button.OnClick = (button) =>
                {
                    ventId.Add(myTracker.CurrentTarget!.Id);
                    button.StartCoolDown();
                    button.ShowUsesIcon(3).text = (--mineNum).ToString();
                    if (mineNum <= 0 && Miner.NumOfMinesOption >= 3) new StaticAchievementToken("miner.common1");
                };
                button.ShowUsesIcon(3).text = mineNum.ToString();
                button.CoolDownTimer = Bind(new Timer(Miner.LayingMineCoolDownOption).SetAsAbilityCoolDown().Start());
                button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                button.SetLabel("layMines");
            }
        }

        [Local]
        void OnEnterVent(PlayerVentEnterEvent ev)
        {
            if(ventId.Contains(ev.Vent.Id))
            {
                ventId.Remove(ev.Vent.Id);
                MyPlayer.MurderPlayer(ev.Player, PlayerState.Exploded, EventDetail.Kill, KillParameter.RemoteKill);
                if (ev.Player.PlayerId != MyPlayer.PlayerId && ++killNum >= 3) new StaticAchievementToken("miner.challenge");
            }
        }

        [Local]
        void OnExitVent(PlayerVentExitEvent ev)
        {
            if (ventId.Contains(ev.Vent.Id))
            {
                ventId.Remove(ev.Vent.Id);
                MyPlayer.MurderPlayer(ev.Player, PlayerState.Exploded, EventDetail.Kill, KillParameter.RemoteKill);
                if (ev.Player.PlayerId != MyPlayer.PlayerId && ++killNum >= 3) new StaticAchievementToken("miner.challenge");
            }
        }

        internal void SetNum(int num) => mineNum = num;
    }
}

public class Miner : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Virial.Color MyColor = new(100, 50, 0);
    private Miner() : base("miner", MyColor, RoleCategory.CrewmateRole, Crewmate.MyTeam, [LayingMineCoolDownOption, NumOfMinesOption]) { }
    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    internal static FloatConfiguration LayingMineCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.miner.layingMineCooldown", (2.5f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    internal static IntegerConfiguration NumOfMinesOption = NebulaAPI.Configurations.Configuration("options.role.miner.numOfMines", (1, 10), 3);

    public static Miner MyRole = new Miner();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player)
        {
            if (!MyPlayer.TryGetModifier<MinerModifier.Instance>(out var mm)) MyPlayer.Unbox().RpcInvokerSetModifier(MinerModifier.MyRole, null).InvokeSingle();
            //For Test
            //else mm.SetNum(NumOfMinesOption);
        }

        void RuntimeAssignable.OnActivated() { }
    }
}
