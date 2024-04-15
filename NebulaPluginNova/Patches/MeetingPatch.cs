﻿using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using Nebula.Modules;
using UnityEngine.Rendering;
using Nebula.Behaviour;
using static MeetingHud;
using Steamworks;
using System.Reflection;
using Nebula.Map;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Nebula.Patches;

[NebulaRPCHolder]
public static class MeetingModRpc
{
    private static Vector3 ToVoteAreaPos(int index)
    {
        int x = index % 3;
        int y = index / 3;
        return MeetingHud.Instance.VoteOrigin + new Vector3(MeetingHud.Instance.VoteButtonOffsets.x * (float)x, MeetingHud.Instance.VoteButtonOffsets.y * (float)y, -0.9f - (float)y * 0.01f);
    }

    public static void SortVotingArea(this MeetingHud __instance, Func<PlayerModInfo, int> rank)
    {
        var ordered = __instance.playerStates.OrderBy(p => p.TargetPlayerId + 32 * rank.Invoke(NebulaGameManager.Instance!.GetModPlayerInfo(p.TargetPlayerId)!)).ToArray();

        for(int i = 0; i < ordered.Length; i++)
            __instance.StartCoroutine(ordered[i].transform.Smooth(ToVoteAreaPos(i), 1.5f).WrapToIl2Cpp());
    }

    public static readonly RemoteProcess RpcBreakEmergencyButton = new("BreakEmergencyButton",
        (_) => ShipStatus.Instance.BreakEmergencyButton());

    public static readonly RemoteProcess<(int voteMask, bool canSkip, float votingTime, bool exileEvenIfTie)> RpcChangeVotingStyle = new("ChangeVotingStyle",
        (message,_) =>
        {
            MeetingHudExtension.VotingMask = message.voteMask;
            MeetingHudExtension.CanSkip = message.canSkip;
            MeetingHudExtension.ExileEvenIfTie = message.exileEvenIfTie;

            MeetingHud.Instance.ResetPlayerState();

            MeetingHud.Instance.SortVotingArea(p =>
            {
                if (((1 << p.PlayerId) & message.voteMask) != 0) return 0;
                if (p.IsDead) return 2;
                return 1;
            });

            MeetingHudExtension.ReflectVotingMask();

            MeetingHudExtension.VotingTimer = message.votingTime;
            MeetingHud.Instance.lastSecond = Mathf.Min(11, (int)message.votingTime);
        }
        );

    public static readonly RemoteProcess<(byte reporter,byte reported)> RpcNoticeStartMeeting = new("ModStartMeeting",
    (message,_) =>
    {
        var reporter = NebulaGameManager.Instance?.GetModPlayerInfo(message.reporter);
        var reported = NebulaGameManager.Instance?.GetModPlayerInfo(message.reported);

        GameEntityManager.Instance?.AllEntities.Do(a => a.OnPreMeetingStart(reporter!, reported));

        if (reported != null)
            GameEntityManager.Instance?.AllEntities.Do(a => a.OnReported(reporter!, reported));
        else
            GameEntityManager.Instance?.AllEntities.Do(a => a.OnEmergencyMeeting(reporter!));
    });

    public static readonly RemoteProcess<(List<VoterState> states, byte exiled, byte[] exiledAll,  bool tie)> RpcModCompleteVoting = new("CompleteVoting", 
        (writer,message) => {
            writer.Write(message.states.Count);
            foreach(var state in message.states)
            {
                writer.Write(state.VoterId);
                writer.Write(state.VotedForId);
            }
            writer.Write(message.exiled);
            writer.WriteBytesAndSize(message.exiledAll);
            writer.Write(message.tie);
        },
        (reader) => {
            List<VoterState> states = new();
            int statesNum = reader.ReadInt32();
            for (int i = 0; i < statesNum; i++)
            {
                var state = new VoterState() { VoterId = reader.ReadByte(), VotedForId = reader.ReadByte() };
                states.Add(state);
            }

            return (states,reader.ReadByte(),reader.ReadBytesAndSize(), reader.ReadBoolean());
        },
        (message, _) => {
            ForcelyVotingComplete(MeetingHud.Instance, message.states, message.exiled, message.exiledAll, message.tie);
        }
        );

