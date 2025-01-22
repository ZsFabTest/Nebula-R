using Nebula.Behaviour;
using Nebula.Game.Statistics;
using Nebula.Modules.GUIWidget;
using Nebula.Roles.Modifier;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;

namespace Nebula.Roles.Modifier;

public class MadmateModifier : DefinedModifierTemplate, DefinedAllocatableModifier, HasCitation, RoleFilter
{
    private MadmateModifier() : base("madmateModifier", Virial.Color.ImpostorColor, [NumToSpawnOption, RoleChanceOption, CanIdentifyImpostorsOptionEditor])
    {
        ConfigurationHolder?.SetDisplayState(() => NumToSpawnOption == 0 ? ConfigurationHolderState.Inactivated : RoleChanceOption == 100 ? ConfigurationHolderState.Emphasized : ConfigurationHolderState.Activated);
    }

    private static IntegerConfiguration NumToSpawnOption = NebulaAPI.Configurations.Configuration("options.role.madmateModifier.numToSpawn", (0, 15), 1);
    static private IntegerConfiguration RoleChanceOption = NebulaAPI.Configurations.Configuration("options.role.madmateModifier.roleChance", (10, 100, 10), 100, decorator: num => num + "%", title: new TranslateTextComponent("options.role.chance"));
    static private IntegerConfiguration CanIdentifyImpostorsOption = NebulaAPI.Configurations.Configuration("options.role.madmate.canIdentifyImpostors", (0, 3), 0);
    static private IOrderedSharableVariable<int>[] NumOfTasksToIdentifyImpostorsOptions = [
        NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors0", (0, 10), 2),
        NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors1", (0, 10), 4),
        NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors2", (0, 10), 6)
        ];
    static private IConfiguration CanIdentifyImpostorsOptionEditor = NebulaAPI.Configurations.Configuration(
        () => CanIdentifyImpostorsOption.GetDisplayText() + StringExtensions.Color(
            " (" +
            NumOfTasksToIdentifyImpostorsOptions
                .Take(CanIdentifyImpostorsOption)
                .Join(option => option.Value.ToString(), ", ")
            + ")", Color.gray),
        () =>
        {
            List<GUIWidget> widgets = new([CanIdentifyImpostorsOption.GetEditor().Invoke()]);

            if (CanIdentifyImpostorsOption.GetValue() > 0)
            {
                List<GUIWidget> inner = new([
                    GUI.API.LocalizedText(GUIAlignment.Left, GUI.API.GetAttribute(AttributeAsset.OptionsTitleHalf), "options.role.madmate.requiredTasksForIdentifying"),
                    GUI.API.RawText(GUIAlignment.Left, GUI.API.GetAttribute(AttributeAsset.OptionsFlexible), ":"),
                    GUI.API.HorizontalMargin(0.05f)
                    ]);
                int length = CanIdentifyImpostorsOption.GetValue();
                for (int i = 0; i < length; i++)
                {
                    if (i != 0) inner.AddRange([GUI.API.HorizontalMargin(0.05f), GUI.API.RawText(GUIAlignment.Center, GUI.API.GetAttribute(Virial.Text.AttributeAsset.OptionsFlexible), ",")]);

                    var option = NumOfTasksToIdentifyImpostorsOptions[i];

                    inner.AddRange([
                        GUI.API.RawText(GUIAlignment.Center, GUI.API.GetAttribute(Virial.Text.AttributeAsset.OptionsValueShorter), option.CurrentValue.ToString()),
                        GUI.API.SpinButton(GUIAlignment.Center, v => { option.ChangeValue(v, true); NebulaAPI.Configurations.RequireUpdateSettingScreen(); })
                        ]);
                }
                widgets.Add(new HorizontalWidgetsHolder(Virial.Media.GUIAlignment.Left, inner));
            }
            return new VerticalWidgetsHolder(Virial.Media.GUIAlignment.Left, widgets);
        }
        );

    string ICodeName.CodeName => "MDAM";
    Citation? HasCitation.Citaion => Citations.TheOtherRolesGM;
    bool AssignableFilter<DefinedRole>.Test(DefinedRole role) => role.ModifierFilter?.Test(this) ?? false;
    void AssignableFilter<DefinedRole>.ToggleAndShare(DefinedRole role) => role.ModifierFilter?.ToggleAndShare(this);
    void AssignableFilter<DefinedRole>.SetAndShare(Virial.Assignable.DefinedRole role, bool val) => role.ModifierFilter?.SetAndShare(this, val);
    RoleFilter HasRoleFilter.RoleFilter => this;
    bool ISpawnable.IsSpawnable => NumToSpawnOption > 0;

