using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;

namespace Nebula.Roles.Crewmate;

public class Bait : DefinedRoleTemplate, HasCitation, DefinedRole
{

    private Bait(): base("bait", new(0, 247, 255), RoleCategory.CrewmateRole, Crewmate.MyTeam, [ShowKillFlashOption, ReportDelayOption, ReportDelayDispersionOption, CanSeeVentFlashOption]) {
    }

    Citation? HasCitation.Citaion => Citations.TheOtherRoles;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static internal BoolConfiguration ShowKillFlashOption = NebulaAPI.Configurations.Configuration("options.role.bait.showKillFlash", false);
    static internal FloatConfiguration ReportDelayOption = NebulaAPI.Configurations.Configuration("options.role.bait.reportDelay", (0f, 5f, 0.5f), 0f, FloatConfigurationDecorator.Second);
    static internal FloatConfiguration ReportDelayDispersionOption = NebulaAPI.Configurations.Configuration("options.role.bait.reportDelayDispersion", (0f, 10f, 0.25f), 0.5f, FloatConfigurationDecorator.Second);
    static internal BoolConfiguration CanSeeVentFlashOption = NebulaAPI.Configurations.Configuration("options.role.bait.canSeeVentFlash", false);

    static public Bait MyRole = new Bait();

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player){}

        void RuntimeAssignable.OnActivated() { }


        AchievementToken<(bool cleared, bool triggered)>? acTokenChallenge;
        private IEnumerator CoReport(PlayerControl murderer)
        {
            if(ShowKillFlashOption) AmongUsUtil.PlayQuickFlash(MyRole.UnityColor);

            float t = Mathf.Max(0.1f, ReportDelayOption) + ReportDelayDispersionOption * (float)System.Random.Shared.NextDouble();
            yield return new WaitForSeconds(t);
            murderer.CmdReportDeadBody(MyPlayer.VanillaPlayer.Data);
        }

        [Local, OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner) return; //自殺の場合は何もしない

            new StaticAchievementToken("bait.common1");
            acTokenChallenge ??= new("bait.challenge", (false, true), (val, _) => val.cleared);
        }

        [Local,OnlyMyPlayer]
        void OnExiled(PlayerExiledEvent ev)
        {
            new StaticAchievementToken("bait.another1");
        }

        [OnlyMyPlayer]
        void BaitReportOnMurdered(PlayerMurderedEvent ev)
        { 
            if (ev.Murderer.AmOwner && !MyPlayer.AmOwner) NebulaManager.Instance.StartCoroutine(CoReport(ev.Murderer.VanillaPlayer).WrapToIl2Cpp());
        }

        [Local]
        void OnEnterVent(PlayerVentEnterEvent ev)
        {
            if (CanSeeVentFlashOption) AmongUsUtil.PlayQuickFlash(MyRole.UnityColor.AlphaMultiplied(0.3f));
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (acTokenChallenge != null) acTokenChallenge.Value.triggered = false;
        }

        [Local]
        void OnPlayerExiled(PlayerExiledEvent ev)
        {
            if ((acTokenChallenge?.Value.triggered ?? false) && ev.Player.PlayerId == (MyPlayer.MyKiller?.PlayerId ?? 255))
                acTokenChallenge.Value.cleared = true;
        }
    }
}

public class BaitModifier : DefinedModifierTemplate, HasCitation, DefinedAllocatableModifier, RoleFilter
{
    private BaitModifier() : base("baitModifier", new(0, 247, 255), [NumOfRolesOption, RoleChanceOption, Bait.ShowKillFlashOption, Bait.ReportDelayOption, Bait.ReportDelayDispersionOption, Bait.CanSeeVentFlashOption])
    {
        ConfigurationHolder?.SetDisplayState(() => NumOfRolesOption == 0 ? ConfigurationHolderState.Inactivated : RoleChanceOption == 100 ? ConfigurationHolderState.Emphasized : ConfigurationHolderState.Activated);
    }

    private static IntegerConfiguration NumOfRolesOption = NebulaAPI.Configurations.Configuration("options.role.baitModifier.numToSpawn", (0, 15), 1);
    private static IntegerConfiguration RoleChanceOption = NebulaAPI.Configurations.Configuration("options.role.baitModifier.roleChance", (10, 100, 10), 100, decorator: num => num + "%", title: new TranslateTextComponent("options.role.chance"));
    
