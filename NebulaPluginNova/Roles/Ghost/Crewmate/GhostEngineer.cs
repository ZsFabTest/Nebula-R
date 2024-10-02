using Hazel;
using Nebula.Map;
using Nebula.Patches;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Game;
using Virial.Helpers;

namespace Nebula.Roles.Ghost.Crewmate;

[NebulaRPCHolder]
public class GhostEngineer : DefinedGhostRoleTemplate, HasCitation, DefinedGhostRole
{
    public GhostEngineer() : base("ghostEngineer", new(63, 72, 204), RoleCategory.CrewmateRole, [NumOfRepairingOption]) { }

    Citation? HasCitation.Citaion => Citations.Nebula_Remake_LongTimeSupport;

    string ICodeName.CodeName => "GEG";

    static private IntegerConfiguration NumOfRepairingOption = NebulaAPI.Configurations.Configuration("options.role.ghostEngineer.numOfRepairing", (1, 5), 2);

    static public GhostEngineer MyRole = new GhostEngineer();
    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player) { }

        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.RepairButton.png", 115f);

        private int leftFix = 0;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftFix = NumOfRepairingOption;
                var repairButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                repairButton.SetSprite(buttonSprite.GetSprite());
                repairButton.Availability = (button) => MyPlayer.CanMove && leftFix > 0 && Utilities.AmongUsUtil.InAnySab;
                repairButton.Visibility = (button) => MyPlayer.IsDead;
                repairButton.OnClick = (button) =>
                {
                    RpcFixSabotage.Invoke(MyPlayer.PlayerId);
                    leftFix--;
                    repairButton.ShowUsesIcon(3).text = $"{leftFix}";
                };
                repairButton.ShowUsesIcon(3).text = $"{ leftFix }";
                repairButton.CoolDownTimer = Bind(new Timer(0f).SetAsAbilityCoolDown().Start());
                repairButton.SetLabel("repair");
            }
        }

        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == NebulaGameEnds.CrewmateGameEnd && leftFix <= 0 && NumOfRepairingOption > 1)
                new StaticAchievementToken("ghostEngineer.challenge");
        }
    }

    private static void RepairSabotage()
    {
        for (int i = 0; i < PlayerControl.LocalPlayer.myTasks.Count; i++)
        {
            var task = PlayerControl.LocalPlayer.myTasks[i];
            if (task.TaskType is TaskTypes.FixLights or TaskTypes.RestoreOxy or TaskTypes.ResetReactor or TaskTypes.ResetSeismic or TaskTypes.FixComms or TaskTypes.StopCharles)
            {
                var sabTask = task.TryCast<SabotageTask>();
                if (sabTask != null)
                {
                    SwitchSystem switchSystem = ShipStatus.Instance?.Systems[SystemTypes.Electrical].Cast<SwitchSystem>() ?? null!;
                    switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
                    ShipStatus.Instance?.RpcUpdateSystem(SystemTypes.Comms, 16);
                    var o2reactor = task.TryCast<NoOxyTask>()?.reactor;
                    if (o2reactor != null) o2reactor.Countdown = 10000f;
                    task.TryCast<ReactorTask>()?.reactor.ClearSabotage();
                    task.TryCast<HeliCharlesTask>()?.sabotage.ClearSabotage();
                    sabTask.GetIl2CppType().GetMethod("FixedUpdate", Il2CppSystem.Reflection.BindingFlags.InvokeMethod | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance)?.Invoke(sabTask, null);
                    break;
                }
            }
        }
    }

    private static readonly RemoteProcess<byte> RpcFixSabotage = new(
        "FixSabotage", (message, _) => RepairSabotage());
}
