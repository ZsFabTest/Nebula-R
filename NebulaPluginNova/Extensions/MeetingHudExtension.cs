﻿using Epic.OnlineServices.Presence;
using Il2CppSystem.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace Nebula.Extensions;


[NebulaRPCHolder]
public static class MeetingHudExtension
{
    public static void ResetPlayerState(this MeetingHud meetingHud)
    {
        Reset();
        foreach (PlayerVoteArea pva in meetingHud.playerStates)
        {
            bool isDead = NebulaGameManager.Instance?.GetModPlayerInfo(pva.TargetPlayerId)?.IsDead ?? true;

            if (pva.AmDead == isDead) continue;

            pva.SetDead(pva.DidReport, isDead);
            pva.Overlay.gameObject.SetActive(isDead);
        }

        foreach (PlayerVoteArea voter in meetingHud.playerStates)
        {
            if (!voter.DidVote) continue;

            var p = NebulaGameManager.Instance?.GetModPlayerInfo(voter.TargetPlayerId);
            if (p?.AmOwner ?? false) meetingHud.ClearVote();

            voter.ThumbsDown.enabled = false;
            voter.UnsetVote();
        }
    }

    public static Dictionary<byte, int> WeightMap= new();
    public static Dictionary<byte, int> LastVotedForMap = new();
    public static List<GameObject> LeftContents = new();
    public static List<(byte exiledId, byte sourceId, TranslatableTag playerState, TranslatableTag eventTag)> ExtraVictims = new();

    public static void Reset()
    {
        LastVotedForMap.Clear();
        WeightMap.Clear();
        ExtraVictims.Clear();
        LeftContents.Clear();
    }

    public static void AddLeftContent(GameObject obj)
    {
        obj.transform.SetParent(MeetingHud.Instance.transform);
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.localPosition = new Vector3(-4.8f, 1.8f - 1.2f * (float)LeftContents.Count, -40f);
        LeftContents.Add(obj);
    }

    public static void ExileExtraVictims()
    {
        foreach(var victims in ExtraVictims)
        {
            var player = NebulaGameManager.Instance?.GetModPlayerInfo(victims.exiledId);
            if (player == null) continue;

            player.MyControl.Exiled();
            player.MyControl.Data.IsDead = true;
            player.MyState = victims.playerState;

            player.AssignableAction(role =>
            {
                role.OnDead();
            });

            PlayerControl.LocalPlayer.GetModInfo()?.AssignableAction(r => r.OnPlayerDeadLocal(player.MyControl));

            NebulaGameManager.Instance?.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.Kill, victims.sourceId == byte.MaxValue ? null : victims.sourceId, 1 << victims.exiledId) { RelatedTag = victims.eventTag });
        }

        ExtraVictims.Clear();
    }

    public static void ModCastVote(this MeetingHud meeting, byte playerId, byte suspectIdx,int votes)
    {
        RpcModCastVote.Invoke((playerId, suspectIdx, votes));
    }

    private static RemoteProcess<(byte source, byte target, int weight)> RpcModCastVote = new(
        "CaseVote",
        (message, _) =>
        {
            WeightMap[message.source] = message.weight;
            if (AmongUsClient.Instance.AmHost) MeetingHud.Instance.CastVote(message.source, message.target);
        }
        );
    
}
