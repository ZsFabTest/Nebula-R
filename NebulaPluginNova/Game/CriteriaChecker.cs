using Nebula.Behaviour;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Virial;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Game;

[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
public class NebulaEndCriteria
{
    static NebulaEndCriteria()
    {
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new SabotageCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new CrewmateCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new ImpostorCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new JackalCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new LoversCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new JesterCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new PavlovCriteria().Register(NebulaAPI.CurrentGame!));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new MoriartyCriteria().Register(NebulaAPI.CurrentGame!));
    }


    int gameModeMask;

    public bool IsValidCriteria => (gameModeMask & GeneralConfigurations.CurrentGameMode) != 0;

    public Func<CustomEndCondition?>? OnUpdate = null;
    public Func<PlayerControl?, Tuple<CustomEndCondition, int>?>? OnExiled = null;
    public Func<CustomEndCondition?>? OnTaskUpdated = null;

    public NebulaEndCriteria(int gameModeMask = 0xFFFF)
    {
        this.gameModeMask = gameModeMask;
    }

    public static (int totalAlive, int allEvil, int impostor, int madmate, int jackal, int pavlov, int moriarty) GetRoleData()
    {
        (int totalAlive, int allEvil, int impostor, int madmate, int jackal, int pavlov, int moriarty) result = new(0, 0, 0, 0, 0, 0, 0);
        NebulaGameManager.Instance?.AllPlayerInfo().Do(p =>
        {
            if (p.IsDead) return;
            result.totalAlive++;
            if (p.Role.Role.Team == Impostor.MyTeam)
            {
                result.allEvil++;
                result.impostor++;
                return;
            }
            if (p.Role.Role.Team == Jackal.MyTeam || p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole))
            {
                result.allEvil++;
                result.jackal++;
                return;
            }
            if (p.IsMadmate()) result.madmate++;
            if (p.Role.Role.Team == Pavlov.MyTeam)
            {
                result.allEvil++;
                result.pavlov++;
                return;
            }
            if (p.Role.Role.Team == Moriarty.MyTeam)
            {
                result.allEvil++;
                result.moriarty++;
                return;
            }
        });
        return result;
    }

    private class SabotageCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.ImpostorWin, GameEndReason.Sabotage);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            if (ShipStatus.Instance != null)
            {
                var status = ShipStatus.Instance;
                if (status.Systems != null)
                {
                    ISystemType? systemType = status.Systems.ContainsKey(SystemTypes.LifeSupp) ? status.Systems[SystemTypes.LifeSupp] : null;
                    if (systemType != null)
                    {
                        LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>()!;
                        if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                        {
                            lifeSuppSystemType.Countdown = 10000f;
                            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Sabotage);
                        }
                    }

                    foreach (ISystemType systemType2 in ShipStatus.Instance.Systems.Values)
                    {
                        ICriticalSabotage? criticalSabotage = systemType2.TryCast<ICriticalSabotage>();
                        if (criticalSabotage != null && criticalSabotage.Countdown < 0f)
                        {
                            criticalSabotage.ClearSabotage();
                            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Sabotage);
                        }
                    }
                }
            }
        }
    };

    private class CrewmateCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.CrewmateWin, GameEndReason.Situation);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            if (GetRoleData().allEvil > 0) return;

            NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Situation);
        }

        void OnTaskUpdate(PlayerTaskUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.CrewmateWin, GameEndReason.Task);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            int quota = 0;
            int completed = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDisconnected) continue;

                if (!p.Tasks.IsCrewmateTask) continue;
                quota += p.Tasks.Quota;
                completed += p.Tasks.TotalCompleted;
            }
            if (quota > 0 && quota <= completed) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Task);
        }
    };

    private class ImpostorCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.ImpostorWin, GameEndReason.Situation);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            /*
            int impostors = 0;
            int totalAlive = 0;
            int madmates = 0;
            
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;

                //Loversではないインポスターのみカウントに入れる
                if (p.Role.Role.Team == Impostor.MyTeam && !p.Unbox().TryGetModifier<Lover.Instance>(out _)) impostors++;

                if (p.IsMadmate())
                {
                    madmates++;
                    continue;
                }

                //ジャッカル陣営が生存している間は勝利できない
                if (p.Role.Role.Team == Jackal.MyTeam || p.Unbox().AllModifiers.Any(m => m.Modifier == SidekickModifier.MyRole)) return;
            }
            */

            var roleData = GetRoleData();
            if (roleData.allEvil - roleData.impostor > 0) return;
            int totalAlive = roleData.totalAlive, impostors = roleData.impostor, madmates = roleData.madmate;

            if(impostors * 2 >= (totalAlive - madmates)) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Situation);
        }
    };

    private class JackalCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            /*
            int totalAlive = NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead);

            bool isJackalTeam(GamePlayer p) => p.Role.Role.Team == Jackal.MyTeam || p.Unbox().AllModifiers.Any(m => m.Modifier == SidekickModifier.MyRole);

            int totalAliveAllJackals = 0;

            //全体の生存しているジャッカルの人数を数えると同時に、ジャッカル陣営が勝利できない状況なら調べるのをやめる
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;

                if (isJackalTeam(p) && !p.TryGetModifier<MadmateModifier.Instance>(out _)) totalAliveAllJackals++;

                //ラバーズが生存している間は勝利できない
                if (p.Unbox().TryGetModifier<Lover.Instance>(out _)) return;
                //インポスターが生存している間は勝利できない
                if (p.Role.Role.Team == Impostor.MyTeam) return;
            }
            */

            var roleData = GetRoleData();
            if (roleData.allEvil - roleData.jackal > 0) return;
            int totalAlive = roleData.totalAlive, totalAliveAllJackals = roleData.jackal;

            //全ジャッカルに対して、各チームごとに勝敗を調べる
            foreach (var jackal in NebulaGameManager.Instance!.AllPlayerInfo().Where(p => !p.IsDead && p.Role.Role == Roles.Neutral.Jackal.MyRole))
            {
                var jRole = (jackal.Role as Roles.Neutral.Jackal.Instance);
                if (!(jRole?.CanWinDueToKilling ?? false)) continue;

                int aliveJackals = NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead && (jRole!.IsSameTeam(p)));
                
                //他のJackal陣営が生きていたら勝利できない
                if (aliveJackals < totalAliveAllJackals) continue;

                if (aliveJackals * 2 >= totalAlive) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            }
        }
    };

    private class LoversCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            int totalAlive = GetRoleData().totalAlive;
            if (totalAlive > 3) return;

            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead) continue;
                totalAlive++;
                if (p.Unbox().TryGetModifier<Lover.Instance>(out var lover)){
                    if (lover.MyLover?.IsDead ?? true) continue;

                    NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.LoversWin, GameEndReason.Situation);
                }
            }
        }
    };

    private class JesterCriteria : IModule, IGameOperator
    {
        void OnExiled(PlayerExiledEvent ev) 
        {
            if (ev.Player?.Role.Role == Roles.Neutral.Jester.MyRole) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JesterWin, GameEndReason.Special, BitMasks.AsPlayer(1u << ev.Player.PlayerId));
        }
    };

    private class PavlovCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.PavlovWin, GameEndReason.Situation);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            var roleData = GetRoleData();
            if (roleData.allEvil - roleData.pavlov > 0) return;

            foreach (var pavlov in NebulaGameManager.Instance!.AllPlayerInfo().Where((p) => p.Role.Role == Roles.Neutral.Pavlov.MyRole))
            {
                var pRole = (pavlov.Role as Roles.Neutral.Pavlov.Instance);

                int alivePavlovs = NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead && (pRole!.IsSameTeam(p)));

                if (alivePavlovs < roleData.pavlov) continue;

                if (alivePavlovs * 2 >= roleData.totalAlive) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.PavlovWin, GameEndReason.Situation);
            }
        }
    }

    private class MoriartyCriteria : IModule, IGameOperator
    {
        void OnUpdate(GameUpdateEvent ev)
        {
            var criteriaUpdateEvent = new CriteriaUpdateEvent(NebulaGameEnd.MoriartyWin, GameEndReason.Situation);
            GameOperatorManager.Instance?.Run(criteriaUpdateEvent);
            if (criteriaUpdateEvent.blockWinning) return;

            var roleData = GetRoleData();
            if (roleData.allEvil - roleData.pavlov > 0) return;

            foreach (var moriarty in NebulaGameManager.Instance!.AllPlayerInfo().Where((p) => p.Role.Role == Roles.Neutral.Moriarty.MyRole))
            {
                var pRole = (moriarty.Role as Roles.Neutral.Moriarty.Instance);

                int aliveMoriarty = NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead && (pRole!.IsSameTeam(p)));

                if (aliveMoriarty < roleData.moriarty) continue;

                if (aliveMoriarty * 2 >= roleData.totalAlive) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.MoriartyWin, GameEndReason.Situation);
            }
        }
    }
}

