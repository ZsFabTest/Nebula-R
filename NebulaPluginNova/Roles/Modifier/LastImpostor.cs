using Nebula.Behaviour;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Modifier;

public class LastImpostor : DefinedModifierTemplate, DefinedAllocatableModifier, HasCitation, RoleFilter
{
    private LastImpostor() : base("lastImpostor", Virial.Color.ImpostorColor, [CanSpawnOption, CanGuessAnyRoleOption])
    {
        ConfigurationHolder?.SetDisplayState(() => CanSpawnOption ? ConfigurationHolderState.Emphasized : ConfigurationHolderState.Inactivated);
    }
    Citation? HasCitation.Citaion => Citations.TheOtherRolesGMH;

    internal static BoolConfiguration CanSpawnOption = NebulaAPI.Configurations.Configuration("options.role.lastImpostor.canSpawn", false);
    internal static BoolConfiguration CanGuessAnyRoleOption = NebulaAPI.Configurations.Configuration("options.role.lastImpostor.canGuessAnyRole", false);

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    string ICodeName.CodeName => "LI";
    bool AssignableFilter<DefinedRole>.Test(DefinedRole role) => role.ModifierFilter?.Test(this) ?? false;
    void AssignableFilter<DefinedRole>.ToggleAndShare(DefinedRole role) => role.ModifierFilter?.ToggleAndShare(this);
    void AssignableFilter<DefinedRole>.SetAndShare(Virial.Assignable.DefinedRole role, bool val) => role.ModifierFilter?.SetAndShare(this, val);
    RoleFilter HasRoleFilter.RoleFilter => this;
    bool ISpawnable.IsSpawnable => CanSpawnOption;
    int HasAssignmentRoutine.AssignPriority => 1;
    void HasAssignmentRoutine.TryAssign(Virial.Assignable.IRoleTable roleTable) { }
    void IAssignToCategorizedRole.GetAssignProperties(RoleCategory category, out int assign100, out int assignRandom, out int assignChance)
    {
        if (category == RoleCategory.ImpostorRole) assign100 = CanSpawnOption ? 1 : 0;
        else assign100 = 0;
        assignRandom = 0;
        assignChance = 0;
    }

    static public LastImpostor MyRole = new LastImpostor();

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        void RuntimeAssignable.OnActivated() { }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " LI".Color(MyRole.UnityColor);
        }


        [Local]
        void OnMeetingStart(MeetingStartEvent ev) => LIGuesserSystem.OnMeetingStart();

        [Local, OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev) => LIGuesserSystem.OnDead();

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if(MyPlayer.IsDead || MyPlayer.Role.Role.Category != RoleCategory.ImpostorRole
                 || PlayerControl.AllPlayerControls.GetFastEnumerator().Count(
                   (p) => !p.Data.IsDead && p.GetModInfo()?.Role.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole) > 1)
                MyPlayer.Unbox().RpcInvokerUnsetModifier(MyRole).InvokeSingle();
        }
    }
}

static file class LIGuesserSystem
{
    static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
    public static MetaScreen LastGuesserWindow = null!;

    static public MetaScreen OpenGuessWindow(Action<DefinedRole> onSelected)
    {
        var window = MetaScreen.GenerateWindow(new(7.4f, 4.2f), HudManager.Instance.transform, new Vector3(0, 0, -50f), true, false);

        MetaWidgetOld widget = new();

        MetaWidgetOld inner = new();
        inner.Append(Roles.AllRoles.Where(r => (r.CanBeGuess || LastImpostor.CanGuessAnyRoleOption) && r.IsSpawnable), r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask }, 4, -1, 0, 0.59f);
        MetaWidgetOld.ScrollView scroller = new(new(6.6f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
        widget.Append(scroller);

        window.SetWidget(widget);

        IEnumerator CoCloseOnResult()
        {
            while (MeetingHud.Instance.state != MeetingHud.VoteStates.Results) yield return null;

            window.CloseScreen();
        }

        window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());


        return window;
    }

    static private SpriteLoader targetSprite = SpriteLoader.FromResource("Nebula.Resources.LITargetIcon.png", 115f);
    static public void OnMeetingStart()
    {
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(targetSprite,
            state => {
                var p = state.MyPlayer;
                LastGuesserWindow = OpenGuessWindow((r) =>
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead) return;
                    if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                    if (p?.Role.Role == r)
                        NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.MeetingKill);
                    else if (Neutral.SchrödingersCat.MyRoles.Any(
                       (sr) => p?.Role.Role is Neutral.SchrödingersCat.Instance
                       or Neutral.SchrödingersCat.InstanceCrewmate
                       or Neutral.SchrödingersCat.InstanceImpostor
                       or Neutral.SchrödingersCat.InstanceJackal)
                       && r == Neutral.SchrödingersCat.MyRole)
                        NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.MeetingKill);
                    else
                        NebulaAPI.CurrentGame?.LocalPlayer.MurderPlayer(NebulaAPI.CurrentGame.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.MeetingKill);

                    if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                    LastGuesserWindow = null!;
                });
            },
            p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && !PlayerControl.LocalPlayer.Data.IsDead
            ));
    }

    static public void OnDead()
    {
        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
        LastGuesserWindow = null!;
    }
}
