using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;

namespace Nebula.Roles.Crewmate;

public class Oracle : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private Oracle() : base("oracle", new(254, 156, 45), RoleCategory.CrewmateRole, Crewmate.MyTeam, [OracleCoolDownOption, NumOfInfoOption]) { }
    Citation? HasCitation.Citaion => Citations.NebulaOnTheShip;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    private static FloatConfiguration OracleCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.oracle.oracleCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    private static IntegerConfiguration NumOfInfoOption = NebulaAPI.Configurations.Configuration("options.role.oracle.numOfInfo", (3, 10), 4);

    public static Oracle MyRole = new Oracle();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) 
        {
            oracleResults = new();
        }
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.OracleButton.png", 100f);
        private Dictionary<byte, string> oracleResults = new();
        private TMPro.TextMeshPro message = null!;
        private float duration = 0f;

        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner)
            {
                message = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
                new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(4f, 0.72f) }.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(message);
                message.transform.localPosition = new Vector3(0, -1.5f, -5f);
                message.color = MyRole.UnityColor;
                this.message.gameObject.SetActive(false);
                duration = 0f;
                Bind(new GameObjectBinding(message.gameObject));

                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p)));
                var oracleButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                oracleButton.SetSprite(buttonSprite.GetSprite());
                oracleButton.Availability = (button) => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                oracleButton.Visibility = (button) => !MyPlayer.IsDead;
                oracleButton.OnClick = (button) =>
                {
                    string info;
                    if(!oracleResults.TryGetValue(myTracker.CurrentTarget!.PlayerId, out info!))
                    {
                        info = GetInfomation(myTracker.CurrentTarget!, NumOfInfoOption).TrimEnd(' ').TrimEnd(',');
                        oracleResults.Add(myTracker.CurrentTarget!.PlayerId, info);
                        new StaticAchievementToken("oracle.common2");
                    }else info = oracleResults[myTracker.CurrentTarget!.PlayerId];

                    string message = Language.Translate("role.oracle.message").Replace("%PLAYER%",myTracker.CurrentTarget.Name).Replace("%DETAIL%",info);
                    this.message.text = message;
                    this.message.gameObject.SetActive(true);
                    Debug.LogWarning($"Message: {message}\nMessage.IsActive: {this.message.gameObject.active}");
                    duration = 5f;

                    new StaticAchievementToken("oracle.common1");
                    button.StartCoolDown();
                };
                oracleButton.CoolDownTimer = Bind(new Timer(OracleCoolDownOption).Start());
                oracleButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                oracleButton.SetLabel("oracle");
            }
        }

        string GetInfomation(Virial.Game.Player player, int infoNum)
        {
            var allAssignable = Roles.AllAssignables().ToList();
            List<DefinedRole> allSpawnable = new();
            allAssignable.RemoveAll((r) => !(r is DefinedRole));
            foreach (var r in allAssignable)
            {
                if (((ISpawnable)r).IsSpawnable)
                {
                    allSpawnable.Add((DefinedRole)r);
                }
            }
            var crewmateRoles = new List<DefinedRole>();
            var impostorRoles = new List<DefinedRole>();
            var neutralRoles = new List<DefinedRole>();
            var allRoles = new List<DefinedRole>();
            foreach(var r in allSpawnable)
            {
                allRoles.Add(r);
                switch (r?.Category)
                {
                    case RoleCategory.CrewmateRole:
                        crewmateRoles.Add(r);
                        break;
                    case RoleCategory.ImpostorRole:
                        impostorRoles.Add(r);
                        break;
                    case RoleCategory.NeutralRole:
                        neutralRoles.Add(r);
                        break;
                    default:
                        Debug.LogError("Oracle: Unknown Role");
                        break;
                }
            }

            List<DefinedRole> results = new();
            results.Add(player.Role.Role);
            switch (player?.Role.Role.Category)
            {
                case RoleCategory.CrewmateRole:
                    results.Add(impostorRoles.Count > 0 ? impostorRoles.Random()! : Impostor.Impostor.MyRole);
                    results.Add(neutralRoles.Count > 0 ? neutralRoles.Random()! : Neutral.ChainShifter.MyRole);
                    break;
                case RoleCategory.ImpostorRole:
                    results.Add(crewmateRoles.Count > 0 ? crewmateRoles.Random()! : Crewmate.MyRole);
                    results.Add(neutralRoles.Count > 0 ? neutralRoles.Random()! : Neutral.ChainShifter.MyRole);
                    break;
                case RoleCategory.NeutralRole:
                    results.Add(crewmateRoles.Count > 0 ? crewmateRoles.Random()! : Crewmate.MyRole);
                    results.Add(impostorRoles.Count > 0 ? impostorRoles.Random()! : Impostor.Impostor.MyRole);
                    break;
                default:
                    Debug.LogError("Oracle: Unknown Player Role");
                    break;
            }
            for(int i = 0; i < infoNum - 3; i++)
            {
                var role = allRoles.Count > infoNum ? allRoles.Random()! : Crewmate.MyRole;
                if (results.Contains(role))
                {
                    i--;
                    continue;
                }
                results.Add(role);
            }

            int cmp(DefinedRole a, DefinedRole b)
            {
                if (a.Id > b.Id) return 1;
                else if (a.Id < b.Id) return -1;
                else return 0;
            }

            results.Sort(cmp);

            string result = "";
            foreach(var r in results)
            {
                result += r.DisplayName + ", ";
            }
            return result;
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            message.gameObject.SetActive(false);
        }

        [Local]
        void LoaclUpdate(GameUpdateEvent ev)
        {
            if(duration > 0)
            {
                duration -= Time.deltaTime;
                if(duration < 0)
                {
                    message.gameObject.SetActive(false);
                    Debug.Log("Message: Close");
                }
            }
        }

        [Local]
        void DecorateSidekickColor(PlayerDecorateNameEvent ev)
        {
            string info = "";
            if (oracleResults.TryGetValue(ev.Player.PlayerId, out info!))
                ev.Name += $"<size=0.9>({info})</size>".Color(Color.gray);
        }

        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (oracleResults.TryGetValue(ev.Player.PlayerId, out _))
                oracleResults.Remove(ev.Player.PlayerId);
        }

        [Local]
        void OnPlayerGuessed(PlayerMurderedEvent ev)
        {
            if(ev.Murderer.PlayerId == MyPlayer.PlayerId && ev.Dead.PlayerState == PlayerState.Guessed && oracleResults.TryGetValue(ev.Player.PlayerId, out _))
                new StaticAchievementToken("oracle.challenge");
            if (oracleResults.TryGetValue(ev.Dead.PlayerId, out _))
                oracleResults.Remove(ev.Dead.PlayerId);
        }
    }
}