    Citation? HasCitation.Citaion => Citations.TheOtherRoles;
    string ICodeName.CodeName => "BATM";
    bool AssignableFilter<DefinedRole>.Test(DefinedRole role) => role.ModifierFilter?.Test(this) ?? false;
    void AssignableFilter<DefinedRole>.ToggleAndShare(DefinedRole role) => role.ModifierFilter?.ToggleAndShare(this);
    void AssignableFilter<DefinedRole>.SetAndShare(Virial.Assignable.DefinedRole role, bool val) => role.ModifierFilter?.SetAndShare(this, val);
    RoleFilter HasRoleFilter.RoleFilter => this;
    bool ISpawnable.IsSpawnable => NumOfRolesOption > 0;

    int HasAssignmentRoutine.AssignPriority => 1;
    static public BaitModifier MyRole = new BaitModifier();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    void HasAssignmentRoutine.TryAssign(Virial.Assignable.IRoleTable roleTable)
    {
        var crewmates = roleTable.GetPlayers(RoleCategory.CrewmateRole).Where(p => p.role.CanLoad(this)).OrderBy(_ => Guid.NewGuid()).ToArray();
        int index = 0;

        int maxNum = NumOfRolesOption;
        (byte playerId, DefinedRole role)? target;

        int assigned = 0;
        for (int i = 0; i < maxNum; i++)
        {
            float chance = RoleChanceOption / 100f;
            if ((float)System.Random.Shared.NextDouble() >= chance) continue;

            try
            {
                target = crewmates[index++];

                roleTable.SetModifier(target.Value.playerId, this, new int[] { });

                assigned++;
            }
            catch
            {
                //範囲外アクセス(これ以上割り当てできない)
                break;
            }
        }
    }

    void IAssignToCategorizedRole.GetAssignProperties(RoleCategory category, out int assign100, out int assignRandom, out int assignChance)
    {
        if (category == RoleCategory.CrewmateRole)
        {
            assign100 = RoleChanceOption == 100 ? NumOfRolesOption : 0;
            assignRandom = RoleChanceOption == 100 ? 0 : NumOfRolesOption;
        }
        else
        {
            assign100 = 0;
            assignRandom = 0;
        }

        assignChance = RoleChanceOption;
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }


        AchievementToken<(bool cleared, bool triggered)>? acTokenChallenge;
        private IEnumerator CoReport(PlayerControl murderer)
        {
            if (Bait.ShowKillFlashOption) AmongUsUtil.PlayQuickFlash(MyRole.UnityColor);

            float t = Mathf.Max(0.1f, Bait.ReportDelayOption) + Bait.ReportDelayDispersionOption * (float)System.Random.Shared.NextDouble();
            yield return new WaitForSeconds(t);
            murderer.CmdReportDeadBody(MyPlayer.VanillaPlayer.Data);
        }

        [Local, OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner) return;

            new StaticAchievementToken("bait.common1");
            acTokenChallenge ??= new("bait.challenge", (false, true), (val, _) => val.cleared);
        }

        [OnlyMyPlayer]
        void BaitReportOnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner && !MyPlayer.AmOwner) NebulaManager.Instance.StartCoroutine(CoReport(ev.Murderer.VanillaPlayer).WrapToIl2Cpp());
        }

        [Local]
        void OnEnterVent(PlayerVentEnterEvent ev)
        {
            if (Bait.CanSeeVentFlashOption) AmongUsUtil.PlayQuickFlash(MyRole.UnityColor.AlphaMultiplied(0.3f));
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (acTokenChallenge != null) acTokenChallenge.Value.triggered = false;
        }

        [Local]
        void OnPlayerExiled(PlayerExiledEvent ev)
        {
            if ((acTokenChallenge?.Value.triggered ?? false) && ev.Player.PlayerId == (MyPlayer.MyKiller?.PlayerId ?? 255))
                acTokenChallenge.Value.cleared = true;
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " @".Color(MyRole.UnityColor);
        }

        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.bait.blurb").Color(MyRole.UnityColor);
    }
}