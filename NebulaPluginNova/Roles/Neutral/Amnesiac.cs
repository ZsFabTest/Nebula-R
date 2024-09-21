using Hazel;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula.Compat;
using Nebula.Game.Statistics;
using Nebula.Roles.Crewmate;
using Unity.Services.Core.Internal;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;

namespace Nebula.Roles.Neutral;

/*
public class AmnesiacModifier : DefinedModifierTemplate, DefinedModifier
{
    private AmnesiacModifier() : base("amnesiacModifier", Amnesiac.MyColor, withConfigurationHolder: false) { }

    bool DefinedAssignable.ShowOnHelpScreen => false;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    public static AmnesiacModifier MyRole = new AmnesiacModifier();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => false;
        public Instance(Virial.Game.Player player) : base(player) { }
        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LayingMinesButton.png", 115f);

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myTracker = Bind(ObjectTrackers.ForDeadBody(null, MyPlayer, (p) => true));
                var button = Bind(new ModAbilityButton(true)).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility);

                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = (button) => MyPlayer.CanMove && myTracker.CurrentTarget != null;
                button.Visibility = (button) => !MyPlayer.IsDead;// && MyPlayer.Role.Role == Amnesiac.MyRole;
                button.OnClick = (button) =>
                {
                    var target = myTracker.CurrentTarget!;
                    MyPlayer.Unbox().RpcInvokerSetRole(target.Role.Role, target.Role.RoleArguments).InvokeSingle();
                    button.StartCoolDown();
                };
                button.CoolDownTimer = Bind(new Timer(Amnesiac.RecallCoolDownOption).SetAsAbilityCoolDown().Start());
                button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                button.SetLabel("recall");
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (canSeeAllInfo) name += Language.Translate("role.amnesiac.postfix").Color(MyRole.UnityColor);
        }

        // [OnlyMyPlayer]
        // void BlockWin(PlayerBlockWinEvent ev)
        // {
        //    if (Amnesiac.CanOnlyWinAsNeutralOption) ev.IsBlocked |= ev.GameEnd != NebulaGameEnds.AmnesiacGameEnd;
        // }

        // void TakenWin(EndCriteriaMetEvent ev)
        // {
        //     if (!MyPlayer.IsDead && Amnesiac.CanOnlyWinAsNeutralOption) ev.TryOverwriteEnd(NebulaGameEnds.AmnesiacGameEnd, GameEndReason.Special);
        // }

        // [OnlyMyPlayer]
        // void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.AmnesiacWin && Amnesiac.CanOnlyWinAsNeutralOption);
    }
}
*/

public class AmnesiacItem : IReleasable, ILifespan
{
    public static readonly UnityEngine.Color ItemColor = new(210f / 255f, 220f / 255f, 234f / 255f);
    private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LayingMinesButton.png", 115f);
    private ModAbilityButton? button = null;
    private Virial.Components.ObjectTracker<Virial.Game.Player>? myTracker = null;
    private ComponentHolder? componentHolder = null;
    private Virial.Game.Player? MyPlayer = null;
    public AmnesiacItem(Virial.Game.Player myPlayer) 
    {
        //Debug.Log(isExists.Get());
        //Debug.Log((isExists & (1 << myPlayer.PlayerId)));
        if ((StaticRemoteIntData.AmnesiacIsExists & (1 << myPlayer.PlayerId)) != 0) return;
        MyPlayer = myPlayer;
        if (MyPlayer.AmOwner)
        {
            Debug.Log(StaticRemoteIntData.AmnesiacIsExists | (1 << MyPlayer.PlayerId));
            Debug.LogError(StaticRemoteIntData.AmnesiacIsExists.Update(StaticRemoteIntData.AmnesiacIsExists.Get() | (1 << MyPlayer.PlayerId)));
            Debug.LogError(StaticRemoteIntData.AmnesiacIsExists.Get());
            componentHolder = new();
            myTracker = componentHolder.Bind(ObjectTrackers.ForDeadBody(null, MyPlayer, (p) => true));
            button = componentHolder.Bind(new ModAbilityButton(true)).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility);

            button.SetSprite(buttonSprite.GetSprite());
            button.Availability = (button) => MyPlayer.CanMove && myTracker.CurrentTarget != null;
            button.Visibility = (button) => !MyPlayer.IsDead;// && MyPlayer.Role.Role == Amnesiac.MyRole;
            button.OnClick = (button) =>
            {
                var target = myTracker.CurrentTarget!;
                MyPlayer.Unbox().RpcInvokerSetRole(target.Role.Role, target.Role.RoleArguments).InvokeSingle();
                button.StartCoolDown();
            };
            button.CoolDownTimer = componentHolder.Bind(new Timer(Amnesiac.RecallCoolDownOption).SetAsAbilityCoolDown().Start());
            button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
            button.SetLabel("recall");
        }
        GameOperatorManager.Instance?.Register<GameEndEvent>((ev) => this.ReleaseIt(), this);
    }
    void IReleasable.Release() 
    {
        Debug.Log("AmnesiacItem Released");
        componentHolder?.ReleaseIt();
        StaticRemoteIntData.AmnesiacIsExists.Update(StaticRemoteIntData.AmnesiacIsExists ^ (1 << MyPlayer!.PlayerId));
    }

    bool ILifespan.IsDeadObject => componentHolder?.IsDeadObject ?? true;
}

public class Amnesiac : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Virial.Color MyColor = new(210, 220, 234);
    public static Team MyTeam = new("teams.amnesiac", MyColor, TeamRevealType.OnlyMe);
    private Amnesiac() : base("amnesiac", MyColor, RoleCategory.NeutralRole, MyTeam, [RecallCoolDownOption/*, CanOnlyWinAsNeutralOption*/]) { }
    Citation? HasCitation.Citaion => Citations.Project_Lotus;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    internal static FloatConfiguration RecallCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.amnesiac.recallCooldown", (2.5f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    // internal static BoolConfiguration CanOnlyWinAsNeutralOption = NebulaAPI.Configurations.Configuration("options.role.amnesiac.canOnlyWinAsNeutral", false);

    public static Amnesiac MyRole = new Amnesiac();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }

        void RuntimeAssignable.OnActivated() 
        {
            new AmnesiacItem(MyPlayer);
        }
    }
}
