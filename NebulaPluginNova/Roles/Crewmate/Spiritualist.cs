using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial;
using Virial.Events.Game;

namespace Nebula.Roles.Crewmate;

public class Spiritualist : DefinedRoleTemplate, DefinedRole
{
    private Spiritualist() : base("spiritualist", new(200, 191, 231), RoleCategory.CrewmateRole, Crewmate.MyTeam, [CharCountOption, LettersEachMeetingOption]) { }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static IntegerConfiguration CharCountOption = NebulaAPI.Configurations.Configuration("options.role.spiritualist.charCount", (1, 100), 5);
    private static IntegerConfiguration LettersEachMeetingOption = NebulaAPI.Configurations.Configuration("options.role.spiritualist.lettersEachMeeting", (1, 36), 5);

    static public Spiritualist MyRole = new Spiritualist();
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        const string letterTab = "qwertyuiopasdfghjklzxcvbnm1234567890";
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }

        GamePlayer? lastReported = null;
        string subTab = string.Empty;

        void OnMeetingEnd(MeetingEndEvent ev) => lastReported = null;

        [Local]
        void OnReportDeadBody(ReportDeadBodyEvent ev)
        {
            //Psychic自身が通報した死体であるとき
            //if (ev.Reporter.AmOwner && ev.Reported != null)
            if (ev.Reported != null)
            {
                lastReported = ev.Reported;
                var tab = letterTab.ToList();
                while (subTab.Length < LettersEachMeetingOption)
                {
                    char c = tab.Random();
                    subTab += c;
                    tab.Remove(c);
                }
                var tmp = subTab.OrderBy(x => x).ToList();
                subTab = tmp.Join();
                RpcNoticeSeleted.Invoke((ev.Reported.PlayerId, subTab));
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
                ev.chatText = ev.chatText.ToLower();
                //ev.chatText = Regex.Replace(ev.chatText, subTab, "", RegexOptions.IgnoreCase);
                ev.chatText = ev.chatText.RemoveAll(letterTab.RemoveAll(subTab.ToCharArray()).ToCharArray());
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

        private static readonly RemoteProcess<(byte, string)> RpcNoticeSeleted = new(
            "NoticeSeleted",
            (message, _) =>
            {
                if (message.Item1 == PlayerControl.LocalPlayer.PlayerId)
                {
                    var Message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
                    new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(4f, 0.72f) }.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(Message);
                    Message.transform.localPosition = new Vector3(0, -1.5f, -5f);
                    Message.color = MyRole.UnityColor;
                    Message.text = Language.Translate("role.spiritualist.seleted");
                    float duration = 5f;
                    bool flag = true;
                    var timer = new GameObjectLifespan(Message.gameObject);
                    GameOperatorManager.Instance?.Register<GameUpdateEvent>(ev =>
                    {
                        duration -= Time.deltaTime;
                        if (flag && duration <= 0f)
                        {
                            Message.text = message.Item2;
                            flag = false;
                        }
                        //if (duration <= 0f) UnityEngine.GameObject.Destroy(Message);
                    }, timer);
                    GameOperatorManager.Instance?.Register<MeetingVoteEndEvent>(ev =>
                    {
                        if(Message != null)
                        {
                            UnityEngine.GameObject.Destroy(Message);
                            Message = null;
                        }
                    }, timer);
                }
            });
    }
}


