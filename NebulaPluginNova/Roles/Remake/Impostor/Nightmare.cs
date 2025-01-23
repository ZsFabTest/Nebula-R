using Nebula.Behaviour;
using Nebula.Game.Statistics;
using Nebula.Modules.GUIWidget;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
namespace Nebula.Roles.Impostor;

public class Nightmare : DefinedSingleAbilityRoleTemplate<Nightmare.Ability>, DefinedRole
{
    private Nightmare() : base("nightmare", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [killCoolDownOption, speedRateOption]) { }

    //RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static FloatConfiguration killCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.nightmare.killCoolDown", (2.5f, 60f, 2.5f), 7.5f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration speedRateOption = NebulaAPI.Configurations.Configuration("options.role.nightmare.speedRate", (1.25f, 5f, 0.25f), 1.75f, FloatConfigurationDecorator.Ratio);
    
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);

    public static Nightmare MyRole = new Nightmare();

    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {
        public Ability(Virial.Game.Player player) : base(player) 
        {
            if (AmOwner)
            {
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer)));

                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = button => myTracker.CurrentTarget != null && Check();
                killButton.Visibility = button => !MyPlayer.IsDead;
                killButton.OnClick = button =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = Bind(new Timer(killCoolDownOption).Start());
                killButton.SetLabel("kill");
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        bool IPlayerAbility.HideKillButton => true;

        private bool Check()
        {
            for (int i = 0; i < PlayerControl.LocalPlayer.myTasks.Count; i++)
            {
                var task = PlayerControl.LocalPlayer.myTasks[i];
                if (task.TaskType is TaskTypes.FixLights)
                    return true;
            }
            return false;
        }

        [OnlyMyPlayer]
        void EditNameColor(PlayerDecorateNameEvent ev)
        {
            if (Check()) ev.Color = MyPlayer.IsImpostor ? MyRole.RoleColor : Neutral.Jackal.MyTeam.Color;
        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (MyPlayer.VanillaPlayer.MyPhysics.bodyType is PlayerBodyTypes.Normal && Check())
            {
                var lastFlipX = MyPlayer.VanillaPlayer.MyPhysics.FlipX;
                MyPlayer.VanillaPlayer.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
                MyPlayer.VanillaPlayer.cosmetics.SetBodyCosmeticsVisible(false);
                MyPlayer.VanillaPlayer.cosmetics.hat.Visible = true;
                MyPlayer.VanillaPlayer.MyPhysics.FlipX = lastFlipX;
                MyPlayer.GainAttribute(speedRateOption, float.MaxValue, false, 0, "nebula::nightmare");
            }
            else if (MyPlayer.VanillaPlayer.MyPhysics.bodyType is PlayerBodyTypes.Seeker && !Check())
            {
                var lastFlipX = MyPlayer.VanillaPlayer.MyPhysics.FlipX;
                MyPlayer.VanillaPlayer.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);
                MyPlayer.VanillaPlayer.cosmetics.SetBodyCosmeticsVisible(true);
                MyPlayer.VanillaPlayer.MyPhysics.FlipX = lastFlipX;
                MyPlayer.GainAttribute(1f, 0f, false, 0, "nebula::nightmare");
            }
        }

        void IGameOperator.OnReleased()
        {
            var lastFlipX = MyPlayer.VanillaPlayer.MyPhysics.FlipX;
            MyPlayer.VanillaPlayer.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);
            MyPlayer.VanillaPlayer.cosmetics.SetBodyCosmeticsVisible(true);
            MyPlayer.VanillaPlayer.MyPhysics.FlipX = lastFlipX;
            MyPlayer.GainAttribute(1f, 0f, false, 0, "nebula::nightmare");
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
        }
    }
}