    private static void ForcelyVotingComplete(MeetingHud meetingHud, List<VoterState> states, byte exiled, byte[] exiledAll, bool tie)
    {
        var readonlyStates = states.ToArray();

        GameEntityManager.Instance?.GetPlayerEntities(PlayerControl.LocalPlayer.PlayerId).Do(e =>
        {
            var voted = Helpers.GetPlayer(
            ((VoterState?)states.FirstOrDefault(s => s.VoterId == PlayerControl.LocalPlayer.PlayerId))?.VotedForId ?? 255);

            e.OnVotedLocal(voted, exiledAll.Contains(voted?.PlayerId ?? 255));

            var votedBy = states.Where(s => s.VotedForId == PlayerControl.LocalPlayer.PlayerId).Select(s => s.VoterId).Distinct().Select(id => Helpers.GetPlayer(id)).Distinct().ToArray();
            e.OnVotedForMeLocal(votedBy!);

            e.OnDiscloseVotingLocal(readonlyStates);
        });

        //追放者とタイ投票の結果だけは必ず書き換える
        meetingHud.exiledPlayer = Helpers.GetPlayer(exiled)?.Data;
        meetingHud.wasTie = tie;
        MeetingHudExtension.ExiledAll = exiledAll.Select(p => Helpers.GetPlayer(p)!).ToArray();

        if (meetingHud.state == MeetingHud.VoteStates.Results) return;

        meetingHud.state = MeetingHud.VoteStates.Results;
        meetingHud.SkipVoteButton.gameObject.SetActive(false);
        meetingHud.SkippedVoting.gameObject.SetActive(true);
        AmongUsClient.Instance.DisconnectHandlers.Remove(meetingHud.TryCast<IDisconnectHandler>());
        for (int i = 0; i < GameData.Instance.PlayerCount; i++)
        {
            PlayerControl @object = GameData.Instance.AllPlayers[i].Object;
            if (@object != null && @object.Data != null && @object.Data.Role) @object.Data.Role.OnVotingComplete();
        }
        meetingHud.PopulateResults(states.ToArray());
        meetingHud.SetupProceedButton();
        try
        {
            MeetingHud.VoterState voterState = states.FirstOrDefault((MeetingHud.VoterState s) => s.VoterId == PlayerControl.LocalPlayer.PlayerId);
            GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(voterState.VotedForId);
        }
        catch 
        {
        }

        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
            ControllerManager.Instance.CloseOverlayMenu(DestroyableSingleton<HudManager>.Instance.Chat.name);
        }
        ControllerManager.Instance.CloseOverlayMenu(meetingHud.name);
        ControllerManager.Instance.OpenOverlayMenu(meetingHud.name, null, meetingHud.ProceedButtonUi);
    }
}

