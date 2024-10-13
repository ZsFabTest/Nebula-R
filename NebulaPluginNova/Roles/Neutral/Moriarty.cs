using Nebula.Behaviour;
using Nebula.Compat;
using Nebula.Game.Statistics;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;

namespace Nebula.Roles.Neutral;

public class Moriarty : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Virial.Color MyColor = new(106, 252, 45);
    static public RoleTeam MyTeam = new Team("teams.moriarty", MyColor, TeamRevealType.Teams);
    public Moriarty() : base("moriarty", MyColor, RoleCategory.NeutralRole, MyTeam, [SuicideCoolDownOption]) { }
    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    private static FloatConfiguration SuicideCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.moriarty.suicideCoolDown", (5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);

    public static Moriarty MyRole = new Moriarty();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            moriartyTeamId = (byte)arguments.Get(0, MyPlayer.PlayerId);
            hasRecruited = arguments.Get(1, 0) == 0 ? false : true;
        }
        public byte moriartyTeamId { get; private init; }
        bool hasRecruited;
        int[]? RuntimeAssignable.RoleArguments => [moriartyTeamId, hasRecruited ? 1 : 0];
        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.RecruitButton.png", 115f);
        private static Image suicideButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MoriartySuicideButton.png", 115f);

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => ObjectTrackers.StandardPredicate(p) && !IsSameTeam(p), MyColor.ToUnityColor()));
                var button = Bind(new ModAbilityButton(isLeftSideButton: true).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility));

                button.SetSprite(buttonSprite.GetSprite());
                button.Availability = button => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                button.Visibility = button => !MyPlayer.IsDead && !hasRecruited;
                button.OnClick = button =>
                {
                    var target = myTracker.CurrentTarget;
                    target!.Unbox().RpcInvokerSetRole(Moran.MyRole, [moriartyTeamId]).InvokeSingle();
                    hasRecruited = true;
                    button.ReleaseIt();
                };
                button.CoolDownTimer = Bind(new Timer(15f).Start());
                button.SetLabel("recruit");

                var suicideButton = Bind(new ModAbilityButton().KeyBind(Virial.Compat.VirtualKeyInput.Ability));
                suicideButton.SetSprite(suicideButtonSprite.GetSprite());
                suicideButton.Availability = button => myTracker.CurrentTarget != null && MyPlayer.CanMove;
                suicideButton.Visibility = button => !MyPlayer.IsDead;
                suicideButton.OnClick = button =>
                {
                    var target = myTracker.CurrentTarget!;
                    MyPlayer.MurderPlayer(target, PlayerStates.Dead, EventDetails.Kill, KillParameter.NormalKill);
                    MyPlayer.MurderPlayer(MyPlayer, PlayerStates.Suicide, EventDetails.Kill, KillParameter.WithKillSEWidely);
                    if (target.Role.Role == Crewmate.Sherlock.MyRole)
                    {
                        new StaticAchievementToken("moriarty.challenge");
                        //NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.MoriartyWin, 1 << MyPlayer.PlayerId);
                        NebulaGameManager.Instance?.RpcInvokeForcelyWin(NebulaGameEnd.MoriartyWin, 1 << MyPlayer.PlayerId);
                    }
                    new StaticAchievementToken("moriarty.common");
                    button.ReleaseIt();
                };
                suicideButton.CoolDownTimer = Bind(new Timer(SuicideCoolDownOption).Start());
                suicideButton.SetLabel("suicide");
            }
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.MoriartyWin 
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && IsSameTeam(p)));
        /*
        [OnlyMyPlayer]
        void CheckExtraWin(PlayerCheckExtraWinEvent ev) => ev.SetWin(ev.GameEnd == NebulaGameEnd.MoriartyWin
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && ev.WinnersMask.Test(p) && IsSameTeam(p)));
        */

        public bool IsSameTeam(Virial.Game.Player player)
        {
            return (player.Role is Moran.Instance moran && moran.moriartyTeamId == moriartyTeamId) ||
                (player.Role is Instance moriarty && moriarty.moriartyTeamId == moriartyTeamId);
        }

        [Local]
        void DecorateMoranColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(ev.Player)) ev.Color = MyRole.RoleColor;
        }

        [OnlyMyPlayer]
        void DecorateMoriartyColor(PlayerDecorateNameEvent ev)
        {
            var myInfo = PlayerControl.LocalPlayer.GetModInfo();
            if (myInfo == null) return;

            if (IsSameTeam(myInfo)) ev.Color = MyRole.RoleColor;
        }
    }
}

