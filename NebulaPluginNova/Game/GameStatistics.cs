﻿using AmongUs.Data.Player;
using Il2CppInterop.Runtime.Injection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using UnityEngine;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Runtime;
using Virial.Text;
using Virial.Utilities;

namespace Nebula.Game;

[NebulaPreprocessForNoS(PreprocessPhaseForNoS.PostBuildNoS)]
public static class EventDetail
{
    static public TranslatableTag Kill = new("statistics.events.kill");
    static public TranslatableTag Exiled = new("statistics.events.exiled");
    static public TranslatableTag Misfire = new("statistics.events.misfire");
    static public TranslatableTag GameStart = new("statistics.events.startGame");
    static public TranslatableTag GameEnd = new("statistics.events.endGame");
    static public TranslatableTag MeetingEnd = new("statistics.events.endMeeting");
    static public TranslatableTag Report = new("statistics.events.report");
    static public TranslatableTag BaitReport = new("statistics.events.baitReport");
    static public TranslatableTag EmergencyButton = new("statistics.events.emergency");
    static public TranslatableTag MayorButton = new("statistics.events.mayorEmergency");
    static public TranslatableTag Disconnect = new("statistics.events.disconnect");
    static public TranslatableTag Revive = new("statistics.events.revive");
    static public TranslatableTag Eat = new("statistics.events.eat");
    static public TranslatableTag Clean = new("statistics.events.clean");
    static public TranslatableTag Missed = new("statistics.events.missed");
    static public TranslatableTag Guess = new("statistics.events.guess");
    static public TranslatableTag Embroil = new("statistics.events.embroil");
    static public TranslatableTag Trap = new("statistics.events.trap");
    static public TranslatableTag Accident = new("statistics.events.accident");
    static public TranslatableTag FakeSabotage = new("statistics.events.fakeSabotage");
    static public TranslatableTag Curse = new("statistics.events.curse");
    static public TranslatableTag Layoff = new("statistics.events.layoff");
    static public TranslatableTag DestroyKill = new("statistics.events.destroy");

    static EventDetail()
    {
        Virial.Text.EventDetails.Kill = Kill;
        Virial.Text.EventDetails.Exiled = Exiled;
        Virial.Text.EventDetails.Misfire = Misfire;
        Virial.Text.EventDetails.GameStart = GameStart;
        Virial.Text.EventDetails.GameEnd = GameEnd;
        Virial.Text.EventDetails.MeetingEnd = MeetingEnd;
        Virial.Text.EventDetails.Report = Report;
        Virial.Text.EventDetails.BaitReport = BaitReport;
        Virial.Text.EventDetails.EmergencyButton = EmergencyButton;
        Virial.Text.EventDetails.Disconnect = Disconnect;
        Virial.Text.EventDetails.Revive = Revive;
        Virial.Text.EventDetails.Eat = Eat;
        Virial.Text.EventDetails.Clean = Clean;
        Virial.Text.EventDetails.Missed = Missed;
        Virial.Text.EventDetails.Guess = Guess;
        Virial.Text.EventDetails.Embroil = Embroil;
        Virial.Text.EventDetails.Trap = Trap;
        Virial.Text.EventDetails.Accident = Accident;
    }
}

public enum GameStatisticsGatherTag
{
    Spawn
}

[Flags]
internal enum PlayerTrackingFlags : byte
{
    IsDead = 0x01,
    InVent = 0x02,
    IsInvisible = 0x04,
}

/// <summary>
/// 
/// </summary>
/// <param name="Position"></param>
/// <param name="States"></param>
internal record TrackedPlayerMoment(Vector2 Position, PlayerTrackingFlags States)
{
    bool HasState(PlayerTrackingFlags flag) => (States & flag) != 0;
}

internal class TrackedMoment
{
    public TrackedPlayerMoment[] PlayerData { get; init; }
    public float Time { get; init; }

    public TrackedMoment() { }

    public static TrackedMoment CaptureCurrent()
    {
        var trackingDataArray = new TrackedPlayerMoment[NebulaGameManager.Instance!.AllPlayersNum];
        foreach(var p in NebulaGameManager.Instance.AllPlayerInfo())
        {
            PlayerTrackingFlags flag = 0;
            if (p.IsDead) flag |= PlayerTrackingFlags.IsDead;
            if (p.VanillaPlayer.inVent) flag |= PlayerTrackingFlags.InVent;
            if (p.Unbox().VisibilityLevel > 0) flag |= PlayerTrackingFlags.IsInvisible;
            trackingDataArray[p.PlayerId] = new(p.VanillaPlayer.transform.position, flag);
        }

        TrackedMoment tracked = new() { PlayerData = trackingDataArray, Time = NebulaGameManager.Instance.CurrentTime };

        return tracked;
    }
}