public class CriteriaManager
{
    private record TriggeredGameEnd(Virial.Game.GameEnd gameEnd, Virial.Game.GameEndReason reason, BitMask<Virial.Game.Player>? additionalWinners);
    private List<TriggeredGameEnd> triggeredGameEnds = new();
    
    public void Trigger(Virial.Game.GameEnd gameEnd, Virial.Game.GameEndReason reason, BitMask<Virial.Game.Player>? additionalWinners)
    {
        triggeredGameEnds.Add(new(gameEnd, reason, additionalWinners));
    }

    public void CheckAndTriggerGameEnd()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        //if (AssassinSystem.isAssassinMeeting) return;

        //終了条件が確定済みなら何もしない
        if (NebulaGameManager.Instance?.EndState != null) return;

        if ((ExileController.Instance) && !Minigame.Instance) triggeredGameEnds.RemoveAll(t => t.reason == GameEndReason.Situation);

        if(triggeredGameEnds.Count == 0) return;

        var end = triggeredGameEnds.MaxBy(g => g.gameEnd.Priority);
        triggeredGameEnds.Clear();

        if (end == null) return;

        NebulaGameManager.Instance?.InvokeEndGame(end.gameEnd, end.reason, end.additionalWinners != null ? (NebulaGameManager.Instance.AllPlayerInfo().Aggregate(0, (v, p) => end.additionalWinners.Test(p) ? (v | (1 << p.PlayerId)) : v)) : 0);
    }
}
