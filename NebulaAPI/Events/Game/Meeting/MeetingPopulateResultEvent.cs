using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Game.Meeting;

public class MeetingPopulateResultEvent : Event
{
    internal MeetingHud Instance;
    internal MeetingPopulateResultEvent(MeetingHud instance) { this.Instance = instance; }
}
