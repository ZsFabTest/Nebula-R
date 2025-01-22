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
    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.Remake.DecideIcon.png", 115f);
    static public void OnMeetingStart(int leftGuess, bool canJudgeNeutralRoles, bool canJudgeMadmate, bool canJudgeLovers, bool canJudgeAnyone, Action JudgeAction)
    {
        bool hasGuessed = false;

        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(targetSprite,
            state => {
                var p = state.MyPlayer;
                if (canJudgeAnyone)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, RemakeInit.PlayerStatus.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if(p?.Role.Role.Category == RoleCategory.ImpostorRole)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, RemakeInit.PlayerStatus.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if(canJudgeNeutralRoles && p?.Role.Role.Category == RoleCategory.NeutralRole)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, RemakeInit.PlayerStatus.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if (canJudgeMadmate && p!.IsMadmate())
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, RemakeInit.PlayerStatus.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else if (canJudgeLovers && (bool)p?.TryGetModifier<Lover.Instance>(out _)!)
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, RemakeInit.PlayerStatus.Judged, EventDetail.Guess, KillParameter.MeetingKill);
                else
                    NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(NebulaAPI.CurrentGame.LocalPlayer, RemakeInit.PlayerStatus.Misjudged, EventDetail.Missed, KillParameter.MeetingKill);

                JudgeAction.Invoke();
                hasGuessed = true;
                leftGuess--;
            },
            p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftGuess > 0 && !hasGuessed && !PlayerControl.LocalPlayer.Data.IsDead
            ));
    }
}

[NebulaRPCHolder]
public class Decider : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public bool IsEvilRole { get; private set; }

    private Decider(bool isEvilRole) : base(
        isEvilRole ? "evilDecider" : "niceDecider", 
        isEvilRole ? new(Palette.ImpostorRed) : new(219, 162, 25), 
        isEvilRole ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole, 
        isEvilRole ? Impostor.Impostor.MyTeam : Crewmate.MyTeam, 
        isEvilRole ? [NumOfGuessOption, CanCallEmergencyMeetingOption]
            : [NumOfGuessOption, CanJudgeNeutralRolesOption, CanJudgeMadmateOption, 
                CanJudgeLoversOption, CanCallEmergencyMeetingOption, CanJudgeAnyoneWhenBecomeMadmateOption]) 
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagBeginner);
        IsEvilRole = isEvilRole;
    }

    Citation? HasCitation.Citaion => RemakeInit.Citations.NebulaOnTheShipRLTS;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => IsEvilRole ? new EvilInstance(player, arguments.Get(0, NumOfGuessOption)) : new NiceInstance(player, arguments.Get(0, NumOfGuessOption));

    static private IntegerConfiguration NumOfGuessOption = NebulaAPI.Configurations.Configuration("options.role.judge.numOfGuess", (1, 15), 3);
    static private BoolConfiguration CanJudgeNeutralRolesOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeNeutralRoles", true);
    static private BoolConfiguration CanJudgeMadmateOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeMadmate", true);
    static private BoolConfiguration CanJudgeLoversOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeLovers", false);
    static private BoolConfiguration CanCallEmergencyMeetingOption = NebulaAPI.Configurations.Configuration("options.role.judge.canCallEmergencyMeeting", true);
    static private BoolConfiguration CanJudgeAnyoneWhenBecomeMadmateOption = NebulaAPI.Configurations.Configuration("options.role.judge.canJudgeAnyoneWhenBecomeMadmate", true);

    static public Decider MyNiceRole = new(false);
    static public Decider MyEvilRole = new(true);

    public class NiceInstance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyNiceRole;
        public NiceInstance(GamePlayer player, int left) : base(player) 
        {
            leftGuess = left;
        }

        void RuntimeAssignable.OnActivated() { }

        private int leftGuess = NumOfGuessOption.GetValue();
        int[]? RuntimeAssignable.RoleArguments => [leftGuess];

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => JudgeSystem.OnMeetingStart(leftGuess, CanJudgeNeutralRolesOption, CanJudgeMadmateOption, CanJudgeLoversOption, CanJudgeAnyoneWhenBecomeMadmateOption && MyPlayer.IsMadmate(), () => leftGuess--);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }

    public class EvilInstance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyEvilRole;
        public EvilInstance(GamePlayer player, int left) : base(player)
        {
            leftGuess = left;
        }

        void RuntimeAssignable.OnActivated() { }

        private int leftGuess = NumOfGuessOption.GetValue();
        int[]? RuntimeAssignable.RoleArguments => [leftGuess]; 

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => JudgeSystem.OnMeetingStart(leftGuess, CanJudgeNeutralRolesOption, CanJudgeMadmateOption, CanJudgeLoversOption, true, () => leftGuess--);

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }
}