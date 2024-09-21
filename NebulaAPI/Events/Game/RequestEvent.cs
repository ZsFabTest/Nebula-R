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
    public int[] arguments { get; private init; }
    public RequestEvent(string info, int[]? arguments = null)
    {
        requestInfo = info;
        requestResult = false;
        this.arguments = arguments ?? new int[] { };
    }
    public void Report(bool flag) => requestResult |= flag;
}
