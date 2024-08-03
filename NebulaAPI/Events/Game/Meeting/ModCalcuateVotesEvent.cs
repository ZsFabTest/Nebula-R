using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Game.Meeting;

public class ModCalcuateVotesEvent : Event
{
    private Dictionary<byte, int> VoteResult;

    public Dictionary<byte, int> CalVoteResult()
    {
        return VoteResult;
    }

    internal ModCalcuateVotesEvent(Dictionary<byte,int> dic)
    {
        this.VoteResult = dic;
    }
}
