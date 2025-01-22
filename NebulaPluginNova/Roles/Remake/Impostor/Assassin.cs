using Nebula.Behaviour;
using Nebula.Game.Statistics;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using static Nebula.Roles.Impostor.Marionette;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Assassin : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private Assassin() : base("assassin", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [AssassinCoolDownOption, LeavesDurationOption])
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagDifficult, ConfigurationTags.TagDifficult);
    }
    Citation? HasCitation.Citaion => RemakeInit.Citations.NebulaOnTheShipRLTS;

    private static IRelativeCoolDownConfiguration AssassinCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.assassin.assassinCoolDown", CoolDownType.Immediate, (2.5f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), 0f, (0.125f, 2f, 0.125f), 1f);
    private static FloatConfiguration LeavesDurationOption = NebulaAPI.Configurations.Configuration("options.role.assassin.leavesDuration", (0f, 20f, 0.5f), 5f, FloatConfigurationDecorator.Second);

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Assassin MyRole = new Assassin();

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class Leaves : NebulaSyncStandardObject
    {
        public static string MyTag = "Leaves";
        private static SpriteLoader sprite = SpriteLoader.FromResource("Nebula.Resources.Remake.Leaves.png", 175f);
        public Leaves(Vector2 pos, bool reverse) : base(pos, ZOption.Just, false, sprite.GetSprite())
        {
            MyRenderer.flipX = reverse;
            MyBehaviour = MyRenderer.gameObject.AddComponent<EmptyBehaviour>();
            MyRenderer.color = Color.green;
        }

        public bool Flipped { get => MyRenderer.flipX; set => MyRenderer.flipX = value; }
        public EmptyBehaviour MyBehaviour = null!;

        static Leaves()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new Leaves(new Vector2(args[0], args[1]), args[2] < 0f));
        }
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        static private Image monitorButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DecoyMonitorButton.png", 115f);
        static private Image seleteButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.TrackButton.png", 115f);
        public Instance(GamePlayer player) : base(player) { }
        private Virial.Game.Player? Target = null;
        bool RuntimeRole.HasVanillaKillButton => false;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var monitorButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.SecondaryAbility);
                monitorButton.SetSprite(monitorButtonSprite.GetSprite());
                monitorButton.Availability = (button) => true;
                monitorButton.Visibility = (button) => !MyPlayer.IsDead && Target != null;
                monitorButton.OnClick = (button) =>
                {
                    AmongUsUtil.ToggleCamTarget(Target!.VanillaPlayer, null);
                };
                monitorButton.SetLabel("monitor");

                var myTracker = Bind(ObjectTrackers.ForPlayer(null, MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer)));

                var seleteButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                seleteButton.SetSprite(seleteButtonSprite.GetSprite());
                seleteButton.Availability = (button) => myTracker.CurrentTarget != null;
                seleteButton.Visibility = (button) => !MyPlayer.IsDead;
                seleteButton.OnClick = (button) =>
                {
                    Target = myTracker.CurrentTarget;
                    button.StartCoolDown();
                };
                seleteButton.CoolDownTimer = Bind(new Timer(1f).SetAsAbilityCoolDown().Start());
                seleteButton.SetLabel("mark");

                var killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = button => Target != null;
                killButton.Visibility = button => !MyPlayer.IsDead;
                killButton.OnClick = button =>
                {
                    MyPlayer.MurderPlayer(Target!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    Vector3 pos = Target!.TruePosition.ToUnityVector();
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        var leaves = (NebulaSyncObject.RpcInstantiate(Leaves.MyTag, [
                            pos.x,
                            pos.y + 0.2f,
                            PlayerControl.LocalPlayer.cosmetics.FlipX ? -1f : 1f
                        ]).SyncObject as Leaves);

                        SetLeavesColor(leaves!.ObjectId, Palette.PlayerColors[MyPlayer.PlayerId]);

                        float timer = LeavesDurationOption;

                        var lifeSpan = new FunctionalLifespan(() =>
                        {
                            return timer > 0f;
                        });

                        GameOperatorManager.Instance?.Register<GameUpdateEvent>(_ =>
                        {
                            timer -= Time.deltaTime;
                            if (timer <= 0f && leaves != null && !leaves.MarkedRelease)
                            {
                                NebulaSyncObject.RpcDestroy(leaves.ObjectId);
                            }
                            else if (leaves != null && !leaves.MarkedRelease)
                            {
                                SetLeavesColor(leaves.ObjectId, (Color.green - Palette.PlayerColors[MyPlayer.PlayerId]) * (1 - timer / LeavesDurationOption) + Palette.PlayerColors[MyPlayer.PlayerId]);
                            }
                        }, lifeSpan);
                    });
                    Target = null;
                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(AssassinCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabel("kill");

                killNum = 0;
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            Target = null;
            AmongUsUtil.SetCamTarget();
        }

        [Local]
        void OnDied(PlayerDieEvent ev)
        {
            if (ev.Player.AmOwner) Target = null;
        }

        int killNum = 0;

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (Target != null && Target.IsDead) Target = null;
        }
    }

    private static readonly RemoteProcess<(int, float, float, float)> RpcSetLeavesColor = new(
        "SetLeavesColor",
        (message, _) =>
        {
            var leaves = NebulaSyncObject.GetObject<Leaves>(message.Item1);
            if (leaves == null) return;
            leaves.MyRenderer.color = new UnityEngine.Color(message.Item2, message.Item3, message.Item4);
        });
    private static void SetLeavesColor(int ObjectId, UnityEngine.Color color) => RpcSetLeavesColor.Invoke((ObjectId, color.r, color.g, color.b));
}