internal class TrackedTaskPhase
{
    public bool IsClosed { get; set; } = false;
    public List<TrackedMoment> Moments { get; init; } = new();
    public float Start { get; set; } = 0f;
    public float End { get; set; } = float.MaxValue;
    public void CaptureCurrent()
    {
        Moments.Add(TrackedMoment.CaptureCurrent());
    }
}

internal record TrackedEvent(TrackedMoment Moment, string ImageType, string TranslationKey, int LeftMask, int RightMask)
{
}

internal record ArchivedPlayer(string Name,byte Id,UnityEngine.Color32 MainColor,UnityEngine.Color32 ShadowColor, UnityEngine.Color32 VisorColor,string HatId,string VisorId,string SkinId)
{
    public static ArchivedPlayer FromPlayer(GamePlayer player)
    {
        var outfit = player.DefaultOutfit.outfit;
        byte id = player.PlayerId;
        return new ArchivedPlayer(player.Name, id, Palette.PlayerColors[id], Palette.ShadowColors[id], DynamicPalette.VisorColors[id], outfit.HatId, outfit.VisorId, outfit.SkinId);
    }

    public void ReflectTo(PoolablePlayer player, PlayerMaterial.MaskType maskType)
    {
        player.cosmetics.SetMaskType(maskType);
        Palette.PlayerColors[NebulaPlayerTab.ArchiveColorId] = MainColor;
        Palette.ShadowColors[NebulaPlayerTab.ArchiveColorId] = ShadowColor;
        
        player.cosmetics.SetBodyColor(NebulaPlayerTab.ArchiveColorId);
        player.cosmetics.SetSkin(SkinId, NebulaPlayerTab.ArchiveColorId, null);
        player.cosmetics.SetHatColor(Palette.White);
        player.cosmetics.SetVisorAlpha(1f);
        player.cosmetics.SetHat(HatId, NebulaPlayerTab.ArchiveColorId);
        player.cosmetics.SetVisor(VisorId, NebulaPlayerTab.ArchiveColorId);
        player.cosmetics.visor.Image.sharedMaterial.SetColor(PlayerMaterial.VisorColor, VisorColor);
        player.cosmetics.hat.FrontLayer.sharedMaterial.SetColor(PlayerMaterial.VisorColor, VisorColor);
        player.cosmetics.hat.BackLayer.sharedMaterial.SetColor(PlayerMaterial.VisorColor, VisorColor);
        player.cosmetics.skin.layer.sharedMaterial.SetColor(PlayerMaterial.VisorColor, VisorColor);
        player.cosmetics.SetEnabledColorblind(false);

        player.SetName(Name);
    }

    public void ReflectTo(SpriteRenderer renderer)
    {
        Palette.PlayerColors[NebulaPlayerTab.ArchiveColorId] = MainColor;
        Palette.ShadowColors[NebulaPlayerTab.ArchiveColorId] = ShadowColor;

        PlayerMaterial.SetColors(NebulaPlayerTab.ArchiveColorId, renderer);
        renderer.sharedMaterial.SetColor(PlayerMaterial.VisorColor, VisorColor);
    }
}

internal class ArchivedTrackingData
{
    public TrackedTaskPhase[] TaskPhases;
    public TrackedEvent[] Events;
    public ArchivedPlayer[] Players;

    public byte[] Serialize()
    {
        SerializedDataWriter writer = new();
        writer.Write((byte)1);

        void WriteMoment(TrackedMoment m)
        {
            writer.Write(m.Time);
            foreach (var p in m.PlayerData)
            {
                writer.Write(p.Position.x);
                writer.Write(p.Position.y);
                writer.Write((byte)p.States);
            }
        }

        writer.Write(Players.Length);
        foreach (var p in Players)
        {
            writer.Write(p.Name);
            writer.Write(p.MainColor);
            writer.Write(p.ShadowColor);
            writer.Write(p.VisorColor);
            writer.Write(p.HatId);
            writer.Write(p.VisorId);
            writer.Write(p.SkinId);
        }

        writer.Write(TaskPhases.Length);
        foreach(var t in TaskPhases)
        {
            writer.Write(t.Start);
            writer.Write(t.End);
            writer.Write(t.Moments.Count);
            foreach(var m in t.Moments) WriteMoment(m);
        }

        writer.Write(Events.Length);
        foreach(var e in Events)
        {
            WriteMoment(e.Moment);
            writer.Write(e.ImageType);
            writer.Write(e.TranslationKey);
            writer.Write(e.LeftMask);
            writer.Write(e.RightMask);
        }
        return writer.ToData();
    }

