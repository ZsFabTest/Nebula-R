using NAudio.CoreAudioApi;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public enum CatRoleCategory
{
    Original,
    Crewmate,
    Impostor,
    Jackal
}

public class SchrodingersCat : DefinedRoleTemplate, HasCitation, DefinedRole
{
    CatRoleCategory type = CatRoleCategory.Original;
    static public RoleTeam MyTeam = new Team("teams.schrödingersCat", new(115, 115, 115), TeamRevealType.OnlyMe);
    public SchrodingersCat(CatRoleCategory type) : base(
        GetLocalizeName(type), 
        GetRoleTeam(type).Color, 
        GetRoleCategory(type), 
        GetRoleTeam(type),
        [KillCoolDownOption, ImpostorKillOnlyWhenLeftOneOption],
        type == CatRoleCategory.Original,
        type == CatRoleCategory.Original)
    {
        this.type = type;
        ConfigurationHolder?.ScheduleAddRelated(() => [MyRoles[0].ConfigurationHolder!]);
    }

    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && type is CatRoleCategory.Original;
    bool AssignableFilterHolder.CanLoad(Virial.Assignable.DefinedAssignable assignable) => MyRoles[0].CanLoadDefaultTemplate(assignable);
    bool DefinedAssignable.ShowOnHelpScreen => type == CatRoleCategory.Original;
    bool DefinedAssignable.ShowOnFreeplayScreen => type == CatRoleCategory.Original;
    bool IGuessed.CanBeGuessDefault => true;

    static RoleTeam GetRoleTeam(CatRoleCategory type)
    {
        switch (type)
        {
            case CatRoleCategory.Original:
                return MyTeam;
            case CatRoleCategory.Crewmate:
                return Crewmate.Crewmate.MyTeam;
            case CatRoleCategory.Impostor:
                return Impostor.Impostor.MyTeam;
            case CatRoleCategory.Jackal:
                return Jackal.MyTeam;
            default:
                return MyTeam;
        }
    }

    static RoleCategory GetRoleCategory(CatRoleCategory type)
    {
        switch (type)
        {
            case CatRoleCategory.Original:
                return RoleCategory.NeutralRole;
            case CatRoleCategory.Crewmate:
                return RoleCategory.CrewmateRole;
            case CatRoleCategory.Impostor:
                return RoleCategory.ImpostorRole;
            case CatRoleCategory.Jackal:
                return RoleCategory.NeutralRole;
            default:
                return RoleCategory.NeutralRole;
        }
    }

    static CatRoleCategory GetCatRoleCategory(RoleCategory type)
    {
        switch (type)
        {
            case RoleCategory.NeutralRole:
                return CatRoleCategory.Jackal;
            case RoleCategory.CrewmateRole:
                return CatRoleCategory.Crewmate;
            case RoleCategory.ImpostorRole:
                return CatRoleCategory.Impostor;
            default:
                return CatRoleCategory.Original;
        }
    }

    static string GetLocalizeName(CatRoleCategory type)
    {
        switch (type)
        {
            case CatRoleCategory.Original:
                return "originalSchrödingersCat";
            case CatRoleCategory.Crewmate:
                return "crewmateSchrödingersCat";
            case CatRoleCategory.Impostor:
                return "impostorSchrödingersCat";
            case CatRoleCategory.Jackal:
                return "jackalSchrödingersCat";
            default:
                return "schrödingersCat";
        }
    }

    Citation HasCitation.Citaion => Citations.TheOtherRolesGMH;

    static private IRelativeCoolDownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.schrödingersCat.killCoolDown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static private BoolConfiguration ImpostorKillOnlyWhenLeftOneOption = NebulaAPI.Configurations.Configuration("options.role.schrödingersCat.whenCanKill", true);
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, (CatRoleCategory)arguments.Get(0,0));

    public static List<SchrodingersCat> MyRoles = new List<SchrodingersCat>() {
        new(CatRoleCategory.Original),
        new(CatRoleCategory.Crewmate),
        new(CatRoleCategory.Impostor),
        new(CatRoleCategory.Jackal)};
    private class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRoles[(int)type];
        public CatRoleCategory type = CatRoleCategory.Original;
        public Instance(Virial.Game.Player player, CatRoleCategory type) : base(player)
        {
            this.type = type;
        }
        int[]? RuntimeAssignable.RoleArguments => [(int)type];
        bool RuntimeRole.HasVanillaKillButton => false;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner && type is CatRoleCategory.Impostor or CatRoleCategory.Jackal)
            {
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.KillablePredicate(MyPlayer).Invoke(p) && (type != CatRoleCategory.Jackal || p.Role.Role.Team != Jackal.MyTeam) ));

                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = button => myTracker.CurrentTarget != null && (ImpostorKillOnlyWhenLeftOneOption || type != CatRoleCategory.Impostor || NebulaGameManager.Instance!.AllPlayerInfo.Count(p => p.IsImpostor) <= 1);
                killButton.Visibility = button => !MyPlayer.IsDead;
                killButton.OnClick = button =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(KillCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabel("kill");
                killButton.GetKillButtonLike();
            }
        }

        [Local]
        void DecorateSidekickColor(PlayerDecorateNameEvent ev)
        {
            if (type != CatRoleCategory.Jackal) return;
            if (ev.Player.Role.Role.Team == Jackal.MyTeam) ev.Color = Jackal.MyTeam.Color;
        }

        //サイドキックはジャッカルを識別できる
        [OnlyMyPlayer]
        void DecorateJackalColor(PlayerDecorateNameEvent ev)
        {
            if (type != CatRoleCategory.Jackal) return;
            var myInfo = PlayerControl.LocalPlayer.GetModInfo();
            if (myInfo == null) return;

            if (myInfo.Role.Role.Team == Jackal.MyTeam) ev.Color = Jackal.MyTeam.Color;
        }

        [OnlyMyPlayer]
        void OnKilled(PlayerCheckKilledEvent ev)
        {
            if (ev.IsMeetingKill) return;
            if (type == CatRoleCategory.Original)
            {
                if (ev.Killer.Role.Role.Category is RoleCategory.NeutralRole && ev.Killer.Role.Role.Team != Jackal.MyTeam) return;
                ev.Result = KillResult.ObviousGuard;
                MyPlayer.Unbox().RpcInvokerSetRole(MyRoles[(int)GetCatRoleCategory(ev.Killer.Role.Role.Category)], [(int)GetCatRoleCategory(ev.Killer.Role.Role.Category)]).InvokeSingle();
            }
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.JackalWin && !MyPlayer.IsDead && type == CatRoleCategory.Jackal);

        bool RuntimeAssignable.CanKill(Virial.Game.Player player)
        {
            if (type != CatRoleCategory.Jackal) return true;
            if (player.Role.Role.Team == Jackal.MyTeam) return false;
            return true;
        }
    }
}
