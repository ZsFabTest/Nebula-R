using Nebula.Game.Statistics;
using Virial.Assignable;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Ghost.Complex;

public class Unyielding : DefinedGhostRoleTemplate, DefinedGhostRole
{
    private Unyielding() : base("unyielding", new(191, 26, 95), RoleCategory.CrewmateRole | RoleCategory.ImpostorRole | RoleCategory.NeutralRole, []) { }

    string ICodeName.CodeName => "UYD";

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);
    public static Unyielding MyRole = new Unyielding();
    
    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;

        public Instance(Virial.Game.Player player) : base(player) { }
        private static Image reviveButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BuskReviveButton.png", 115f);
        private bool hasRevived = false;
        private bool canbespector = false;

        void RuntimeAssignable.OnActivated() 
        {
            DefinedRole GetDefaultRole(RoleCategory category)
            {
                switch (category)
                {
                    case RoleCategory.CrewmateRole:
                        return Nebula.Roles.Crewmate.Crewmate.MyRole;
                    case RoleCategory.ImpostorRole:
                        return Nebula.Roles.Impostor.Impostor.MyRole;
                    case RoleCategory.NeutralRole:
                        return Nebula.Roles.Crewmate.Crewmate.MyRole;
                    default:
                        return Nebula.Roles.Crewmate.Crewmate.MyRole;
                }
            }
            if (AmOwner)
            {
                hasRevived = false;
                canbespector = false;

                var myTracker = Bind(ObjectTrackers.ForDeadBody(null, MyPlayer, (p) => p.PlayerId != MyPlayer.PlayerId));
                var revievButton = Bind(new ModAbilityButton()).KeyBind(NebulaInput.GetInput(Virial.Compat.VirtualKeyInput.Ability));
                revievButton.SetSprite(reviveButtonSprite.GetSprite());
                revievButton.Availability = (button) => MyPlayer.CanMove && myTracker.CurrentTarget != null;
                revievButton.Visibility = (button) => MyPlayer.IsDead && !hasRevived;
                revievButton.OnClick = (button) =>
                {
                    var targetRole = myTracker.CurrentTarget!.Role.Role;
                    MyPlayer.Unbox().RpcInvokerSetRole(targetRole, myTracker.CurrentTarget!.Role.RoleArguments).InvokeSingle();
                    foreach (var d in Helpers.AllDeadBodies()) 
                        if (d.ParentId == myTracker.CurrentTarget!.PlayerId)
                            MyPlayer.Revive(null, new(d.transform.position), true);
                    AmongUsUtil.RpcCleanDeadBody(myTracker.CurrentTarget!.PlayerId);
                    myTracker.CurrentTarget!.Unbox().RpcInvokerSetRole(GetDefaultRole(targetRole.Category), null).InvokeSingle();
                    hasRevived = true;
                    new StaticAchievementToken("unyielding.common1");
                };
                revievButton.SetLabel("revive");
            }
        }

        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if (MyPlayer.IsDead && !hasRevived && !Helpers.AnyNonTriggersBetween(MyPlayer.VanillaPlayer.GetTruePosition(), ev.Dead.VanillaPlayer.GetTruePosition(), out var vec))
                new StaticAchievementToken("unyielding.challenge2");
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (hasRevived && ev.EndState.Winners.Test(MyPlayer))
                new StaticAchievementToken("unyielding.challenge1");
        }

        [Local]
        void CheckCanSeeAllInfo(RequestEvent ev)
        {
            if (ev.requestInfo == "checkCanSeeAllInfo") ev.Report(!hasRevived);
        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if(NebulaGameManager.Instance!.CanBeSpectator && !hasRevived)
            {
                NebulaGameManager.Instance?.SetSpectator(false);
                canbespector = true;
            }else if(!NebulaGameManager.Instance!.CanBeSpectator && canbespector)
                NebulaGameManager.Instance?.SetSpectator(true);
        }
    }
}