    public static ArchivedTrackingData? Deserialize(Stream stream)
    {
        SerializedDataReader reader = new(stream);

        int version = reader.ReadByte();

        if (version == 1)
            return DeserializeV1(reader);

        return null;
    }

    private static TrackedMoment DeserializeMomentV1(SerializedDataReader reader, int players)
    {
        float time = reader.ReadSingle();
        TrackedPlayerMoment[] trackedPlayers = new TrackedPlayerMoment[players];
        for (int p = 0; p < trackedPlayers.Length; p++)
            trackedPlayers[p] = new(new(reader.ReadSingle(), reader.ReadSingle()), (PlayerTrackingFlags)reader.ReadByte());
        return new() { Time = time, PlayerData = trackedPlayers };
    }

    private static ArchivedTrackingData DeserializeV1(SerializedDataReader reader) { 
        ArchivedPlayer[] players = new ArchivedPlayer[reader.ReadInt32()];
        for (int p = 0; p < players.Length; p++)
        {
            players[p] = new(reader.ReadString(),(byte)p,reader.ReadColor32(), reader.ReadColor32(), reader.ReadColor32(), reader.ReadString(), reader.ReadString(), reader.ReadString());
        }

        TrackedTaskPhase[] taskPhases = new TrackedTaskPhase[reader.ReadInt32()];
        for(int t = 0;t< taskPhases.Length; t++)
        {
            float start = reader.ReadSingle();
            float end = reader.ReadSingle();
            TrackedMoment[] moments = new TrackedMoment[reader.ReadInt32()];
            for(int m = 0; m < moments.Length; m++)
                moments[m] = DeserializeMomentV1(reader, players.Length);
            taskPhases[t] = new() { Start = start, End = end, Moments = new(moments), IsClosed = true };
        }

        TrackedEvent[] events = new TrackedEvent[reader.ReadInt32()];
        for (int e = 0; e < events.Length; e++)
        {
            events[e] = new(DeserializeMomentV1(reader, players.Length), reader.ReadString(), reader.ReadString(), reader.ReadInt32(), reader.ReadInt32());
        }

        return new() { Players = players, TaskPhases = taskPhases, Events = events };
    }
}

[NebulaPreprocessForNoS(PreprocessPhaseForNoS.FixStructure)]
internal class TrackedEventImage
{
    public string TextId { get; private init; }
    public int Id { get; private set; } = -1;
    public Virial.Media.Image Image { get; private init; }

    public TrackedEventImage(string id, Virial.Media.Image image)
    {
        this.TextId = id;
        this.Image = image;
        AllImages.Add(this);
        ImagesDic[id] = this;
    }

    static public List<TrackedEventImage> AllImages = new();
    static public Dictionary<string, TrackedEventImage> ImagesDic = new();

    static void Preproces(NebulaPreprocessor preprocessor)
    {
        int num = 0;
        foreach(var i in AllImages.OrderBy(i => i.TextId))
        {
            i.Id = num;
            num++;
        }
        AllImages.Sort((i1, i2) => i2.Id - i1.Id);
    }
}


[NebulaPreprocessForNoS(PreprocessPhaseForNoS.BuildNoSModule)]
internal class GameTracker : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static public GameTracker? Instance { get; private set; } = null;

    static GameTracker()
    {
        DIManager.Instance.RegisterModule(() => new GameTracker());
    }

    public GameTracker()
    {
        Instance = this;
        this.Register(NebulaGameManager.Instance!);
    }

    public List<TrackedTaskPhase> TaskPhases { get; private init; } = new();
    public List<TrackedEvent> Events { get; private init; } = new();

    void AddTaskPhase()
    {
        TaskPhases.Add(new());
        interval = 0f;
    }

    void OnTaskPhaseRestarted(TaskPhaseRestartEvent ev) => AddTaskPhase();
    void OnGameStarted(GameStartEvent ev) => AddTaskPhase();

    void FixTaskPhase() {
        if (TaskPhases.Count > 0) TaskPhases[TaskPhases.Count - 1].IsClosed = true;
    }

    void OnMeetingStart(MeetingPreStartEvent ev) => FixTaskPhase();
    void OnGameEnd(GameEndEvent ev) => FixTaskPhase();

    float interval = 0f;
    void OnUpdate(GameUpdateEvent ev)
    {
        if(TaskPhases.Count > 0 && !TaskPhases[TaskPhases.Count - 1].IsClosed)
        {
            if (interval > 0f)
            {
                interval -= Time.deltaTime;
            }
            else
            {
                TaskPhases[TaskPhases.Count - 1].CaptureCurrent();
            }
        }
    }

    public ArchivedTrackingData Output()
    {
        return new ArchivedTrackingData() { Players = NebulaGameManager.Instance!.AllPlayerInfo().Select(p => ArchivedPlayer.FromPlayer(p)).ToArray(), TaskPhases = TaskPhases.ToArray() };
    }

    static private string GetImageTagById(int id) { }

    static private RemoteProcess<(CommunicableTextTag eventDetail, int imageId, int leftMask, int rightMask)> RpcRecordEvent = new(
            "RecordEvent",
            (message, calledByMe) =>
            {
                Instance?.Events.Add(new(TrackedMoment.CaptureCurrent(), GetImageTagById(message.imageId), message.eventDetail.TranslationKey, message.leftMask, message.rightMask));
            }
        );
}

