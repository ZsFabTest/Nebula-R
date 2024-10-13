/*
using Virial.Assignable;
using Virial.Events.Game;
using Virial.Events.Player;

namespace Nebula.Roles.Modifier;

public class TianMengLucky : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private TianMengLucky() : base("tianMengLucky", "TML", new(11, 45, 14), []) { }

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    public static TianMengLucky MyRole = new TianMengLucky();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(Virial.Game.Player player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (MyPlayer.Unbox().Role.Role == Neutral.Sidekick.MyRole || MyPlayer.TryGetModifier<Neutral.SidekickModifier.Instance>(out _))
                MyPlayer.Unbox().RpcInvokerUnsetModifier(MyRole).InvokeSingle();
        }

        [Local]
        void OnCriteriaUpdate(CriteriaUpdateEvent ev)
        {
            if (!MyPlayer.IsDead && MyPlayer.Role.Role.Category is RoleCategory.CrewmateRole && ev.CriterialGameEnd == NebulaGameEnd.CrewmateWin)
                ev.BlockWinning(true);
        }

        [Local]
        void BlockWin(PlayerBlockWinEvent ev)
        {
            if (MyPlayer.Role.Role.Category is RoleCategory.CrewmateRole && 
                ev.GameEnd == NebulaGameEnd.CrewmateWin)
                ev.SetBlockedIf(true);
            if (MyPlayer.Role.Role.Category is RoleCategory.ImpostorRole && 
              PlayerControl.AllPlayerControls.GetFastEnumerator().Count(
                (p) => !p.Data.IsDead && 
                  p.GetModInfo()?.Role.Role.Category is RoleCategory.ImpostorRole) > 1 &&
              ev.GameEnd == NebulaGameEnd.ImpostorWin)
                ev.SetBlockedIf(true);
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (ev.Player.AmOwner && MyPlayer.Role.Role.Category is RoleCategory.NeutralRole)
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.JesterWin, 1 << MyPlayer.PlayerId);
        }

        [Local]
        void CheckWin(PlayerCheckExtraWinEvent ev)
        {
            if (ev.Player.AmOwner && MyPlayer.Role.Role.Category is RoleCategory.CrewmateRole && 
              ev.GameEnd == NebulaGameEnd.ImpostorWin)
                ev.SetWin(true);
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " Yee".Color(MyRole.UnityColor);
        }
    }
}
*/
