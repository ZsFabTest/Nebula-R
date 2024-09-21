using Nebula.Compat;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;

namespace Nebula.Roles.Crewmate;

/*
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
*/

public class Bomb : INebulaScriptComponent
{
    public int ventId = int.MinValue;
    public byte ownerId = byte.MaxValue;
    public Bomb(Virial.Game.Player player, Vent vent) : base()
    {
        ventId = vent.Id;
        ownerId = player.PlayerId;
    }

    public bool OnActivated(Virial.Game.Player player, Vent vent, ref int killNum)
    {
        if (!MarkedRelease && vent.Id == ventId)
        {
            Helpers.GetPlayer(ownerId).GetModInfo()?.MurderPlayer(player, PlayerStates.Exploded, null, KillParameter.RemoteKill);
            this.ReleaseIt();
            if (player.PlayerId != ownerId && ++killNum >= 3) new StaticAchievementToken("miner.challenge");
            return true;
        }
        return false;
    }
}

public class Miner : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Virial.Color MyColor = new(100, 50, 0);
    private Miner() : base("miner", MyColor, RoleCategory.CrewmateRole, Crewmate.MyTeam, [LayingMineCoolDownOption, NumOfMinesOption]) { }
    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    internal static FloatConfiguration LayingMineCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.miner.layingMineCooldown", (2.5f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    internal static IntegerConfiguration NumOfMinesOption = NebulaAPI.Configurations.Configuration("options.role.miner.numOfMines", (1, 10), 3);

    public static Miner MyRole = new Miner();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            mineNum = arguments.Get(0, NumOfMinesOption);
            //For Test
            //else mm.SetNum(NumOfMinesOption);
        }

        // int[]? RuntimeAssignable.RoleArguments => ventId.ToArray();
        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LayingMinesButton.png", 115f);
        List<Bomb> bombs = new();
        int mineNum = NumOfMinesOption;
        int killNum = 0;
        int[]? RuntimeAssignable.RoleArguments => [mineNum];

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                killNum = 0;
                var myTracker = Bind(ObjectTrackers.ForVents(null, MyPlayer, (v) => !bombs.Any((b) => b.ventId == v.Id), MyColor.ToUnityColor()));
                var button = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = (button) => MyPlayer.CanMove && myTracker.CurrentTarget != null && mineNum > 0;
                button.Visibility = (button) => !MyPlayer.IsDead;
                button.OnClick = (button) =>
                {
                    Bomb bomb = new(MyPlayer, myTracker.CurrentTarget!);
                    bombs.Add(bomb);
                    GameOperatorManager.Instance?.Register<PlayerVentEnterEvent>((ev) => 
                         bomb.OnActivated(ev.Player, ev.Vent,ref killNum), bomb);
                    GameOperatorManager.Instance?.Register<PlayerVentExitEvent>((ev) =>
                         bomb.OnActivated(ev.Player, ev.Vent, ref killNum), bomb);

                    button.StartCoolDown();
                    button.ShowUsesIcon(3).text = (--mineNum).ToString();
                    if (mineNum <= 0 && NumOfMinesOption >= 3) new StaticAchievementToken("miner.common1");
                };
                button.ShowUsesIcon(3).text = mineNum.ToString();
                button.CoolDownTimer = Bind(new Timer(Miner.LayingMineCoolDownOption).SetAsAbilityCoolDown().Start());
                button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                button.SetLabel("layMines");
            }
        }

        /*
        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner && !MyPlayer.TryGetModifier<MinerModifier.Instance>(out var mm)) MyPlayer.Unbox().RpcInvokerSetModifier(MinerModifier.MyRole, null).InvokeSingle();
        }
        */
    }
}
