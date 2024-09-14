using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Game;

internal class RequestEvent : Event
{
    public string requestInfo { get; private init; }
    public bool requestResult { get; private set; }
    public RequestEvent(string info)
    {
        requestInfo = info;
        requestResult = false;
    }
    public void Report(bool flag) => requestResult |= flag;
}
