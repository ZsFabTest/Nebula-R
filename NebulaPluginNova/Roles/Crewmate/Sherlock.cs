using Cpp2IL.Core.Extensions;
using Nebula.Compat;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;

namespace Nebula.Roles.Crewmate;

public class Sherlock : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private Sherlock() : base("sherlock", new(230, 190, 70), RoleCategory.CrewmateRole, Crewmate.MyTeam, [NumOfInvestigationOption, InvestigateCoolDownOption, CanKnowExactlyRoleOption]) { }
    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    private static IntegerConfiguration NumOfInvestigationOption = NebulaAPI.Configurations.Configuration("options.role.sherlock.numOfInvestigation", (1, 24), 3);
    private static FloatConfiguration InvestigateCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.sherlock.investiateCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    private static BoolConfiguration CanKnowExactlyRoleOption = NebulaAPI.Configurations.Configuration("options.role.sherlock.canKnowExactlyRole", true);

    public static Sherlock MyRole = new Sherlock();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player)
        {
            investigationResult = new();
        }
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.InvestigateButton.png", 115f);
        Dictionary<byte, string> investigationResult;
        int leftNum = 0;

        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner)
            {
                leftNum = NumOfInvestigationOption;
                investigationResult = new();
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p)));
                var investigateButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                investigateButton.SetSprite(buttonSprite.GetSprite());
                investigateButton.Availability = (button) => myTracker.CurrentTarget != null && MyPlayer.CanMove && leftNum > 0;
                investigateButton.Visibility = (button) => !MyPlayer.IsDead;
                investigateButton.ShowUsesIcon(3).text = leftNum.ToString();
                investigateButton.OnClick = (button) =>
                {
                    if(CanKnowExactlyRoleOption) investigationResult[myTracker.CurrentTarget!.PlayerId] = myTracker.CurrentTarget!.Role.Role.DisplayName.Color(myTracker.CurrentTarget!.Role.Role.Color.ToUnityColor());
                    else investigationResult[myTracker.CurrentTarget!.PlayerId] = categoryToString(myTracker.CurrentTarget!.Role.Role.Category);
                    if (myTracker.CurrentTarget.IsImpostor) new StaticAchievementToken("sherlock.common");
                    leftNum--;
                    button.StartCoolDown();
                    button.ShowUsesIcon(3).text = leftNum.ToString();
                };
                investigateButton.CoolDownTimer = Bind(new Timer(InvestigateCoolDownOption).SetAsAbilityCoolDown().Start());
                investigateButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                investigateButton.SetLabel("investigate");
            }
        }

        string categoryToString(RoleCategory category)
        {
            switch (category)
            {
                case RoleCategory.CrewmateRole:
                    return Language.Translate("role.sherlock.investigation.crewmate").Color(NebulaTeams.CrewmateTeam.Color.ToUnityColor());
                case RoleCategory.ImpostorRole:
                    return Language.Translate("role.sherlock.investigation.impostor").Color(NebulaTeams.ImpostorTeam.Color.ToUnityColor());
                case RoleCategory.NeutralRole:
                    return Language.Translate("role.sherlock.investigation.neutral").Color(NebulaTeams.ChainShifterTeam.Color.ToUnityColor());
                default:
                    return string.Empty;
            }
        }

        [Local]
        void DecoratePlayerName(PlayerDecorateNameEvent ev)
        {
            if (investigationResult.TryGetValue(ev.Player.PlayerId, out var info))
                ev.Name += $"<size=0.9>({info})</size>";
        }
    }
}