[HarmonyPatch(typeof(MeetingCalledAnimation), nameof(MeetingCalledAnimation.CoShow))]
class MeetingCalledAnimationPatch
{
    public static void Prefix(MeetingCalledAnimation __instance)
    {
        if(ClientOption.AllOptions[ClientOption.ClientOptionType.ForceSkeldMeetingSE].Value == 1)
        {
            bool isEmergency = __instance.Stinger == ShipStatus.Instance.EmergencyOverlay.Stinger;
            __instance.Stinger = (isEmergency ? VanillaAsset.MapAsset[0].EmergencyOverlay : VanillaAsset.MapAsset[0].ReportOverlay).Stinger;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        //会議室が開くか否かのチェック
        if (AmongUsClient.Instance.IsGameOver || MeetingHud.Instance) return false;
        
        //フェイクタスクでない緊急タスクがある場合ボタンは押せない
        if (target == null &&
            PlayerControl.LocalPlayer.myTasks.Find((Il2CppSystem.Predicate<PlayerTask>)
            (task => PlayerTask.TaskIsEmergency(task) && 
                (NebulaGameManager.Instance?.LocalFakeSabotage?.MyFakeTasks.All(
                    type => ShipStatus.Instance.GetSabotageTask(type)?.TaskType != task.TaskType) ?? true))) != null)
                return false;
            
        

        if (__instance.Data.IsDead) return false;
        MeetingRoomManager.Instance.AssignSelf(__instance, target);
        if (!AmongUsClient.Instance.AmHost) return false;
        
        HudManager.Instance.OpenMeetingRoom(__instance);
        __instance.RpcStartMeeting(target);

        MeetingModRpc.RpcNoticeStartMeeting.Invoke((__instance.PlayerId, target?.PlayerId ?? 255));

        if (target == null)
        {
            NebulaGameManager.Instance!.EmergencyCalls++;
            if (NebulaGameManager.Instance!.EmergencyCalls == GeneralConfigurations.NumOfMeetingsOption) MeetingModRpc.RpcBreakEmergencyButton.Invoke();
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
class StartMeetingPatch
{
    public static void Prefix()
    {
        //会議前の位置を共有する
        PlayerModInfo.RpcSharePreMeetingPoint.Invoke((PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.transform.position));
    }
}


[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.canBeHighlighted))]
class MeetingCanBeHighlightedPatch
{
    public static void Postfix(PlayerVoteArea __instance,ref bool __result)
    {
        __result = __result && (MeetingHudExtension.VotingMask & (1 << __instance.TargetPlayerId)) != 0;
    }
}

[HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.CoRun))]
class MeetingIntroStartPatch
{
    public static void Postfix(MeetingIntroAnimation __instance,ref Il2CppSystem.Collections.IEnumerator __result)
    {
        __result = Effects.Sequence(
            Effects.Action((Il2CppSystem.Action)(() =>
            {
                NebulaGameManager.Instance?.OnMeetingStart();
            })),
            __result            
            );
    }
}


