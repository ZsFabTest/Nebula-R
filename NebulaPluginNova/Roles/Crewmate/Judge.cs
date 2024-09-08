using Nebula.Behaviour;
using Nebula.Game.Statistics;
using Nebula.Roles.Modifier;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;

namespace Nebula.Roles.Crewmate;

static file class JudgeSystem
{
    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.JudgeIcon.png", 115f);
    static public void OnMeetingStart(int leftGuess, bool canJudgeNeutralRoles, bool canJudgeMadmate, bool canJudgeLovers, bool canJudgeAnyone)
    {
        bool hasGuessed = false;

        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(targetSprite,
            state => {
                var p = state.MyPlayer;
                if (canJudgeAnyone)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if(p?.Role.Role.Category == RoleCategory.ImpostorRole)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if(canJudgeNeutralRoles && p?.Role.Role.Category == RoleCategory.NeutralRole)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if (canJudgeMadmate && p!.IsMadmate())
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if (canJudgeLovers && (bool)p?.TryGetModifier<Lover.Instance>(out _)!)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else
                {
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(NebulaAPI.CurrentGame.LocalPlayer, PlayerState.Misjudged, EventDetail.Missed, KillParameter.MeetingKill);
                    new StaticAchievementToken("judge.another1");
                }
                    
                hasGuessed = true;
                leftGuess--;
            },
            p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftGuess > 0 && !hasGuessed && !PlayerControl.LocalPlayer.Data.IsDead
            ));
    }

    static public void OnGameEnd(GamePlayer myInfo)
    {
        var guessKills = NebulaGameManager.Instance?.AllPlayerInfo().Count(p => p.PlayerState == PlayerState.Guessed && p.MyKiller == myInfo) ?? 0;
        if (guessKills >= 1) new StaticAchievementToken("judge.common1");
        if (!myInfo.IsDead && NebulaGameManager.Instance?.EndState?.EndCondition == NebulaGameEnds.CrewmateGameEnd && guessKills >= 3) new StaticAchievementToken("judge.challenge");
    }
}

[NebulaRPCHolder]
public class Judge : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private Judge() : base("judge", new(219, 162, 25), RoleCategory.CrewmateRole, Crewmate.MyTeam, [NumOfGuessOption, CanJudgeNeutralRolesOption, CanJudgeMadmateOption, CanJudgeLoversOption, CanCallEmergencyMeetingOption, CanJudgeAnyoneWhenBecomeMadmateOption]) 
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagBeginner);
    }

    Citation? HasCitation.Citaion => Citations.SuperNewRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private IntegerConfiguration NumOfGuessOption = NebulaAPI.Configurations.Configuration("options.role.judge.numOfGuess", (1, 15), 3);
    static private BoolConfiguration CanJudgeNeutralRolesOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeNeutralRoles", true);
    static private BoolConfiguration CanJudgeMadmateOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeMadmate", true);
    static private BoolConfiguration CanJudgeLoversOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeLovers", false);
    static private BoolConfiguration CanCallEmergencyMeetingOption = NebulaAPI.Configurations.Configuration("options.role.judge.canCallEmergencyMeeting", true);
    static private BoolConfiguration CanJudgeAnyoneWhenBecomeMadmateOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeAnyoneWhenBecomeMadmate", true);

    static public Judge MyRole = new Judge();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }

        private int leftGuess = NumOfGuessOption.GetValue();

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => JudgeSystem.OnMeetingStart(leftGuess, CanJudgeNeutralRolesOption, CanJudgeMadmateOption, CanJudgeLoversOption, CanJudgeAnyoneWhenBecomeMadmateOption && MyPlayer.IsMadmate());

        [Local]
        void OnGameEnd(GameEndEvent ev) => JudgeSystem.OnGameEnd(MyPlayer);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }
}