/// <summary>
/// 旧版のイベント記録
/// </summary>

[NebulaRPCHolder]
public class GameStatistics
{
    public class EventVariation
    {
        static Dictionary<int, EventVariation> AllEvents = new();
        static private DividedSpriteLoader iconSprite = DividedSpriteLoader.FromResource("Nebula.Resources.GameStatisticsIcon.png", 100f, 8, 1);
        static public EventVariation Kill = new(0, iconSprite.AsLoader(0), iconSprite.AsLoader(0), true, true);
        static public EventVariation Exile = new(1, iconSprite.AsLoader(2), iconSprite.AsLoader(2), false, false);
        static public EventVariation GameStart = new(2, iconSprite.AsLoader(1), iconSprite.AsLoader(1), true, false);
        static public EventVariation GameEnd = new(3, iconSprite.AsLoader(1), iconSprite.AsLoader(1), true, false);
        static public EventVariation MeetingEnd = new(4, iconSprite.AsLoader(1), iconSprite.AsLoader(1), true, false);
        static public EventVariation Report = new(5, iconSprite.AsLoader(4), iconSprite.AsLoader(4), true, false);
        static public EventVariation EmergencyButton = new(6, iconSprite.AsLoader(3), iconSprite.AsLoader(3), true, false);
        static public EventVariation Disconnect = new(7, iconSprite.AsLoader(5), iconSprite.AsLoader(5), false, false);
        static public EventVariation Revive = new(8, iconSprite.AsLoader(6), iconSprite.AsLoader(6), true, false);
        static public EventVariation CleanBody = new(9, iconSprite.AsLoader(7), iconSprite.AsLoader(7), true, false);

        public int Id { get; private init; }
        public ISpriteLoader? EventIcon { get; private init; }
        public ISpriteLoader? InteractionIcon { get; private init; }
        public bool ShowPlayerPosition { get; private init; }
        public bool CanCombine { get; private init; }
        public EventVariation(int id, ISpriteLoader? eventIcon, ISpriteLoader? interactionIcon, bool showPlayerPosition, bool canCombine)
        {
            Id = id;
            EventIcon = eventIcon;
            InteractionIcon = interactionIcon;
            CanCombine = canCombine;

            AllEvents.Add(id, this);
            ShowPlayerPosition = showPlayerPosition;
        }
        static public EventVariation ValueOf(int id) => AllEvents[id];

        
    }

    public class Event
    {
        public EventVariation Variation { get; private init; }
        public float Time { get; private init; }
        public byte? SourceId { get; private init; }
        public int TargetIdMask { get; private set; }
        public Tuple<byte, Vector2>[] Position { get; private init; }
        public CommunicableTextTag? RelatedTag { get; set; } = null;


        public Event(EventVariation variation, byte? sourceId, int targetIdMask,GameStatisticsGatherTag? positionTag = null)
            : this(variation, NebulaGameManager.Instance!.CurrentTime, sourceId, targetIdMask,positionTag) { }

        public Event(EventVariation variation, float time, byte? sourceId, int targetIdMask, GameStatisticsGatherTag? positionTag)
        {
            Variation = variation;
            Time = time;
            SourceId = sourceId;
            TargetIdMask = targetIdMask;

            if (variation.ShowPlayerPosition)
            {
                List<Tuple<byte, Vector2>> list = new();
                foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (p.Data.IsDead && p.PlayerId != sourceId && ((TargetIdMask & (1 << p.PlayerId)) == 0)) continue;

                    if (positionTag != null)
                        list.Add(new Tuple<byte, Vector2>(p.PlayerId, NebulaGameManager.Instance!.GameStatistics.Gathering[positionTag.Value][p.PlayerId]));
                    else
                        list.Add(new Tuple<byte, Vector2>(p.PlayerId, p.transform.position));
                }
                Position = list.ToArray();
            }
            else
            {
                Position = new Tuple<byte, Vector2>[0];
            }
        }

        public bool IsSimilar(Event target)
        {
            if (!Variation.CanCombine) return false;
            return Variation == target.Variation && SourceId == target.SourceId && RelatedTag == target.RelatedTag;
        }