[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingStartPatch
{
    static private ISpriteLoader LightColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorLight.png", 100f);
    static private ISpriteLoader DarkColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorDark.png", 100f);

    class MeetingPlayerContent
    {
        public TMPro.TextMeshPro NameText = null!, RoleText = null!;
        public PlayerModInfo Player = null!;
    }

    static private void Update(List<MeetingPlayerContent> meetingContent)
    {
        foreach(var content in meetingContent)
        {
            try
            {
                if (content.NameText) content.Player.UpdateNameText(content.NameText, true);
                if (content.RoleText) content.Player.UpdateRoleText(content.RoleText);
            }
            catch
            {
                if(content.RoleText) content.RoleText.gameObject.SetActive(false);
            }
        }

        foreach(var p in MeetingHud.Instance.playerStates)
        {
            p.PlayerIcon.cosmetics.hat.FrontLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            p.PlayerIcon.cosmetics.hat.BackLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            p.PlayerIcon.cosmetics.visor.Image.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            p.PlayerIcon.cosmetics.skin.layer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            p.PlayerIcon.cosmetics.currentBodySprite.BodySprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            p.XMark.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }
    }

    static void Postfix(MeetingHud __instance)
    {
        MeetingHudExtension.Reset();
        MeetingHudExtension.InitMeetingTimer();

        NebulaManager.Instance.CloseAllUI();

        List<MeetingPlayerContent> allContents = new();

        __instance.transform.localPosition = new Vector3(0f, 0f, -25f);


        //色の明暗を表示
        foreach (var player in __instance.playerStates)
        {
            bool isLightColor = DynamicPalette.IsLightColor(Palette.PlayerColors[player.TargetPlayerId]);

            SpriteRenderer renderer = UnityHelper.CreateObject<SpriteRenderer>("Color", player.transform, new Vector3(1.2f, -0.18f, -1f));
            renderer.sprite = isLightColor ? LightColorSprite.GetSprite() : DarkColorSprite.GetSprite();

            //色テキストをプレイヤーアイコンそばに移動
            var localPos = player.ColorBlindName.transform.localPosition;
            localPos.x = -0.947f;
            localPos.z -= 0.15f;
            player.ColorBlindName.transform.localPosition = localPos;

            var roleText = GameObject.Instantiate(player.NameText, player.transform);
            roleText.transform.localPosition = new Vector3(0.3384f, -0.13f, -0.02f);
            roleText.transform.localScale = new Vector3(0.57f, 0.57f);
            roleText.rectTransform.sizeDelta += new Vector2(0.35f, 0f);

            allContents.Add(new() { Player = NebulaGameManager.Instance!.GetModPlayerInfo(player.TargetPlayerId)!, NameText = player.NameText, RoleText = roleText });

            player.CancelButton.GetComponent<SpriteRenderer>().material = __instance.Glass.material;
            player.ConfirmButton.GetComponent<SpriteRenderer>().material = __instance.Glass.material;
            player.CancelButton.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;
            player.ConfirmButton.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;

            var button = player.PlayerButton.Cast<PassiveButton>();
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            button.OnClick.AddListener(() =>
            {
                if (player.canBeHighlighted()) player.Select();
            });
            button.OnMouseOver.AddListener(() =>
            {
                if (player.canBeHighlighted()) player.SetHighlighted(true);
            });
        }

        Update(allContents);

        IEnumerator CoUpdate()
        {
            while (true)
            {
                Update(allContents);
                yield return null;
            }
        }
        __instance.StartCoroutine(CoUpdate().WrapToIl2Cpp());
    }
}


[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    static bool Prefix(MeetingHud __instance)
    {
        if (__instance.state == MeetingHud.VoteStates.Animating) return false;

        __instance.UpdateButtons();

        switch (__instance.state)
        {
            case MeetingHud.VoteStates.Discussion:

                MeetingHudExtension.DiscussionTimer -= Time.deltaTime;
                if (MeetingHudExtension.DiscussionTimer > 0f)
                {
                    //議論時間中
                    __instance.UpdateTimerText(StringNames.MeetingVotingBegins, Mathf.CeilToInt(MeetingHudExtension.DiscussionTimer));
                    for (int i = 0; i < __instance.playerStates.Length; i++) __instance.playerStates[i].SetDisabled();
                    __instance.SkipVoteButton.SetDisabled();
                    return false;
                }

                //議論時間から投票時間へ
                __instance.state = MeetingHud.VoteStates.NotVoted;
                bool active = MeetingHudExtension.VotingTimer > 0;
                __instance.TimerText.gameObject.SetActive(active);

                __instance.discussionTimer = (float)GameManager.Instance.LogicOptions.CastFast<LogicOptionsNormal>().GetDiscussionTime();

                MeetingHud.Instance!.lastSecond = 11;

                MeetingHudExtension.ReflectVotingMask();

                return false;
            case MeetingHud.VoteStates.NotVoted:
            case MeetingHud.VoteStates.Voted:
                MeetingHudExtension.VotingTimer -= Time.deltaTime;
                if (MeetingHudExtension.VotingTimer > 0f)
                {
                    //投票時間中
                    int intCnt = Mathf.CeilToInt(MeetingHudExtension.VotingTimer);
                    __instance.UpdateTimerText(StringNames.MeetingVotingEnds, intCnt);
                    if (__instance.state == MeetingHud.VoteStates.NotVoted && intCnt < __instance.lastSecond)
                    {
                        __instance.lastSecond = intCnt;
                        __instance.StartCoroutine(Effects.PulseColor(__instance.TimerText, Color.red, Color.white, 0.25f));
                        SoundManager.Instance.PlaySound(__instance.VoteEndingSound, false, 1f, null).pitch = Mathf.Lerp(1.5f, 0.8f, (float)__instance.lastSecond / 10f);
                    }
                }
                else
                {
                    //結果開示へ
                    __instance.ForceSkipAll();
                }
                break;

            case MeetingHud.VoteStates.Results:
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                {
                    MeetingHudExtension.ResultTimer -= Time.deltaTime;
                    __instance.UpdateTimerText(StringNames.MeetingProceeds, Mathf.CeilToInt(MeetingHudExtension.ResultTimer));
                    if (AmongUsClient.Instance.AmHost && MeetingHudExtension.ResultTimer <= 0f) __instance.HandleProceed();
                }
                break;
        }

        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
class MeetingClosePatch
{ 
    public static void Postfix(MeetingHud __instance)
    {
        //ベント内のプレイヤー情報をリセットしておく
        VentilationSystem? ventilationSystem = ShipStatus.Instance.Systems[SystemTypes.Ventilation].TryCast<VentilationSystem>();
        if (ventilationSystem != null) ventilationSystem.PlayersInsideVents.Clear();

        GameEntityManager.Instance?.AllEntities.Do(e => e.OnStartExileCutScene());
        NebulaManager.Instance.CloseAllUI();
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetMaskLayer))]
class VoteMaskPatch
{
    public static bool Prefix(PlayerVoteArea __instance)
    {
        return false;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetTargetPlayerId))]
class VoteAreaVCPatch
{
    private static SpriteLoader VCFrameSprite = SpriteLoader.FromResource("Nebula.Resources.MeetingVCFrame.png", 119f);
    public static void Postfix(PlayerVoteArea __instance)
    {
        try
        {
            if (GeneralConfigurations.UseVoiceChatOption)
            {
                var frame = UnityHelper.CreateObject<SpriteRenderer>("VCFrame", __instance.transform, new Vector3(0, 0, -0.5f));
                frame.sprite = VCFrameSprite.GetSprite();
                frame.color = Color.clear;
                var col = Palette.PlayerColors[__instance.TargetPlayerId];
                if(Mathf.Max((int)col.r, (int)col.g, (int)col.b) < 100) col = Color.Lerp(col, Color.white, 0.4f);
                
                var client = NebulaGameManager.Instance?.VoiceChatManager?.GetClient(__instance.TargetPlayerId);
                float alpha = 0f;
                if (client != null)
                {
                    var script = frame.gameObject.AddComponent<ScriptBehaviour>();
                    script.UpdateHandler += () =>
                    {
                        if (client.Level > 0.09f)
                            alpha = Mathf.Clamp(alpha + Time.deltaTime * 4f, 0f, 1f);
                        else
                            alpha = Mathf.Clamp(alpha - Time.deltaTime * 4f, 0f, 1f);
                        col.a = (byte)(alpha * 255f);
                        frame.color = col;
                    };
                }
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Start))]
class VoteAreaPatch
{
    public static void Postfix(PlayerVoteArea __instance)
    {
        if (!MeetingHud.Instance) return;

        try
        {
            var maskParent = UnityHelper.CreateObject<SortingGroup>("MaskedObjects", __instance.transform, new Vector3(0, 0, -0.1f));
            __instance.MaskArea.transform.SetParent(maskParent.transform);
            __instance.PlayerIcon.transform.SetParent(maskParent.transform);
            __instance.Overlay.maskInteraction = SpriteMaskInteraction.None;
            __instance.Overlay.material = __instance.Megaphone.material;

            var mask = __instance.MaskArea.gameObject.AddComponent<SpriteMask>();
            mask.sprite = __instance.MaskArea.sprite;
            mask.transform.localScale = __instance.MaskArea.size;
            __instance.MaskArea.enabled = false;

            __instance.Background.material = __instance.Megaphone.material;

            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            __instance.PlayerIcon.cosmetics.hat.FrontLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.hat.FrontLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            __instance.PlayerIcon.cosmetics.hat.BackLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.hat.BackLayer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            __instance.PlayerIcon.cosmetics.visor.Image.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.visor.Image.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            __instance.PlayerIcon.cosmetics.skin.layer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.skin.layer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.gameObject.AddComponent<ZOrderedSortingGroup>();
        }
        catch { }
    }
}



[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Confirm))]
class CastVotePatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte suspectStateIdx)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) return false;

        foreach (var state in __instance.playerStates)
        {
            state.ClearButtons();
            state.voteComplete = true;
        }

        __instance.SkipVoteButton.ClearButtons();
        __instance.SkipVoteButton.voteComplete = true;
        __instance.SkipVoteButton.gameObject.SetActive(false);

        if (__instance.state != MeetingHud.VoteStates.NotVoted) return false;
        
        __instance.state = MeetingHud.VoteStates.Voted;
        
        //CmdCastVote(Mod)
        int vote = 1;
        GameEntityManager.Instance.GetPlayerEntities(PlayerControl.LocalPlayer.PlayerId).Do(r => r.OnCastVoteLocal(suspectStateIdx, ref vote));
        __instance.ModCastVote(PlayerControl.LocalPlayer.PlayerId, suspectStateIdx, vote);
        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