public class Moran : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public Moran() : base("moran", Moriarty.MyColor, RoleCategory.NeutralRole, Moriarty.MyTeam, [SnipeCoolDownOption, ShotSizeOption, ShotEffectiveRangeOption, StoreRifleOnFireOption, StoreRifleOnUsingUtilityOption, CanSeeRifleInShadowOption, CanKillHidingPlayerOption, AimAssistOption, DelayInAimAssistOption], false, optionHolderPredicate: () => ((DefinedRole)Moriarty.MyRole).IsSpawnable)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Moriarty.MyRole.ConfigurationHolder!]);
        ConfigurationHolder!.Title = ConfigurationHolder.Title.WithComparison("role.moriarty.moran.name");

        MetaAbility.RegisterCircle(new("role.moran.shotRange", () => ShotEffectiveRangeOption, () => null, UnityColor));
        MetaAbility.RegisterCircle(new("role.moran.shotSize", () => ShotSizeOption * 0.25f, () => null, UnityColor));
    }
    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player, arguments);

    static private IRelativeCoolDownConfiguration SnipeCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.moran.snipeCoolDown", CoolDownType.Immediate, (10f, 60f, 2.5f), 20f, (-40f, 40f, 2.5f), -10f, (0.125f, 2f, 0.125f), 1f);
    static private FloatConfiguration ShotSizeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotSize", (0.25f, 4f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration ShotEffectiveRangeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotEffectiveRange", (2.5f, 50f, 2.5f), 25f, FloatConfigurationDecorator.Ratio);
    static private BoolConfiguration StoreRifleOnFireOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnFire", true);
    static private BoolConfiguration StoreRifleOnUsingUtilityOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnUsingUtility", false);
    static private BoolConfiguration CanSeeRifleInShadowOption = NebulaAPI.Configurations.Configuration("options.role.moran.canSeeRifleInShadow", false);
    static private BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.moran.canKillHidingPlayer", false);
    static private BoolConfiguration AimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.aimAssist", false);
    static private FloatConfiguration DelayInAimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.delayInAimAssistActivation", (0f, 20f, 1f), 3f, FloatConfigurationDecorator.Second, () => AimAssistOption);

    public static Moran MyRole = new Moran();
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player, int[] arguments) : base(player)
        {
            moriartyTeamId = (byte)arguments.Get(0, MyPlayer.PlayerId);
        }
        public byte moriartyTeamId { get; private init; }
        int[]? RuntimeAssignable.RoleArguments => [moriartyTeamId];
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MoranSnipeButton.png", 115f);
        static private Image aimAssistSprite = SpriteLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);
        public Sniper.SniperRifle? MyRifle = null;
        private ModAbilityButton? equipButton = null;
        private ModAbilityButton? killButton = null;

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MyRifle != null && StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.VanillaPlayer;
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }


        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.MoriartyWin
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && IsSameTeam(p)));
        /*
        [OnlyMyPlayer]
        void CheckExtraWin(PlayerCheckExtraWinEvent ev) => ev.SetWin(ev.GameEnd == NebulaGameEnd.MoriartyWin
            && NebulaGameManager.Instance!.AllPlayerInfo().Any(p => !p.IsDead && ev.WinnersMask.Test(p) && IsSameTeam(p)));
        */

        public bool IsSameTeam(Virial.Game.Player player)
        {
            return (player.Role is Moran.Instance moran && moran.moriartyTeamId == moriartyTeamId) ||
                (player.Role is Instance moriarty && moriarty.moriartyTeamId == moriartyTeamId);
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                equipButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability, "sniper.equip");
                equipButton.SetSprite(buttonSprite.GetSprite());
                equipButton.Availability = (button) => MyPlayer.CanMove;
                equipButton.Visibility = (button) => !MyPlayer.IsDead;
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));
                };
                equipButton.SetLabel("equip");

                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill, "sniper.kill");
                killButton.Availability = (button) => MyRifle != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) =>
                {
                    var target = MyRifle?.GetTarget(ShotSizeOption, ShotEffectiveRangeOption);
                    if (target != null)
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Sniped, EventDetail.Kill, KillParameter.RemoteKill);
                        if (target.Role.Role == Crewmate.Sherlock.MyRole)
                        {
                            //NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.MoriartyWin, 1 << MyPlayer.PlayerId);
                            NebulaGameManager.Instance?.RpcInvokeForcelyWin(NebulaGameEnd.MoriartyWin, 1 << MyPlayer.PlayerId);
                            new StaticAchievementToken("moran.challenge");
                        }
                        else if (IsSameTeam(target)) new StaticAchievementToken("moran.another");
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.VanillaPlayer, 0);
                    }

                    button.StartCoolDown();

                    if (StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));

                };
                killButton.CoolDownTimer = Bind(new Timer(SnipeCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("snipe");
                killButton.SetCanUseByMouseClick();
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            equipButton?.SetLabel("equip");
        }


        IEnumerator CoShowAimAssist()
        {
            IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                Color targetColor = new Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if (renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", HudManager.Instance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)PlayerControl.LocalPlayer.transform.position).normalized;
                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }

                    float angle = Mathf.Atan2(dir.y, dir.x);
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle * 180f / (float)Math.PI);
                    renderer.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2f, Mathf.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = Color.Lerp(Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while (a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }

                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(DelayInAimAssistOption);

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = Bind(new Sniper.SniperRifle(MyPlayer));

            if (AmOwner && AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.ReleaseIt();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifle",
        (message, _) =>
        {
            var role = NebulaGameManager.Instance?.GetPlayer(message.playerId)?.Role;
            if (role is Instance moran)
            {
                if (message.equip)
                    moran.EquipRifle();
                else
                    moran.UnequipRifle();
            }
        }
        );
    }
}