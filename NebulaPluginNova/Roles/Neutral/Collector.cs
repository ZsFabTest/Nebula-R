using Nebula.Behaviour;
using Nebula.Modules;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using static MeetingHud;

namespace Nebula.Roles.Neutral;

public class Collector : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static RoleTeam MyTeam = new Team("teams.collector", new(163, 73, 164), TeamRevealType.OnlyMe);
    private Collector() : base("collector",new(163, 73, 164), RoleCategory.NeutralRole, MyTeam, [CollectedVotesToWinOption, CanCallEmergencyMeetingOption]) { }
    Citation? HasCitation.Citaion => Citations.TownOfHostY;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments);

    private static IntegerConfiguration CollectedVotesToWinOption = NebulaAPI.Configurations.Configuration("options.role.collector.collectedVotesToWin", (1,30), 15);
    static private BoolConfiguration CanCallEmergencyMeetingOption = NebulaAPI.Configurations.Configuration("options.role.collector.canCallEmergencyMeeting", true);

    public static Collector MyRole = new Collector();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        private int collection = 0;
        private byte voteFor = byte.MaxValue;

        int[]? RuntimeAssignable.RoleArguments => new int[] { collection };

        public Instance(GamePlayer player, int[] arguments) : base(player)
        {
            collection = arguments.Get(0,0);
        }

        void RuntimeAssignable.OnActivated()
        {
            voteFor = byte.MaxValue;
        }

        [Local]
        void OnVotesCalcuationEnd(VoteCalcuationEndEvent ev)
        {
            var dictionary = ev.CalVoteResult();
            collection += dictionary[voteFor];
            if (dictionary[voteFor] == 1)
            {
                new StaticAchievementToken("collector.another1");
                return;
            }
            if (dictionary[voteFor] >= 5) new StaticAchievementToken("collector.common1");
            if (dictionary[voteFor] >= 10) new StaticAchievementToken("collector.challenge");
        }

        [Local]
        void OnCastVoteLocal(PlayerVoteCastLocalEvent ev)
        {
            voteFor = ev.VoteFor?.PlayerId ?? byte.MaxValue;
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!MyPlayer.IsDead && collection >= CollectedVotesToWinOption)
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.CollectorWin, 1 << MyPlayer.PlayerId);
        }

        [Local]
        void AppendExtraTaskText(PlayerTaskTextLocalEvent ev)
        {
            var text = Language.Translate("role.collector.taskText");
            var detail = $"{collection}/{CollectedVotesToWinOption.GetValue()}";

            ev.AppendText(text.Replace("%DETAIL%", detail).Color(MyRole.UnityColor));
        }

        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }
}