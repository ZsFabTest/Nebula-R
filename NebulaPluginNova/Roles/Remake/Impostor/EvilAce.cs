using Nebula.Game.Statistics;
using Nebula.Modules.GUIWidget;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
namespace Nebula.Roles.Impostor;

public class EvilAce : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private EvilAce() : base("evilAce", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [killCoolDownOption]) { }

    Citation? HasCitation.Citaion => RemakeInit.Citations.NebulaOnTheShipRLTS;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private static FloatConfiguration killCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.evilAce.killCoolDown", (0.125f, 1f, 0.125f), 0.75f, decorator: val => val + ("x" + $" ({string.Format("{0:#.#}", AmongUsUtil.VanillaKillCoolDown * val)}{Language.Translate("options.sec")})".Color(Color.gray)));

    public static EvilAce MyRole = new EvilAce();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        Dictionary<byte, DefinedRole> roleTab = new();

        bool RuntimeRole.HasVanillaKillButton => false;
        Timer killTimer = new Timer(AmongUsUtil.VanillaKillCoolDown).SetAsKillCoolDown();
        bool hasChanged = false;

        void RuntimeAssignable.OnActivated() 
        {
            roleTab = new();
            if (AmOwner)
            {
                PlayerControl.AllPlayerControls.GetFastEnumerator().Do((p) =>
                {
                    if (p.PlayerId != MyPlayer.PlayerId && p.GetModInfo()?.Role.Role.Category == RoleCategory.ImpostorRole)
                        roleTab[p.PlayerId] = p.GetModInfo()!.Role.Role;
                });

                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer)));

                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = button => myTracker.CurrentTarget != null;
                killButton.Visibility = button => !MyPlayer.IsDead;
                killButton.OnClick = button =>
                {
                    MyPlayer.MurderPlayer(myTracker.CurrentTarget!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(killTimer.Start());
                killButton.SetLabel("kill");
                killButton.GetKillButtonLike();
            }
        }

        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if(ev.Murderer.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                roleTab[ev.Dead.PlayerId] = ev.Dead.Role.Role;
            }
        }

        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (ev.Player.MyKiller?.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                roleTab[ev.Player.PlayerId] = ev.Player.Role.Role;
            }
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (!hasChanged && NebulaGameManager.Instance!.AllPlayerInfo.Count(p => p.Role.Role.Category == RoleCategory.ImpostorRole) <= 1)
            {
                hasChanged = true;
                killTimer.SetRange(0, killCoolDownOption * AmongUsUtil.VanillaKillCoolDown);
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            string rawText = string.Empty;
            foreach (var kvpair in roleTab)
                rawText = $"{rawText}<b>{Helpers.GetPlayer(kvpair.Key)?.GetModInfo()?.Unbox().ColoredDefaultName ?? "Unknown Player"}</b>: {kvpair.Value.DisplayColoredName}\n";
            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, GUI.API.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.evilAce.message.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, GUI.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(rawText)))
                , MeetingOverlayHolder.IconsSprite[5], MyRole.RoleColor);
        }
    }
}