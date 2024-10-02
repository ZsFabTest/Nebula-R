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
using Virial.Text;

namespace Nebula.Roles.Impostor;

public class SerialKiller : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private SerialKiller() : base("serialKiller", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [killCoolDownOption, suicideTimeOption, timeToGetOption, timeEachKillingGetOption]) { }

    Citation? HasCitation.Citaion => Citations.TheOtherRolesGM;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    private static FloatConfiguration killCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.serialKiller.killCoolDown", (0.125f, 1f, 0.125f), 0.75f, decorator: val => val + ("x" + $" ({string.Format("{0:#.#}", AmongUsUtil.VanillaKillCoolDown * val)}{Language.Translate("options.sec")})".Color(Color.gray)));
    private static FloatConfiguration suicideTimeOption = NebulaAPI.Configurations.Configuration("options.role.serialKiller.suicideTime", (2.5f, 60f, 2.5f), 40f, FloatConfigurationDecorator.Second);
    private static BoolConfiguration timeToGetOption = NebulaAPI.Configurations.Configuration("options.role.serialKiller.timeToGet", true);
    private static FloatConfiguration timeEachKillingGetOption = NebulaAPI.Configurations.Configuration("options.role.serialKiller.timeEachKillingGet", (2.5f, 60f, 2.5f), 27.5f, FloatConfigurationDecorator.Second, () => !timeToGetOption);

    public static SerialKiller MyRole = new SerialKiller();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        private Timer suicideTimer = null!;
        private bool hasKilled = false;
        private int killCount = 0;

        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SerialKillerSuicideButton.png", 100f);

        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner)
            {
                hasKilled = false;
                killCount = 0;
                suicideTimer = Bind(new Timer(suicideTimeOption).Pause());
                var suicideButton = Bind(new ModAbilityButton(true));
                suicideButton.SetSprite(buttonSprite.GetSprite());
                suicideButton.Availability = (button) => true;
                suicideButton.Visibility = (button) => !MyPlayer.IsDead && hasKilled;
                suicideButton.CoolDownTimer = Bind(suicideTimer);
                suicideButton.SetLabel("serialKillerSuicide");
            }
        }

        [Local]
        void SetKillTimer(PlayerSetKillTimerEvent ev)
        {
            //if (!PlayerControl.AllPlayerControls.GetFastEnumerator().Any((p) => !p.Data.IsDead && p.PlayerId != MyPlayer.PlayerId && p.GetModInfo()?.Role.Role.Category == RoleCategory.ImpostorRole))
            ev.SetTime(AmongUsUtil.VanillaKillCoolDown * killCoolDownOption);
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if(hasKilled && !MyPlayer.IsDead && !suicideTimer.IsProgressing && !AmongUsUtil.InMeeting)
            {
                MyPlayer.Suicide(PlayerState.Suicide, null, Virial.Game.KillParameter.NormalKill);
                new StaticAchievementToken("serialKiller.common1");
                suicideTimer.Pause().Reset();
            }
            else if(hasKilled && !MyPlayer.IsDead && suicideTimer.IsProgressing && !AmongUsUtil.InMeeting)
            {
                suicideTimer.Resume();
            }
            else if (MyPlayer.IsDead)
            {
                suicideTimer.Pause().Reset();
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            suicideTimer.Pause();
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (hasKilled) suicideTimer.Start(timeToGetOption ? suicideTimer.Max : Math.Min(suicideTimer.CurrentTime + AmongUsUtil.VanillaKillCoolDown, suicideTimer.Max));
        }

        [Local]
        void OnDie(PlayerDieEvent ev)
        {
            if(ev.Player.MyKiller?.PlayerId == MyPlayer.PlayerId && ev.Player.PlayerState == PlayerStates.Dead)
            {
                suicideTimer.Start(timeToGetOption ? suicideTimer.Max : Math.Min(suicideTimer.CurrentTime + timeEachKillingGetOption, suicideTimer.Max));
                if (!hasKilled)
                {
                    hasKilled = true;
                    suicideTimer.Start();
                }
                if (++killCount >= 3) new StaticAchievementToken("serialKiller.common2");
                if (killCount >= 5) new StaticAchievementToken("serialKiller.challenge");
            }
        }
    }
}