        public void Combine(Event target)
        {
            TargetIdMask |= target.TargetIdMask;
        }
    }

    private List<Event> allEvents { get; set; } = new();
    public IEnumerable<Event> AllEvents => allEvents;
    public Event[] Sealed { get => allEvents.ToArray(); }

    public Dictionary<GameStatisticsGatherTag, Dictionary<byte, Vector2>> Gathering { get; set; } = new();

    public void RecordEvent(Event statisticsEvent)
    {
        int index = allEvents.Count;

        if (statisticsEvent.Variation.CanCombine)
        {
            //末尾から検索
            for (int i = allEvents.Count - 1; i >= 0; i--)
            {
                if (allEvents[i].Time > statisticsEvent.Time) index = i;

                //ある程度以上離れた時間のイベントまで来たら検索をやめる
                if (statisticsEvent.Time - allEvents[i].Time > 5f) break;

                if (allEvents[i].IsSimilar(statisticsEvent))
                {
                    allEvents[i].Combine(statisticsEvent);
                    return;
                }
            }
        }
        allEvents.Insert(index, statisticsEvent);
    }

    public void RpcRecordEvent(EventVariation variation, TranslatableTag relatedTag, PlayerControl? source,params PlayerControl[] targets)
    {
        int mask = 0;
        foreach (var p in targets) mask |= 1 << p.PlayerId;
        RpcRecordEvent(variation,relatedTag,source,mask);
    }

    public void RpcRecordEvent(EventVariation variation, TranslatableTag relatedTag, PlayerControl? source, int targetMask) => RpcRecord.Invoke((variation.Id, relatedTag.Id, source?.PlayerId ?? byte.MaxValue, targetMask, 0f));
    public void RpcRecordEvent(EventVariation variation, TranslatableTag relatedTag, float timeLag, PlayerControl? source, int targetMask) => RpcRecord.Invoke((variation.Id, relatedTag.Id, source?.PlayerId ?? byte.MaxValue, targetMask, timeLag));


    static private RemoteProcess<(int variation,int relatedTag,byte sourceId,int targetMask,float timeLag)> RpcRecord = new(
        "RecordStatistics",
       (message, isCalledByMe) =>
       {
           NebulaGameManager.Instance?.GameStatistics.RecordEvent(new Event(EventVariation.ValueOf(message.variation), NebulaGameManager.Instance.CurrentTime + message.timeLag, message.sourceId == byte.MaxValue ? null : message.sourceId, message.targetMask, null) { RelatedTag = TranslatableTag.ValueOf(message.relatedTag) });
       });

    static public RemoteProcess<(GameStatisticsGatherTag tag,byte playerId, Vector2 pos)> RpcPoolPosition = new(
        "PoolPosition",
        (message, _) =>
        {
            if (NebulaGameManager.Instance == null) return;

            if (!NebulaGameManager.Instance!.GameStatistics.Gathering.ContainsKey(message.tag))
                NebulaGameManager.Instance!.GameStatistics.Gathering.Add(message.tag, new());

            NebulaGameManager.Instance!.GameStatistics.Gathering[message.tag][message.playerId] = message.pos;
        }
        );
}

public class CriticalPoint : MonoBehaviour
{
    static CriticalPoint()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CriticalPoint>();
    }
    static private SpriteLoader momentSprite = SpriteLoader.FromResource("Nebula.Resources.GameStatisticsMoment.png", 100f);
    static private SpriteLoader momentRingSprite = SpriteLoader.FromResource("Nebula.Resources.GameStatisticsMomentRing.png", 100f);

    public int IndexMin { get; private set; }
    public int IndexMax { get; private set; }
    GameObject ring = null!;
    public GameStatisticsViewer MyViewer = null!;

    public void SetIndex(int min,int max)
    {
        IndexMin = min; IndexMax = max;
    }

    public bool Contains(int index) => IndexMin <= index && index <= IndexMax;

    public void Start()
    {
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = momentSprite.GetSprite();
        renderer.color = GameStatisticsViewer.MainColor;
        renderer.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        var ringRenderer = UnityHelper.CreateObject<SpriteRenderer>("Ring", transform, Vector3.zero);
        ringRenderer.sprite = momentRingSprite.GetSprite();
        ringRenderer.color = GameStatisticsViewer.MainColor;
        ringRenderer.gameObject.SetActive(false);
        ring = ringRenderer.gameObject;

        
        var button = renderer.gameObject.SetUpButton(true);
        button.OnMouseOver.AddListener(() =>
        {
            renderer.transform.localScale = new Vector3(1f, 1f, 1f);
            MyViewer.OnMouseOver(IndexMin);
        });
        button.OnMouseOut.AddListener(() =>
        {
            renderer.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            MyViewer.OnMouseOut(IndexMin);
        });
        button.OnClick.AddListener(() =>
        {
            MyViewer.OnSelect(ring.active ? -1 : IndexMin);
        });
        
        var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.09f;
    }

    public void OnSomeIndexSelected(int selected)
    {
        ring.gameObject.SetActive(Contains(selected));
    }
}

