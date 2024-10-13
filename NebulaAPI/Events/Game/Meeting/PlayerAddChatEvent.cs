using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Game.Meeting;

public class PlayerAddChatEvent : Event
{
    public string chatText;
    public Virial.Game.Player source { get; private init; }
    public bool isVanillaShow { get; private set; }

    internal PlayerAddChatEvent(Virial.Game.Player sourcePlayer, string chatText, bool isVanillaShow)
    {
        source = sourcePlayer;
        this.chatText = chatText;
        this.isVanillaShow = isVanillaShow;
    }

    public void SetExtraShow() => isVanillaShow = true;
}