using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Text;

namespace Nebula.Roles.Impostor;

public class EvilAce : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private EvilAce() : base("evilAce", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [killCoolDownOption]) { }

    Citation? HasCitation.Citaion => Citations.NebulaOnTheShip_Old;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static FloatConfiguration killCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.evilAce.killCoolDown", (0.125f, 1f, 0.125f), 0.75f, decorator: val => val + ("x" + $" ({string.Format("{0:#.#}", AmongUsUtil.VanillaKillCoolDown * val)}{Language.Translate("options.sec")})".Color(Color.gray)));

    public static EvilAce MyRole = new EvilAce();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        Dictionary<byte, DefinedRole> roleTab = new();

        void RuntimeAssignable.OnActivated() 
        {
            roleTab = new();
            if (AmOwner)
            {
                PlayerControl.AllPlayerControls.GetFastEnumerator().Do((p) =>
                {
                    if (p.PlayerId != MyPlayer.PlayerId && p.GetModInfo()?.Role.Role.Category == RoleCategory.ImpostorRole)
                        roleTab[p.PlayerId] = p.GetModInfo()!.Role.Role;
                });
            }
        }

        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if(ev.Murderer.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                roleTab[ev.Dead.PlayerId] = ev.Dead.Role.Role;
            }

            if (ev.Murderer.PlayerId == MyPlayer.PlayerId)
            {
                if (ev.Dead.PlayerState == PlayerStates.Guessed)
                {
                    new StaticAchievementToken("evilAce.common1");
                    if (ev.Dead.Role.Role.Category == RoleCategory.ImpostorRole)
                        new StaticAchievementToken("evilAce.another1");
                }
            }
        }

        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (ev.Player.MyKiller?.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                roleTab[ev.Player.PlayerId] = ev.Player.Role.Role;
            }
        }

        [Local]
        void DecoratePlayerName(PlayerDecorateNameEvent ev)
        {
            DefinedRole role = null!;
            if(roleTab.TryGetValue(ev.Player.PlayerId, out role!) && role != null)
            {
                ev.Name += $"<size=0.9>({role.DisplayName})</size>";
            }
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            short dead = 0, flag = 0;
            PlayerControl.AllPlayerControls.GetFastEnumerator().DoIf((p) => p.Data.IsDead, (p) =>
            {
                dead++;
                if (roleTab.TryGetValue(p.PlayerId, out _))
                    flag++;
            });
            if (dead >= 5 && flag == dead) 
            {
                new StaticAchievementToken("evilAce.challenge");
            }
        }

        [Local]
        void SetKillTimer(PlayerSetKillTimerEvent ev)
        {
            if (!PlayerControl.AllPlayerControls.GetFastEnumerator().Any((p) => !p.Data.IsDead && p.PlayerId != MyPlayer.PlayerId && p.GetModInfo()?.Role.Role.Category == RoleCategory.ImpostorRole))
                ev.SetTime(AmongUsUtil.VanillaKillCoolDown * killCoolDownOption);
        }
    }
}