public class GameStatisticsViewer : MonoBehaviour
{
    static GameStatisticsViewer()
    {
        ClassInjector.RegisterTypeInIl2Cpp<GameStatisticsViewer>();
    }

    LineRenderer timelineBack = null!, timelineFront = null!;
    GameObject minimap = null!;
    GameObject baseOnMinimap = null!, detailHolder = null!;
    AlphaPulse mapColor = null!;
    GameStatistics.Event[] allStatistics = null!;
    GameStatistics.Event? eventPiled, eventSelected, currentShown;
    GameObject CriticalPoints = null!;

    public float? SelectedTime => eventSelected?.Time;

    public PoolablePlayer PlayerPrefab = null!;
    public TMPro.TextMeshPro GameEndText = null!;
    static public GameStatisticsViewer Instance { get; private set; } = null!;

    public void Start()
    {
        allStatistics = NebulaGameManager.Instance!.GameStatistics.Sealed;
        if (allStatistics.Length == 0) return;

        timelineBack = UnityHelper.SetUpLineRenderer("TimelineBack", transform, new Vector3(0, 0, -10f), LayerExpansion.GetUILayer(), 0.014f);
        timelineFront = UnityHelper.SetUpLineRenderer("TimelineFront", transform, new Vector3(0, 0, -15f), LayerExpansion.GetUILayer(), 0.014f);

        minimap = UnityHelper.CreateObject("Minimap",transform, new Vector3(0, -1.62f, 0));
        var scaledMinimap = UnityHelper.CreateObject("Scaled", minimap.transform, new Vector3(0, 0, 0));
        scaledMinimap.transform.localScale = new Vector3(0.45f, 0.45f, 1);
        var minimapRenderer = GameObject.Instantiate(NebulaGameManager.Instance!.RuntimeAsset.MinimapObjPrefab, scaledMinimap.transform);
        minimapRenderer.gameObject.name = "MapGraphic";
        minimapRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
        minimapRenderer.transform.localPosition = Vector3.zero;
        mapColor = minimapRenderer.GetComponent<AlphaPulse>();
        mapColor.SetColor(MainColor);
        NebulaAsset.CreateSharpBackground(new Vector2(4.6f, 2.8f), MainColor, minimap.transform);
        baseOnMinimap = UnityHelper.CreateObject("Scaler", scaledMinimap.transform,NebulaGameManager.Instance.RuntimeAsset.MinimapPrefab.HerePoint.transform.parent.localPosition);
        detailHolder = UnityHelper.CreateObject("Detail", transform, new Vector3(0, -3.5f, 0));
        Hide();

        CriticalPoints = UnityHelper.CreateObject("CriticalMoments",transform,Vector3.zero);

        StartCoroutine(CoShowTimeLine().WrapToIl2Cpp());
    }

    public void Update()
    {
        GameStatistics.Event? willShown = eventPiled ?? eventSelected;
        if(willShown != currentShown)
        {
            if (willShown == null)
                Hide();
            else
                Show(willShown);
            currentShown = willShown;
        }
    }

    private const float LineHalfWidth = 2.5f;
    public static readonly Color MainColor = new Color(0f, 242f / 255f, 156f / 255f);
    private const float BackColorRate = 0.4f;

    private IEnumerator CoShowCriticalMoment(float p,int indexMin,int indexMax)
    {
        var point = UnityHelper.CreateObject<CriticalPoint>("Moment",CriticalPoints.transform, new Vector3((p * 2f - 1f) * LineHalfWidth, 0f, -20f - indexMin));
        point.MyViewer = this;
        point.SetIndex(indexMin, indexMax);
        yield return null;
    }

