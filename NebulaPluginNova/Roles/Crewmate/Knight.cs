using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Crewmate;

public class Knight : DefinedRoleTemplate, DefinedRole
{
    private Knight() : base("knight", new(198, 97, 97), RoleCategory.CrewmateRole, Crewmate.MyTeam, [BlockCoolDownOption, BlockDurationOption]) { }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    private static FloatConfiguration BlockCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.knight.blockCoolDown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    private static FloatConfiguration BlockDurationOption = NebulaAPI.Configurations.Configuration("options.role.knight.blockDuration", (1f, 10f, 0.5f), 5f, FloatConfigurationDecorator.Second);

    public static Knight MyRole = new Knight();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        private static Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BlockButton.png", 115f);
        private AchievementToken<(bool cleared, int count)>? acTokenChallenge;
        private ModAbilityButton blockButton = null!;
        private RemoteIntData? isBlocking = null;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                isBlocking = new RemoteIntData((int)RemoteIntDataId.KnightDataBase + MyPlayer.PlayerId, 0);
                Debug.Log($"Knight RID ID: {isBlocking.rid_id}");
                acTokenChallenge = new("knight.challenge", (false, 0), (val, _) => val.cleared);

                blockButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);

                blockButton.SetSprite(buttonSprite.GetSprite());
                blockButton.Availability = (button) => MyPlayer.CanMove;
                blockButton.Visibility = (button) => !MyPlayer.IsDead;
                blockButton.OnClick = (button) =>
                {
                    button.ActivateEffect();
                };
                blockButton.OnEffectStart = (button) =>
                {
                    acTokenChallenge!.Value.count = 0;
                    isBlocking.Update(1);
                    //Debug.Log(isBlocking.Update(1));
                    //Debug.Log(isBlocking.Get());
                };
                blockButton.OnEffectEnd = (button) =>
                {
                    isBlocking.Update(0);
                    button.StartCoolDown();
                };
                blockButton.CoolDownTimer = Bind(new Timer(BlockCoolDownOption).SetAsAbilityCoolDown().Start());
                blockButton.EffectTimer = Bind(new Timer(BlockDurationOption));
                blockButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                blockButton.SetLabel("block");
            }
        }

        void RuntimeAssignable.OnInactivated()
        {
            isBlocking?.Update(0);
            isBlocking = null;
        }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKillEvent ev)
        {
            //ev.Result = KillResult.Guard;
            //return;
            //Debug.Log($"{ev.Killer.PlayerId} {MyPlayer.PlayerId}");
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
            if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;
            //Debug.Log(result.ToString());

            //Debug.LogWarning(RemoteIntData.Get((int)RemoteIntDataId.KnightDataBase + ev.Player.PlayerId));
            ev.Result = RemoteIntData.Get((int)RemoteIntDataId.KnightDataBase + ev.Player.PlayerId) == 1 ? KillResult.Guard : KillResult.Kill;
        }

        [OnlyMyPlayer]
        void OnGuard(PlayerGuardEvent ev)
        {
            if (AmOwner)
            {
                Debug.Log("OnGuard");
                new StaticAchievementToken("knight.common1");
                new StaticAchievementToken("knight.common2");
                acTokenChallenge!.Value.count++;
                if (acTokenChallenge!.Value.count >= 3)
                {
                    acTokenChallenge!.Value.cleared = true;
                }
            }
        }
    }
}
