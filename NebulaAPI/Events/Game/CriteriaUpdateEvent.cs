using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Game;

namespace Virial.Events.Game;

/// <summary>
/// 在检查船员、伪装者、豺狼团队、巴普洛夫团队胜利时被调用
/// </summary>
public class CriteriaUpdateEvent : Event
{
    /// <summary>
    /// 当前更新的游戏结果
    /// </summary>
    public GameEnd CriterialGameEnd { get; private init; }

    /// <summary>
    /// 当前判断的游戏结束原因
    /// </summary>
    public GameEndReason CriterialGameEndReason { get; private init; }

    /// <summary>
    /// 若为是 则无论该次检查结果如何禁止游戏结束
    /// </summary>
    public bool blockWinning { get; private set; }

    internal CriteriaUpdateEvent(GameEnd end, GameEndReason reason)
    {
        CriterialGameEnd = end;
        CriterialGameEndReason = reason;
        blockWinning = false;
    }

    /// <summary>
    /// 修改是否阻止胜利
    /// </summary>
    public void BlockWinning(bool flag) => blockWinning |= flag;
}