    int HasAssignmentRoutine.AssignPriority => 1;
    static public MadmateModifier MyRole = new MadmateModifier();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    void HasAssignmentRoutine.TryAssign(Virial.Assignable.IRoleTable roleTable)
    {
        var crewmates = roleTable.GetPlayers(RoleCategory.CrewmateRole).Where(p => p.role.CanLoad(this)).OrderBy(_ => Guid.NewGuid()).ToArray();
        int index = 0;

        int maxNum = NumToSpawnOption;
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
            assign100 = RoleChanceOption == 100 ? NumToSpawnOption : 0;
            assignRandom = RoleChanceOption == 100 ? 0 : NumToSpawnOption;
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
        List<byte> impostors = new();
        public Instance(GamePlayer player) : base(player) { }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd != NebulaGameEnd.ImpostorWin;

        void SetMadmateTask()
        {
            if (AmOwner)
            {
                if (CanIdentifyImpostorsOption > 0)
                {
                    var numOfTasksOptions = NumOfTasksToIdentifyImpostorsOptions.Take(CanIdentifyImpostorsOption);
                    int max = numOfTasksOptions.Max(option => option.Value);

                    using (RPCRouter.CreateSection("MadmateTask"))
                    {
                        MyPlayer.Tasks.Unbox().ReplaceTasksAndRecompute(max, 0, 0);
                        MyPlayer.Tasks.Unbox().BecomeToOutsider();
                    }
                }
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            SetMadmateTask();
            if (AmOwner) IdentifyImpostors();
        }

        public void OnGameStart(GameStartEvent ev)
        {
            SetMadmateTask();
            if (AmOwner) IdentifyImpostors();
        }

        private void IdentifyImpostors()
        {
            //インポスター判別のチャンスだけ繰り返す
            while (CanIdentifyImpostorsOption > impostors.Count && MyPlayer.Tasks.CurrentCompleted >= NumOfTasksToIdentifyImpostorsOptions[impostors.Count].Value)
            {
                var pool = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p.Role.Role.Category == RoleCategory.ImpostorRole && !impostors.Contains(p.PlayerId)).ToArray();
                //候補が残っていなければ何もしない
                if (pool.Length == 0) return;
                //生存しているインポスターだけに絞っても候補がいるなら、そちらを優先する。
                if (pool.Any(p => !p.IsDead)) pool = pool.Where(p => !p.IsDead).ToArray();

                impostors.Add(pool[System.Random.Shared.Next(pool.Length)].PlayerId);

                if (MyPlayer.Tasks.CurrentCompleted > 0) new StaticAchievementToken("madmate.common2");
            }
        }

        [OnlyMyPlayer]
        public void OnTaskCompleteLocal(PlayerTaskCompleteLocalEvent ev) => IdentifyImpostors();


        [Local]
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (impostors.Contains(ev.Player.PlayerId) && ev.Player.IsImpostor) ev.Color = new(Palette.ImpostorRed);
        }

        [Local, OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer?.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                new StaticAchievementToken("madmate.another1");
                if (ev.Murderer != null && impostors.Contains(ev.Murderer.PlayerId)) new StaticAchievementToken("madmate.another2");
                if (ev.Murderer != null && !impostors.Contains(ev.Murderer.PlayerId) && impostors.Count > 0) new StaticAchievementToken("madmate.another3");
            }
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == NebulaGameEnd.ImpostorWin && NebulaGameManager.Instance!.AllPlayerInfo.All(p => p.Role.Role.Category != RoleCategory.ImpostorRole || !p.IsDead))
                new StaticAchievementToken("madmate.challenge");
        }

        bool RuntimeModifier.InvalidateCrewmateTask => true;
        bool RuntimeModifier.MyCrewmateTaskIsIgnored => true;

        string editRoleName(string name, bool isShort)
        {
            if (isShort) return Language.Translate("role.madmateModifier.prefix.short").Color(MyRole.UnityColor) + name;
            else return Language.Translate("role.madmateModifier.prefix").Color(MyRole.UnityColor) + name;
        }

        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort) => editRoleName(lastRoleName, isShort);
    }
}

public static class MadmateExtensions
{
    public static bool IsMadmate(this Virial.Game.Player player)
    {
        return player != null && player.Role.Role == Crewmate.Madmate.MyRole || (player?.TryGetModifier<MadmateModifier.Instance>(out _) ?? false);
    }
}