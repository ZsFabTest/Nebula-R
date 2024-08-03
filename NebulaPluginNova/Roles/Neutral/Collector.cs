﻿using Nebula.Behaviour;
using Nebula.Modules;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;

namespace Nebula.Roles.Neutral;

public class Collector : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static RoleTeam MyTeam = new Team("teams.collector", new(163, 73, 164), TeamRevealType.OnlyMe);
    private Collector() : base("collector",new(163, 73, 164), RoleCategory.NeutralRole, MyTeam, [CollectedVotesToWinOption]) { }
    Citation? HasCitation.Citaion => Citations.TownOfHostEdited;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static IntegerConfiguration CollectedVotesToWinOption = NebulaAPI.Configurations.Configuration("options.role.collector.collectedVotesToWin", (1,30), 15);

    public static Collector MyRole = new Collector();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        private int collection = 0;
        private Dictionary<byte, int> dictionary = new();

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated()
        {
            dictionary = new();
            if (AmOwner) collection = 0;
        }

        [Local]
        void OnModCalcuateVotes(ModCalcuateVotesEvent ev)
        {
            dictionary = ev.CalVoteResult();
        }

        [Local]
        void OnPlayerVoteDisclosed(PlayerVoteDisclosedLocalEvent ev)
        {
            var VoteFor = ev.VoteFor;
            if (VoteFor == null) return;
            collection += dictionary[VoteFor.PlayerId];
            if (dictionary[VoteFor.PlayerId] == 1)
            {
                new StaticAchievementToken("collector.another1");
                return;
            }
            if (dictionary[VoteFor.PlayerId] >= 5) new StaticAchievementToken("collector.common1");
            if (dictionary[VoteFor.PlayerId] >= 10) new StaticAchievementToken("collector.challenge");
        }

        [Local]
        void OnMeedtingEnd(MeetingEndEvent ev)
        {
            if(!MyPlayer.IsDead && collection >= CollectedVotesToWinOption)
                NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.CollectorWin, 1 << MyPlayer.PlayerId);
        }

        [Local]
        void AppendExtraTaskText(PlayerTaskTextLocalEvent ev)
        {
            var text = Language.Translate("role.collector.taskText");
            var detail = $"{collection}/{CollectedVotesToWinOption.GetValue()}";

            ev.AppendText(text.Replace("%DETAIL%", detail).Color(MyRole.UnityColor));
        }
    }
}