static class CheckForEndVotingPatch
{
    public static void AddValue(this Dictionary<byte,int> self, byte target,int num)
    {
        if (self.TryGetValue(target, out var last))
            self[target] = last + num;
        else
            self[target] = num;
    }

    public static Dictionary<byte, int> ModCalculateVotes(MeetingHud __instance)
    {
        Dictionary<byte, int> dictionary = new();

        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            if (playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254)
            {
                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)i, out var vote)) vote = 1;
                dictionary.AddValue(playerVoteArea.VotedFor,vote);
            }
        }
        
        return dictionary;
    }

    public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, 0);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }

    public static bool Prefix(MeetingHud __instance)
    {
        //投票が済んでない場合、なにもしない
        if (!__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote)) return false;

        {
            Dictionary<byte, int> dictionary = ModCalculateVotes(__instance);
            KeyValuePair<byte, int> max = dictionary.MaxPair(out bool tie);

            List<byte> extraVotes = new();

            if (tie)
            {
                foreach (var state in __instance.playerStates)
                {
                    if (!state.DidVote) continue;

                    var modInfo = NebulaGameManager.Instance?.GetModPlayerInfo(state.TargetPlayerId);
                    modInfo?.AssignableAction(r=>r.OnTieVotes(ref extraVotes,state));
                }

                foreach (byte target in extraVotes) dictionary.AddValue(target, 1);

                //再計算する
                max = dictionary.MaxPair(out tie);
            }


            GameData.PlayerInfo exiled = null!;
            GameData.PlayerInfo[] exiledAll = new GameData.PlayerInfo[0];

            if (MeetingHudExtension.ExileEvenIfTie) tie = false;
            try
            {
                if (!tie)
                {
                    //投票対象で最高票を獲得しているプレイヤー全員
                    var exiledPlayers = GameData.Instance.AllPlayers.ToArray().Where(v => !v.IsDead && dictionary.GetValueOrDefault(v.PlayerId) == max.Value && ((MeetingHudExtension.VotingMask & (1 << v.PlayerId)) != 0)).ToArray();
                    exiled = exiledPlayers.First();
                    if (exiledPlayers.Length > 0) exiledAll = exiledPlayers.ToArray();
                }
            }
            catch { }
            List<MeetingHud.VoterState> allStates = new();

            //記名投票分
            foreach (var state in __instance.playerStates)
            {
                if (!state.DidVote) continue;

                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)state.TargetPlayerId, out var vote)) vote = 1;

                for (int i = 0; i < vote; i++)
                {
                    allStates.Add(new MeetingHud.VoterState
                    {
                        VoterId = state.TargetPlayerId,
                        VotedForId = state.VotedFor
                    });
                }
            }

            //追加投票分
            foreach(var votedFor in extraVotes)
            {
                allStates.Add(new MeetingHud.VoterState
                {
                    VoterId = byte.MaxValue,
                    VotedForId = votedFor
                });
            }

            allStates.Add(new() { VoterId = byte.MaxValue-1});
            //__instance.RpcVotingComplete(allStates.ToArray(), exiled, tie);
            MeetingModRpc.RpcModCompleteVoting.Invoke((allStates, exiled?.PlayerId ?? byte.MaxValue, exiledAll.Select(e => e.PlayerId).ToArray(), tie));


        }

        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
class CancelVotingCompleteDirectlyPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        Debug.Log($"Canceled VotingComplete Directly");
        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleRpc))]
class CancelVotingCompleteByRPCPatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte callId)
    {
        if (callId == 23)
        {
            Debug.Log($"Canceled VotingComplete on HandleRpc");
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
class PopulateResultPatch
{
    private static void ModBloopAVoteIcon(MeetingHud __instance,GameData.PlayerInfo? voterPlayer, int index, Transform parent,bool isExtra)
    {
        SpriteRenderer spriteRenderer = GameObject.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
        if (GameManager.Instance.LogicOptions.GetAnonymousVotes() || voterPlayer == null)
            PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
        else
            PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
        
        spriteRenderer.transform.SetParent(parent);
        spriteRenderer.transform.localScale = Vector3.zero;
        __instance.StartCoroutine(Effects.Bloop((float)index * 0.3f + (isExtra ? 0.85f : 0f), spriteRenderer.transform, 1f, isExtra ? 0.5f : 0.7f));

        if (isExtra)
            __instance.StartCoroutine(Effects.Sequence(Effects.Wait((float)index * 0.3f + 0.85f), ManagedEffects.Action(() => parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer)).WrapToIl2Cpp()));
        else
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
    }


    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)]Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState> states)
    {
        Debug.Log("Called PopulateResults");

        GameEntityManager.Instance?.AllEntities.Do(r => r.OnEndVoting());

        __instance.TitleText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults);
        foreach (var voteArea in __instance.playerStates)
        {
            voteArea.ClearForResults();
            MeetingHudExtension.LastVotedForMap[voteArea.TargetPlayerId]= voteArea.VotedFor;
        }

        int lastVoteFor = -1;
        int num = 0;
        Transform? voteFor = null;

        //OrderByは安定ソート
        foreach (var state in states.OrderBy(s => s.VotedForId)){
            if (state.VoterId == byte.MaxValue - 1) continue;
            if(state.VotedForId != lastVoteFor)
            {
                lastVoteFor = state.VotedForId;
                num = 0;
                if (state.SkippedVote)
                    voteFor = __instance.SkippedVoting.transform;
                else
                    voteFor = __instance.playerStates.FirstOrDefault((area) => area.TargetPlayerId == lastVoteFor)?.transform ?? null;
            }

            if (voteFor != null)
            {
                GameData.PlayerInfo? playerById = GameData.Instance.GetPlayerById(state.VoterId);

                ModBloopAVoteIcon(__instance, playerById, num, voteFor, state.VoterId == byte.MaxValue);
                num++;
            }
        }

        return false;
    }
}


//死体の拾い漏れチェック
[HarmonyPatch(typeof(MeetingIntroAnimation), nameof(MeetingIntroAnimation.Init))]
class MeetingIntroAnimationPatch
{
    public static void Prefix(MeetingIntroAnimation __instance, [HarmonyArgument(1)] ref Il2CppReferenceArray<GameData.PlayerInfo> deadBodies)
    {
        List<GameData.PlayerInfo> dBodies = new List<GameData.PlayerInfo>();
        //既に発見されている死体
        foreach (var dBody in deadBodies) dBodies.Add(dBody);
        
        //遅れて発見された死体
        foreach (var dBody in Helpers.AllDeadBodies())
        {
            dBodies.Add(GameData.Instance.GetPlayerById(dBody.ParentId));
            GameObject.Destroy(dBody.gameObject);
        }
        deadBodies = new Il2CppReferenceArray<GameData.PlayerInfo>(dBodies.ToArray());

        //生死を再確認
        MeetingHud.Instance.ResetPlayerState();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
class MeetingDeserializePatch
{
    public static void Postfix(MeetingHud __instance)
    {
        if (__instance.CurrentState is VoteStates.Animating or VoteStates.Discussion) return;

        __instance.UpdatePlayerState();
    }
}