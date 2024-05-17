﻿using Nebula.Roles.Modifier;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Helpers;

namespace Nebula.Roles.Impostor;

public class BountyHunter : DefinedRoleTemplate, HasCitation, DefinedRole
{
    static public BountyHunter MyRole = new BountyHunter();
    private BountyHunter() : base("bountyHunter", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [BountyKillCoolDownOption, OthersKillCoolDownOption, ChangeBountyIntervalOption, ShowBountyArrowOption, ArrowUpdateIntervalOption]) { }
    Citation? HasCitation.Citaion => Citations.TheOtherRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private IRelativeCoolDownConfiguration BountyKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("bountyKillCoolDown", CoolDownType.Ratio, (5f, 60f, 2.5f), 10f, (-30f, 30f, 2.5f), -10f, (0.125f, 2f, 0.125f), 0.5f);
    static private IRelativeCoolDownConfiguration OthersKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("othersKillCoolDown", CoolDownType.Ratio, (5f, 60f, 2.5f), 40f, (-30f, 30f, 2.5f), 20f, (0.125f, 2f, 0.125f), 2f);
    static private BoolConfiguration ShowBountyArrowOption = NebulaAPI.Configurations.Configuration("role.bountyHunter.showBountyArrow", true);
    static private FloatConfiguration ArrowUpdateIntervalOption = NebulaAPI.Configurations.Configuration("role.bountyHunter.arrowUpdateInterval", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => ShowBountyArrowOption);
    static private FloatConfiguration ChangeBountyIntervalOption = NebulaAPI.Configurations.Configuration("role.bountyHunter.changeBountyInterval", (5f, 120f, 5f), 45f, FloatConfigurationDecorator.Second);
    float MaxKillCoolDown => Mathf.Max(BountyKillCoolDownOption.CoolDown, OthersKillCoolDownOption.CoolDown, AmongUsUtil.VanillaKillCoolDown);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        private ModAbilityButton? killButton = null;
        bool RuntimeRole.HasVanillaKillButton => false;

        private AchievementToken<bool>? acTokenKillBounty;
        private AchievementToken<bool>? acTokenKillNonBounty;
        private AchievementToken<(bool cleared,Queue<float> history)>? acTokenChallenge;

        public Instance(GamePlayer player) : base(player)
        {
        }

        private byte currentBounty = 0;

        PoolablePlayer bountyIcon = null!;
        Timer bountyTimer = null!;
        Timer arrowTimer = null!;
        Arrow bountyArrow = null!;
        bool CanBeBounty(PlayerControl target, GamePlayer? myLover) => !target.Data.Role.IsImpostor && myLover != target.GetModInfo();
        void ChangeBounty()
        {
            GamePlayer? myLover = null;
            if (MyPlayer.Unbox().TryGetModifier<Lover.Instance>(out var lover)) myLover = lover.MyLover;

            var arr = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => !p.AmOwner && !p.Data.IsDead && CanBeBounty(p, myLover)).ToArray();
            if (arr.Length == 0) currentBounty = byte.MaxValue;
            else currentBounty = arr[System.Random.Shared.Next(arr.Length)].PlayerId;

            if (currentBounty == byte.MaxValue)
                bountyIcon.gameObject.SetActive(false);
            else
            {
                bountyIcon.gameObject.SetActive(true);
                bountyIcon.UpdateFromPlayerOutfit(NebulaGameManager.Instance!.GetPlayer(currentBounty)!.Unbox().DefaultOutfit, PlayerMaterial.MaskType.None, false, true);
            }

            if (ShowBountyArrowOption) UpdateArrow();

            bountyTimer.Start();
        }

        void UpdateArrow()
        {
            var target = NebulaGameManager.Instance?.GetPlayer(currentBounty);
            if (target==null)
            {
                bountyArrow.IsActive= false;
            }
            else
            {
                bountyArrow.IsActive= true;
                bountyArrow.TargetPos = target.VanillaPlayer.transform.localPosition;
            }

            arrowTimer.Start();
        }

        void UpdateTimer()
        {
            if (!bountyTimer.IsInProcess)
            {
                ChangeBounty();
            }
            bountyIcon.SetName(Mathf.CeilToInt(bountyTimer.CurrentTime).ToString());

            if (ShowBountyArrowOption && !arrowTimer.IsInProcess)
            {
                UpdateArrow();
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                acTokenKillBounty = new("bountyHunter.common1", false, (val, _) => val);
                acTokenKillNonBounty = new("bountyHunter.another1", false, (val, _) => val);
                acTokenChallenge = new("bountyHunter.challenge", (false, new()), (val, _) => val.cleared);

                bountyTimer = Bind(new Timer(ChangeBountyIntervalOption)).Start();
                arrowTimer = Bind(new Timer(ArrowUpdateIntervalOption)).Start();

                var killTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, (p) => !p.IsImpostor && !p.IsDead, null, Impostor.MyRole.CanKillHidingPlayerOption));

                killButton = Bind(new ModAbilityButton(false,true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) => {
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget!,PlayerState.Dead, EventDetail.Kill);

                    if(killTracker.CurrentTarget!.PlayerId == currentBounty)
                    {
                        ChangeBounty();
                        button.CoolDownTimer!.Start(BountyKillCoolDownOption.CoolDown);
                        acTokenKillBounty.Value = true;
                    }
                    else
                    {
                        button.CoolDownTimer!.Start(OthersKillCoolDownOption.CoolDown);
                        acTokenKillNonBounty.Value = true;
                    }

                    acTokenChallenge.Value.history.Enqueue(Time.time);
                    if(acTokenChallenge.Value.history.Count >= 3)
                    {
                        float first = acTokenChallenge.Value.history.DequeAt(3);
                        float last = Time.time;
                        if (last - first < 30f) acTokenChallenge.Value.cleared = true;
                    }

                };
                killButton.CoolDownTimer = Bind(new AdvancedTimer(AmongUsUtil.VanillaKillCoolDown, MyRole.MaxKillCoolDown).SetDefault(AmongUsUtil.VanillaKillCoolDown).SetAsKillCoolDown().Start(10f));
                killButton.SetLabelType(Virial.Components.AbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");

                var iconHolder = HudContent.InstantiateContent("BountyHolder",true);
                this.Bind(iconHolder.gameObject);
                bountyIcon = AmongUsUtil.GetPlayerIcon(MyPlayer.Unbox().DefaultOutfit, iconHolder.transform, Vector3.zero, Vector3.one * 0.5f);
                bountyIcon.ToggleName(true);
                bountyIcon.SetName("", Vector3.one * 4f, Color.white, -1f);

                bountyArrow = Bind(new Arrow().SetColor(Palette.ImpostorRed));

                ChangeBounty();
            }
        }

        [Local]
        void LocalUpdate() => UpdateTimer();
        

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            //死亡しているプレイヤーであれば切り替える
            if (NebulaGameManager.Instance?.GetPlayer(currentBounty)?.IsDead ?? true) ChangeBounty();
        }
    }
}
