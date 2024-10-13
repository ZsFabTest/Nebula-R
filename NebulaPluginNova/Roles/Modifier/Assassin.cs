/*
using Nebula.Behaviour;
using Nebula.Modules.GUIWidget;
using Virial;
using Virial.Assignable;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using static Nebula.Behaviour.MeetingPlayerButtonManager;

namespace Nebula.Roles.Modifier;

[NebulaRPCHolder]
static internal class AssassinSystem
{
    public static bool isAssassinMeeting { get; internal set; } = false;
    public static byte targetId { get; internal set; } = byte.MaxValue;
    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.TargetIcon.png", 115f);
    private static List<PassiveButton> buttons = new();
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    public static class EndVotingPatch
    {
        public static bool Prefix()
        {
            return isAssassinMeeting;
        }
    }
    public static void OnMeetingStart()
    {
        if (!isAssassinMeeting) return;
        MeetingHudExtension.CanSkip = false;
        
        buttons.Clear();
        foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
        {
            var player = NebulaGameManager.Instance?.GetPlayer(playerVoteArea.TargetPlayerId);
            if (player == null) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);

            targetBox.name = "MeetingModButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);

            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = null;
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();

            button.OnClick.AddListener(() => {
                OnSeleted(player);
            });
            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
            buttons.Add(button);
        }
    }

    public static void OnSeleted(Virial.Game.Player player)
    {
        if (player == null) return;
        foreach (var button in buttons) UnityEngine.GameObject.Destroy(button);
        RpcForcelyEndAssassinMeeting.Invoke((true, player.PlayerId));
        int result = 0;
        NebulaGameManager.Instance?.AllPlayerInfo().Do((p) =>
        {
            if (p.Role.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole)
                result |= 1 << p.PlayerId;
        });
        if(player.PlayerId != 0) // Need to change
            NebulaGameManager.Instance?.RpcInvokeForcelyWin(NebulaGameEnd.ImpostorWin, result);
    }

    public static void OnMeetingEnd() => RpcForcelyEndAssassinMeeting.Invoke((false, byte.MaxValue));

    private static readonly RemoteProcess<(bool, byte)> RpcForcelyEndAssassinMeeting = new(
        "ForcelyEndAssassinMeeting",
        (message, _) =>
        {
            MeetingHudExtension.VotingTimer = 0f;
            MeetingHudExtension.ResultTimer = 0f;
            if (isAssassinMeeting) PlayerControl.LocalPlayer.GetModInfo()!.Unbox().RpcInvokerUnsetModifier(Assassin.MyRole).InvokeSingle();
            isAssassinMeeting = message.Item1;
            targetId = message.Item2;
        });
}

public class Assassin : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, HasCitation
{
    private Assassin() : base("assassin", "ASI", NebulaTeams.ImpostorTeam.Color, [], false, allocateToNeutral: false) { }
    Citation? HasCitation.Citaion => Citations.ExtremeRoles;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    public static Assassin MyRole = new Assassin();
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole; 
        public Instance(Virial.Game.Player player, int[] arguments) : base(player) 
        {
            hasStartedAssassinMeeting = arguments.Get(0, 0) == 0 ? false : true;
        }
        private bool hasStartedAssassinMeeting = false;
        int[]? RuntimeAssignable.RoleArguments => [hasStartedAssassinMeeting ? 1 : 0];

        void RuntimeAssignable.OnActivated()
        {
            AssassinSystem.isAssassinMeeting = false;
            AssassinSystem.targetId = byte.MaxValue;
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => AssassinSystem.OnMeetingStart();
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev) => AssassinSystem.OnMeetingEnd();
        [Local]
        void OnDied(PlayerDieEvent ev)
        {
            //Debug.LogError("Assassin Died");
            if (ev.Player.PlayerId == MyPlayer.PlayerId)
            {
                //Debug.LogError("Test");
                RpcSetAssassinMeeting.Invoke(0);
                IEnumerator CoReport()
                {
                    MyPlayer.VanillaPlayer.Revive();
                    yield return new WaitForSeconds(0.03f);
                    MyPlayer.VanillaPlayer.ReportDeadBody(MyPlayer.VanillaPlayer.Data);
                    MyPlayer.VanillaPlayer.Die(DeathReason.Kill, false);
                }
                NebulaManager.Instance.StartCoroutine(CoReport().WrapToIl2Cpp());
            }
        }

        private static readonly RemoteProcess<byte> RpcSetAssassinMeeting = new(
            "SetAssassinMeeting",
            (message, _) =>
            {
                AssassinSystem.isAssassinMeeting = true;
                //Debug.LogError(AssassinSystem.isAssassinMeeting);
            });
    }
}

*/