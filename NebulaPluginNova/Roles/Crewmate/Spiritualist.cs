using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial;
using System.Text.RegularExpressions;
using Virial.Events.Game;
using Nebula.Compat;

namespace Nebula.Roles.Crewmate;

public class Spiritualist : DefinedRoleTemplate, DefinedRole
{
    private Spiritualist() : base("spiritualist", new(200, 191, 231), RoleCategory.CrewmateRole, Crewmate.MyTeam, [CharCountOption, OnlyLettersOption]) { }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static IntegerConfiguration CharCountOption = NebulaAPI.Configurations.Configuration("options.role.spiritualist.charCount", (1, 100), 5);
    private static BoolConfiguration OnlyLettersOption = NebulaAPI.Configurations.Configuration("options.role.spiritualist.onlyLetters", true);

    static public Spiritualist MyRole = new Spiritualist();
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }

        GamePlayer? lastReported = null;

        void OnMeetingEnd(MeetingEndEvent ev) => lastReported = null;

        [Local]
        void OnReportDeadBody(ReportDeadBodyEvent ev)
        {
            //Psychic自身が通報した死体であるとき
            //if (ev.Reporter.AmOwner && ev.Reported != null)
            if (ev.Reported != null)
            {
                lastReported = ev.Reported;
                RpcNoticeSeleted.Invoke(ev.Reported.PlayerId);
            }
        }

        [Local]
        void CheckAddChat(PlayerAddChatEvent ev)
        {
            //ev.SetExtraShow();
            //Debug.LogError(1);
            if (lastReported != null && ev.source.PlayerId == lastReported.PlayerId)
            {
                //Debug.LogError(1);
                if (OnlyLettersOption) ev.chatText = Regex.Replace(ev.chatText, "[^0-9A-Za-z]", "", RegexOptions.IgnoreCase);
                ev.chatText = ev.chatText.Substring(0, Math.Min(CharCountOption, ev.chatText.Length));
                if (ev.chatText == string.Empty) return;
                ev.SetExtraShow();
                lastReported = null;
            }
        }

        /*
        [Local]
        void CheckCanSeeAllInfo(RequestEvent ev)
        {
            if (ev.requestInfo == "checkCanSeeAllInfo") ev.Report(true);
        }
        */

        private static readonly RemoteProcess<byte> RpcNoticeSeleted = new(
            "NoticeSeleted",
            (message, _) =>
            {
                if (message == PlayerControl.LocalPlayer.PlayerId)
                {
                    var Message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
                    new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(4f, 0.72f) }.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(Message);
                    Message.transform.localPosition = new Vector3(0, -1.5f, -5f);
                    Message.color = MyRole.UnityColor;
                    Message.text = Language.Translate("role.spiritualist.seleted");
                    float duration = 5f;
                    var timer = new FunctionalLifespan(() => duration > 0);
                    GameOperatorManager.Instance?.Register<GameUpdateEvent>(ev =>
                    {
                        duration -= Time.deltaTime;
                        if (duration < 0f) UnityEngine.GameObject.Destroy(Message);
                    }, timer);
                }
            });
    }
}


