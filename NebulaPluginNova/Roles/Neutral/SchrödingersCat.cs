using Nebula.Compat;
using Nebula.Game.Statistics;
using Nebula.Roles.Complex;
using System.Reflection.Metadata.Ecma335;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public class SchrödingersCat : DefinedRoleTemplate, HasCitation, DefinedRole
{
    static public RoleTeam MyTeam = new Team("teams.schrödingersCat", new(115, 115, 115), TeamRevealType.OnlyMe);
    private static string[] adds = ["", "Crewmate", "Impostor", "Jackal"];
    private static Virial.Color GetColor(int categoryId)
    {
        switch (categoryId)
        {
            case 0:
                return NebulaTeams.ChainShifterTeam.Color;
            case 1:
                return new(Palette.CrewmateBlue);
            case 2:
                return new(Palette.ImpostorRed);
            case 3:
                return NebulaTeams.JackalTeam.Color;
            default:
                return NebulaTeams.ChainShifterTeam.Color;
        }
    }
    private static RoleCategory GetCategory(int categoryId)
    {
        switch (categoryId)
        {
            case 0:
                return RoleCategory.NeutralRole;
            case 1:
                return RoleCategory.CrewmateRole;
            case 2:
                return RoleCategory.ImpostorRole;
            case 3:
                return RoleCategory.NeutralRole;
            default:
                return RoleCategory.NeutralRole;
        }
    }
    private static int GetCategoryId(DefinedRole role)
    {
        switch (role.Category)
        {
            case RoleCategory.CrewmateRole:
                return 1;
            case RoleCategory.ImpostorRole:
                return 2;
            case RoleCategory.NeutralRole:
                if (role.Team == Jackal.MyTeam)
                    return 3;
                else
                    return 0;
            default:
                return 0;
        }
    }
    private static RoleTeam GetCorrectTeam(int categoryId)
    {
        switch (categoryId)
        {
            case 0:
                return MyTeam;
            case 1:
                return Crewmate.Crewmate.MyTeam;
            case 2:
                return Impostor.Impostor.MyTeam;
            case 3:
                return Jackal.MyTeam;
            default:
                return MyTeam;
        }
    }
    private int categoryId;
    private SchrödingersCat(int categoryId) : base("schrödingersCat" + adds.Get(categoryId, ""), 
        GetColor(categoryId), GetCategory(categoryId), GetCorrectTeam(categoryId), [NumOfLeftImpostorToBeAllowedToKillOption, ImpostorKillCoolDownOption, JackalHasKillOption, JackalKillCoolDownOption],
        categoryId == 0,categoryId == 0)
    {
        this.categoryId = categoryId;
        if (categoryId > 0)
        {
            ConfigurationHolder?.ScheduleAddRelated(() => [MyRole.ConfigurationHolder!]);
        }
    }
    Citation? HasCitation.Citaion => Citations.TheOtherRolesGMH;
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && categoryId == 0;

    bool IGuessed.CanBeGuessDefault => categoryId == 0;
    bool DefinedAssignable.ShowOnHelpScreen => categoryId == 0;

    RuntimeRole CheckAndCreateInstance(Virial.Game.Player player)
    {
        switch (categoryId)
        {
            case 0:
                return new Instance(player);
            case 1:
                return new InstanceCrewmate(player);
            case 2:
                return new InstanceImpostor(player);
            case 3:
                return new InstanceJackal(player);
            default:
                return new Instance(player);
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => CheckAndCreateInstance(player);

    static private IntegerConfiguration NumOfLeftImpostorToBeAllowedToKillOption = NebulaAPI.Configurations.Configuration("options.role.schrödingersCat.numOfLeftImpostorToBeAllowedToKill", (0, 4), 2);
    static private IRelativeCoolDownConfiguration ImpostorKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.schrödingersCat.impostorKillCoolDown", CoolDownType.Relative, (2.5f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), 0f, (0.125f, 2f, 0.125f), 1f);
    static private BoolConfiguration JackalHasKillOption = NebulaAPI.Configurations.Configuration("options.role.schrödingersCat.jackalHasKill", true);
    static private IRelativeCoolDownConfiguration JackalKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.schrödingersCat.jackalKillCoolDown", CoolDownType.Relative, (2.5f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), 0f, (0.125f, 2f, 0.125f), 1f);

    public static SchrödingersCat MyRole = new SchrödingersCat(0);
    public static SchrödingersCat MyRoleCrewmate = new SchrödingersCat(1);
    public static SchrödingersCat MyRoleImpostor = new SchrödingersCat(2);
    public static SchrödingersCat MyRoleJackal = new SchrödingersCat(3);
    public static SchrödingersCat[] MyRoles = { MyRole, MyRoleCrewmate, MyRoleImpostor, MyRoleJackal };
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public Instance(GamePlayer player) : base(player) { }
        DefinedRole RuntimeRole.Role => MyRole;

        void RuntimeAssignable.OnActivated() { }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKillEvent ev)
        {
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse)
            {
                new StaticAchievementToken("schrödingersCat.another1");
                return;
            }
            if (ev.Killer.PlayerId == MyPlayer.PlayerId)
            {
                new StaticAchievementToken("schrödingersCat.another1");
                return;
            }

            if (GetCategoryId(ev.Killer.Role.Role) == 0) return;
            ev.Result = KillResult.Guard;
        }

        [OnlyMyPlayer]
        void OnGuard(PlayerGuardEvent ev)
        {
            //if (ev.Murderer.AmOwner) ev.Murderer.VanillaPlayer.transform.position = MyPlayer.VanillaPlayer.transform.position;
            if (AmOwner)
            {
                int nextCategoryId = GetCategoryId(ev.Murderer.Role.Role);
                AmongUsUtil.PlayQuickFlash(GetColor(nextCategoryId).ToUnityColor());
                MyPlayer.Unbox().RpcInvokerSetRole(MyRoles[nextCategoryId], null!).InvokeSingle();
                new StaticAchievementToken("schrödingersCat.common1");
            }
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if(!MyPlayer.IsDead) new StaticAchievementToken("schrödingersCat.common2");
        }
    }
    public class InstanceCrewmate : RuntimeAssignableTemplate, RuntimeRole
    {
        public InstanceCrewmate(GamePlayer player) : base(player) { }
        DefinedRole RuntimeRole.Role => MyRoleCrewmate;

        void RuntimeAssignable.OnActivated() { }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if(!MyPlayer.IsDead && ev.EndState.EndCondition == NebulaGameEnds.CrewmateGameEnd)
            {
                new StaticAchievementToken("schrödingersCat.challenge");
            }
        }
    }
    public class InstanceImpostor : RuntimeAssignableTemplate, RuntimeRole
    {
        public InstanceImpostor(GamePlayer player) : base(player) { }
        DefinedRole RuntimeRole.Role => MyRoleImpostor;
        bool RuntimeRole.HasVanillaKillButton => false;

        int CheckImpostorNum()
        {
            int count = 0;
            foreach(var player in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (!player.IsDead && player.IsImpostor)
                    count++;
            }
            return count;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var killTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, ObjectTrackers.ImpostorKillPredicate));

                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead && CheckImpostorNum() <= NumOfLeftImpostorToBeAllowedToKillOption;
                killButton.OnClick = (button) => {
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(ImpostorKillCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
            }
        }
    }
    public class InstanceJackal : RuntimeAssignableTemplate, RuntimeRole
    {
        public InstanceJackal(GamePlayer player) : base(player) { }
        DefinedRole RuntimeRole.Role => MyRoleJackal;

        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.JackalWin && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead));

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner && JackalHasKillOption)
            {
                var killTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p) && !IsJackal(p)));

                var killButton = Bind(new Modules.ScriptComponents.ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) => {
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(JackalKillCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
            }
        }

        private bool IsJackal(Virial.Game.Player player)
        {
            if (player == null) return false;
            if (player?.Role is Jackal.Instance) return true;
            if (player?.Role is Sidekick.Instance) return true;
            if (player?.Role is InstanceJackal) return true;
            if (player!.TryGetModifier<SidekickModifier.Instance>(out _)) return true;
            return false;
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!MyPlayer.IsDead && ev.EndState.EndCondition == NebulaGameEnds.JackalGameEnd)
            {
                new StaticAchievementToken("schrödingersCat.challenge");
            }
        }

        [Local]
        void DecorateSidekickColor(PlayerDecorateNameEvent ev)
        {
            if (IsJackal(ev.Player) && ev.Player.PlayerId != MyPlayer.PlayerId) ev.Color = MyRoleJackal.RoleColor;
        }

        //サイドキックはジャッカルを識別できる
        [OnlyMyPlayer]
        void DecorateJackalColor(PlayerDecorateNameEvent ev)
        {
            var myInfo = PlayerControl.LocalPlayer.GetModInfo();
            if (myInfo == null) return;

            if (IsJackal(myInfo) && ev.Player.PlayerId != MyPlayer.PlayerId) ev.Color = MyRoleJackal.RoleColor;
        }
    }
}

