using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Player;

public class PlayerSetKillTimerEvent : AbstractPlayerEvent
{
    public float Time;
    internal PlayerSetKillTimerEvent(Virial.Game.Player player, float time) : base(player) { Time = time; }
    public void SetTime(float time) { Time = time; }
}