    public void OnSelect(int index)
    {
        eventSelected = index >= 0 ? allStatistics[index] : null;
        CriticalPoints.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => obj.GetComponent<CriticalPoint>().OnSomeIndexSelected(index)));
    }

    private void ShowCriticalMoment(float p,ref int index)
    {
        var sum = allStatistics[allStatistics.Length - 1].Time - allStatistics[0].Time;
        int indexMin = index;
        while (index + 1 < allStatistics.Length && allStatistics[index + 1].Time - allStatistics[indexMin].Time < sum*0.01f)
        {
            index++;
        }
        int indexMax = index;
        
        //ゲーム終了と結合する際は後ろに揃える
        if (indexMax == allStatistics.Length - 1) p = 1f;

        StartCoroutine(CoShowCriticalMoment(p, indexMin,indexMax).WrapToIl2Cpp());
        index = indexMax + 1;
    }

    private IEnumerator CoShowTimeLine()
    {
        StartCoroutine(CoShowTimeBackLine().WrapToIl2Cpp());
        yield return new WaitForSeconds(1.4f);

        timelineFront.SetPosition(0, new Vector3(-LineHalfWidth, 0));
        timelineFront.SetPosition(1, new Vector3(-LineHalfWidth, 0));
        timelineFront.SetColors(MainColor, MainColor);

        float p = 0f;

        float minTime = allStatistics[0].Time;
        float maxTime = allStatistics[allStatistics.Length - 1].Time;
        int index = 0;

        ShowCriticalMoment(0, ref index);

        float ToP(float p) => (p - minTime) / (maxTime - minTime);

        while (p < 1f)
        {
            while (index < (allStatistics.Length - 1) && ToP(allStatistics[index].Time) < p) ShowCriticalMoment(ToP(allStatistics[index].Time), ref index);

            timelineFront.SetPosition(1, new Vector3(LineHalfWidth * (p * 2f - 1f), 0));
            p += Time.deltaTime / 3f;
            yield return null;
        }
        while (index < allStatistics.Length) ShowCriticalMoment(ToP(allStatistics[index].Time), ref index);
        timelineFront.SetPosition(1, new Vector3(LineHalfWidth, 0));
    }
    private IEnumerator CoShowTimeBackLine()
    {
        float t = 0f;

        timelineBack.SetPosition(0, new Vector3(-LineHalfWidth, 0));
        timelineBack.SetColors(MainColor * BackColorRate, MainColor.AlphaMultiplied(0));

        while (true)
        {
            float log = Mathf.Log(t + 1f, 1.92f);
            float exp = t > 1.3f ? Mathf.Pow((t - 1.3f) * 0.86f, 3f) : 0f;
            t += Time.deltaTime;

            timelineBack.SetPosition(1, new Vector3(log < 1 ? log * LineHalfWidth : LineHalfWidth, 0));
            float a = exp;
            if (log > 1) a += ((log - 1) / log) * 0.3f * LineHalfWidth;
            timelineBack.endColor = MainColor.AlphaMultiplied(a > 1f ? 1f : a) * BackColorRate;

            if (log > 1f && a > 1f) break;

            yield return null;
        }

        timelineBack.SetPosition(1, new Vector3(LineHalfWidth, 0));
        timelineBack.endColor = MainColor * BackColorRate;
    }

    public void ClearDetail(bool onlyMinimap)
    {
        baseOnMinimap.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => GameObject.Destroy(c)));
        if (!onlyMinimap) detailHolder.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => GameObject.Destroy(c)));
    }

    public void Hide()
    {
        minimap.SetActive(false);
        detailHolder.SetActive(false);
    }
    public void Show(GameStatistics.Event statisticsEvent){
        //対象となるCriticalPointを探す
        int index = 0, indexMin = 0, indexMax = 0;
        while (allStatistics[index] != statisticsEvent) index++;
        CriticalPoints.ForEachChild((Il2CppSystem.Action<GameObject>)((obj) => {
            var criticalPoint = obj.GetComponent<CriticalPoint>();
            if (criticalPoint.Contains(index))
            {
                indexMin = criticalPoint.IndexMin;
                indexMax = criticalPoint.IndexMax;
            }
        }));

        //CriticalPointが一致しない場合は詳細も含めてリセットする
        var lastIndex = Array.IndexOf(allStatistics, currentShown);
        var requireGenerateDetail = !(indexMin <= lastIndex && lastIndex <= indexMax);
        ClearDetail(!requireGenerateDetail);

        minimap.SetActive(true);
        detailHolder.SetActive(true);

        float p = 0f;
        foreach (var pos in statisticsEvent.Position)
        {
            var renderer = GameObject.Instantiate(NebulaGameManager.Instance!.RuntimeAsset.MinimapPrefab.HerePoint, baseOnMinimap.transform);
            PlayerMaterial.SetColors(pos.Item1, renderer);
            renderer.transform.localPosition = (Vector3)(pos.Item2 / NebulaGameManager.Instance!.RuntimeAsset.MapScale) + new Vector3(0, 0, -1f - p);
            var button = renderer.gameObject.SetUpButton();
            button.gameObject.AddComponent<BoxCollider2D>().size = new(0.3f, 0.3f);

            button.OnMouseOver.AddListener(()=> {
                MetaWidgetOld widget = new();

                foreach (var near in statisticsEvent.Position)
                {
                    if (near.Item2.Distance(pos.Item2) > 0.6f) continue;

                    if (widget.Count > 0) widget.Append(new MetaWidgetOld.VerticalMargin(0.1f));
                    var roleText = NebulaGameManager.Instance.RoleHistory.EachMoment(history => history.PlayerId == near.Item1 && !(history.Time > statisticsEvent.Time),
                        (role, ghostRole, modifiers) => RoleHistoryHelper.ConvertToRoleName(role, ghostRole, modifiers, false)).LastOrDefault();
                    widget.Append(new MetaWidgetOld.Text(Nebula.Utilities.TextAttributeOld.BoldAttrLeft) { RawText = NebulaGameManager.Instance.GetPlayer(near.Item1)!.Name });
                    widget.Append(new MetaWidgetOld.VariableText(new Nebula.Utilities.TextAttributeOld(Nebula.Utilities.TextAttributeOld.BoldAttrLeft) { Alignment = TMPro.TextAlignmentOptions.TopLeft }.EditFontSize(1.35f)) { RawText = roleText ?? "" });

                }

                NebulaManager.Instance.SetHelpWidget(button, widget);
            });
            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));

            p += 0.001f;
        }

        
        int num = 0;
        void EventToDetailShower(int eventIndex)
        {
            GameStatistics.Event target = allStatistics[eventIndex];

            GameObject detail = UnityHelper.CreateObject("EventDetail", detailHolder.transform, new Vector3(0, -0.76f * num, -10f));
            
            var backGround = NebulaAsset.CreateSharpBackground(new Vector2(3.4f, 0.7f), MainColor, detail.transform);

            var collider = detail.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3.4f, 0.7f);
            var button = detail.gameObject.SetUpButton(true);
            button.OnClick.RemoveAllListeners();
            button.OnMouseOver.AddListener(() =>
            {
                OnMouseOver(eventIndex);
                backGround.color = Color.Lerp(MainColor,Color.white,0.5f);
            });
            button.OnMouseOut.AddListener(() =>
            {
                OnMouseOver(eventIndex);
                backGround.color = MainColor;
            });

            List<GameObject> objects = new();

            Il2CppArgument<PoolablePlayer> GeneratePlayerView(byte id)
            {
                PoolablePlayer player = GameObject.Instantiate(PlayerPrefab, detail.transform);
                var info = NebulaGameManager.Instance?.GetPlayer(id);
                player.UpdateFromPlayerOutfit(info?.Unbox().DefaultOutfit!, PlayerMaterial.MaskType.None, false, true, null);
                player.ToggleName(true);
                player.SetName(info?.Name!, new Vector3(3.1f, 3.1f, 1f), Color.white, -15f);
                player.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
                player.cosmetics.nameText.transform.parent.localPosition += new Vector3(0f, -1.05f, 0f);
                return player;
            }

            if (target.SourceId.HasValue) objects.Add(GeneratePlayerView(target.SourceId.Value).Value.gameObject);
            
            SpriteRenderer icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", detail.transform, new Vector3(0, 0, -1f));
            icon.sprite = target.Variation.InteractionIcon?.GetSprite()!;
            icon.transform.localScale=new Vector3(0.7f,0.7f,1f);
            if(target.RelatedTag != null)
            {
                var text = GameObject.Instantiate(GameEndText, icon.transform);
                text.text = target.RelatedTag.Text;
                text.color = Color.white;
                text.outlineWidth = 0.1f;
                text.transform.localPosition = new Vector3(0f, -0.18f, -1f);
                text.transform.localScale = new Vector3(0.2f / 0.7f, 0.2f / 0.7f, 1f);
                icon.transform.localPosition += new Vector3(0f, 0.05f, 0f);
            }
            objects.Add(icon.gameObject);

            foreach(var p in NebulaGameManager.Instance!.AllPlayerInfo())
                if((target.TargetIdMask & (1 << p.PlayerId)) != 0)
                    objects.Add(GeneratePlayerView(p.PlayerId).Value.gameObject);

            float width = Mathf.Min(1.2f, (float)(objects.Count - 1) * 0.5f);
            for (int i = 0;i<objects.Count;i++)
            {
                float pos = objects.Count == 1 ? 0 : width * ((float)i / (objects.Count - 1) * 2f - 1f);
                objects[i].transform.localPosition += new Vector3(pos, 0, 0f);
            }

            num++;
        }


        if (requireGenerateDetail)
        {
            for (int i = indexMin; i <= indexMax; i++)
            {
                EventToDetailShower(i);
            }
        }
        
    }

    
    public void OnMouseOver(int index) {
        eventPiled = allStatistics[index];
    }
    public void OnMouseOut(int index) {
        if (eventPiled == allStatistics[index]) eventPiled = null;
    }
    
}