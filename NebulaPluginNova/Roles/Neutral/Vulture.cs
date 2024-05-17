﻿using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;

namespace Nebula.Roles.Neutral;

public class Vulture : DefinedRoleTemplate, HasCitation, DefinedRole
{
    static public Team MyTeam = new("teams.vulture", new(140, 70, 18), TeamRevealType.OnlyMe);
    static public Vulture MyRole = new Vulture();
    
    private Vulture() : base("vulture", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [EatCoolDownOption, NumOfEatenToWinOption, VentConfiguration]) { }
    Citation? HasCitation.Citaion => Citations.TheOtherRoles;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments);

    static private IRelativeCoolDownConfiguration EatCoolDownOption = NebulaAPI.Configurations.KillConfiguration("role.vulture.eatCoolDown", CoolDownType.Immediate, (5f, 60f, 5f), 20f, (-40f, 20f, 5f), -10f, (0.125f, 2f, 0.125f), 0.75f);
    static private IntegerConfiguration NumOfEatenToWinOption = NebulaAPI.Configurations.Configuration("role.vulture.numOfTheEatenToWin", (1,8),3);
    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("role.vulture.vent", true);


    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        private ModAbilityButton? eatButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.EatButton.png", 115f);


        private GameTimer ventCoolDown = (new Timer(VentConfiguration.CoolDown).SetAsAbilityCoolDown().Start() as GameTimer).ResetsAtTaskPhase();
        private GameTimer ventDuration = new Timer(VentConfiguration.Duration);
        private bool canUseVent = VentConfiguration.CanUseVent;
        GameTimer? RuntimeRole.VentCoolDown => ventCoolDown;
        GameTimer? RuntimeRole.VentDuration => ventDuration;
        bool RuntimeRole.CanUseVent => canUseVent;
        int leftEaten = NumOfEatenToWinOption;

        AchievementToken<bool>? acTokenChallenge;

        public Instance(GamePlayer player, int[] arguments) : base(player)
        {
            if (arguments.Length >= 1) leftEaten = arguments[0];
        }
        int[]? RuntimeAssignable.RoleArguments => new int[] { leftEaten };

        private List<(DeadBody deadBody, Arrow arrow)> AllArrows = new();

        [Local]
        void OnDeadBodyGenerated(DeadBodyInstantiateEvent ev)
        {
            AllArrows.Add((ev.DeadBody, Bind(new Arrow(null) { TargetPos = ev.DeadBody.TruePosition }.SetColor(Color.blue))));
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            AllArrows.RemoveAll((tuple) =>
            {
                if (tuple.deadBody)
                {
                    tuple.arrow.TargetPos = tuple.deadBody.TruePosition;
                    return false;
                }
                else
                {
                    tuple.arrow.ReleaseIt();
                    return true;
                }
            });
        }

        void OnReported(ReportDeadBodyEvent ev)
        {
            if (acTokenChallenge != null) acTokenChallenge.Value = false;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                acTokenChallenge = new("vulture.challenge", true, (val, _) =>  val && NebulaGameManager.Instance!.EndState!.EndCondition == NebulaGameEnd.VultureWin && NebulaGameManager.Instance!.EndState!.Winners.Test(MyPlayer) );

                StaticAchievementToken? acTokenCommon = null;

                var eatTracker = Bind(ObjectTrackers.ForDeadBody(null, MyPlayer, (d) => true));

                eatButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                var usesIcon = eatButton.ShowUsesIcon(2);
                eatButton.SetSprite(buttonSprite.GetSprite());
                eatButton.Availability = (button) => eatTracker.CurrentTarget != null && MyPlayer.CanMove;
                eatButton.Visibility = (button) => !MyPlayer.IsDead;
                eatButton.OnClick = (button) => {
                    AmongUsUtil.RpcCleanDeadBody(eatTracker.CurrentTarget!.PlayerId, MyPlayer.PlayerId,EventDetail.Eat);
                    leftEaten--;
                    usesIcon.text=leftEaten.ToString();
                    eatButton.StartCoolDown();

                    acTokenCommon ??= new("vulture.common1");

                    if (leftEaten <= 0) NebulaGameManager.Instance?.RpcInvokeSpecialWin(NebulaGameEnd.VultureWin, 1 << MyPlayer.PlayerId);
                };
                eatButton.CoolDownTimer = Bind(new Timer(EatCoolDownOption.CoolDown).SetAsAbilityCoolDown().Start());
                eatButton.SetLabel("eat");
                usesIcon.text= leftEaten.ToString();
            }
        }

        bool RuntimeRole.HasImpostorVision => true;
    }
}
