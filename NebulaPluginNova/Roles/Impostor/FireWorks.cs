using Il2CppInterop.Runtime.Injection;
using Nebula.Game.Statistics;
using Nebula.Roles.Abilities;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Minimap;
using Virial.Game;

namespace Nebula.Roles.Impostor;

public class FireWorks : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private FireWorks() : base("fireWorks", Impostor.MyTeam.Color, RoleCategory.ImpostorRole, Impostor.MyTeam, [ExplodeCoolDownOption, PlaceFireWorkCoolDownOption])
    {
        MetaAbility.RegisterCircle(new("role.fireWorks.explodeRatio", () => ExplodeRatioOption, () => null, UnityColor));
    }
    Citation? HasCitation.Citaion => Citations.TownOfHost;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Virial.Game.Player player, int[] arguments) => new Instance(player);

    static private FloatConfiguration PlaceFireWorkCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.fireWork.placeFireWorkCoolDown", (2.5f, 30f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    static private IRelativeCoolDownConfiguration ExplodeCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.fireWork.explodeCoolDown", CoolDownType.Immediate, (2.5f, 60f, 2.5f), 32.5f, (-40f, 40f, 2.5f), 7.5f, (0.125f, 2f, 0.125f), 1.25f);
    static private FloatConfiguration ExplodeRatioOption = NebulaAPI.Configurations.Configuration("options.role.fireWork.explodeRatioCoolDown", (0.125f, 10f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio);

    public static FireWorks MyRole = new FireWorks();

    /// <summary>
    /// 烟花爆炸标记
    /// </summary>
    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class FireWorkMark : NebulaSyncStandardObject, IGameOperator
    {
        public static string MyTag = "FireWorkMark";
        private static SpriteLoader markSprite = SpriteLoader.FromResource("Nebula.Resources.CannonMark.png", 100f);
        public FireWorkMark(Vector2 pos) : base(pos, ZOption.Back, false, markSprite.GetSprite())
        {
        }

        static FireWorkMark()
        {
            RegisterInstantiater(MyTag, (args) => new FireWorkMark(new Vector2(args[0], args[1])));
        }
    }

    private static SpriteLoader mapButtonSprite = SpriteLoader.FromResource("Nebula.Resources.CannonButton.png", 100f);
    private static SpriteLoader mapButtonInnerSprite = SpriteLoader.FromResource("Nebula.Resources.FireWorkButtonInner.png", 100f);
    private static SpriteLoader explodeSprite = SpriteLoader.FromResource("Nebula.Resources.BombEffect.png", ExplodeRatioOption * 100f);
    /// <summary>
    /// 烟花发射按钮的地图覆盖层
    /// </summary>
    public class FireWorkMapLayer : MonoBehaviour
    {
        public Instance MyOwner = null!;
        static FireWorkMapLayer() => ClassInjector.RegisterTypeInIl2Cpp<FireWorkMapLayer>();
        public void AddMark(NebulaSyncStandardObject obj, Action onFired)
        {
            var center = VanillaAsset.GetMapCenter(AmongUsUtil.CurrentMapId);
            var scale = VanillaAsset.GetMapScale(AmongUsUtil.CurrentMapId);
            var localPos = VanillaAsset.ConvertToMinimapPos(obj.Position, center, scale);

            var renderer = UnityHelper.CreateObject<SpriteRenderer>("FireWorkButton", transform, localPos.AsVector3(-0.5f));
            renderer.sprite = mapButtonSprite.GetSprite();
            var inner = UnityHelper.CreateObject<SpriteRenderer>("Inner", renderer.transform, new(0f, 0f, -0.1f));
            inner.sprite = mapButtonInnerSprite.GetSprite();

            var button = renderer.gameObject.SetUpButton(true, renderer);
            var collider = renderer.gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;

            button.OnClick.AddListener(() =>
            {
                MyOwner.Explode(obj.Position,obj);
                onFired.Invoke();
                Destroy(button.gameObject);
                MapBehaviour.Instance.Close();
                GameOperatorManager.Instance?.Run(new FireWorkExplodeLocalEvent());
            });
        }
    }

    /// <summary>
    /// 烟花发射事件
    /// </summary>
    private class FireWorkExplodeLocalEvent : Virial.Events.Event
    {
        public FireWorkExplodeLocalEvent() { }
    }

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(Virial.Game.Player player) : base(player) { }
        private List<NebulaSyncStandardObject> Marks = new();
        private FireWorkMapLayer mapLayer = null!;
        // 删除原击杀
        bool RuntimeRole.HasVanillaKillButton => false;
        static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.MarkButton.png", 115f);
        static private Image mapButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.FireWorkMapButton.png", 115f);

        void RuntimeAssignable.OnActivated() 
        {
            if (AmOwner)
            {
                // 标记按钮
                var killButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                killButton.SetSprite(buttonSprite.GetSprite());
                killButton.Availability = (button) => MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) => {
                    var mark = Bind(NebulaSyncObject.RpcInstantiate(FireWorkMark.MyTag, [
                                PlayerControl.LocalPlayer.transform.localPosition.x,
                        PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f
                            ]).SyncObject) as FireWorkMark;
                    Marks.Add(mark!);
                    if (mapLayer) mapLayer.AddMark(mark!, () => Marks.Remove(mark!));

                    killButton.StartCoolDown();
                };
                killButton.CoolDownTimer = Bind(new Timer(PlaceFireWorkCoolDownOption).SetAsAbilityCoolDown().Start());
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Standard);
                killButton.SetLabel("mark");

                // 地图按钮
                var mapButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                mapButton.SetSprite(mapButtonSprite.GetSprite());
                mapButton.Availability = (button) => Marks.Count > 0;
                mapButton.Visibility = (button) => !MyPlayer.IsDead;
                mapButton.OnClick = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        HudManager.Instance.InitMap();
                        MapBehaviour.Instance.ShowNormalMap();
                        MapBehaviour.Instance.taskOverlay.gameObject.SetActive(false);
                    });
                };
                mapButton.SetLabel("fireWork");
                mapButton.CoolDownTimer = Bind(new Timer(ExplodeCoolDownOption.CoolDown).SetAsKillCoolDown().Start());
                GameOperatorManager.Instance?.Register<FireWorkExplodeLocalEvent>(_ => mapButton.StartCoolDown(), mapButton);
            }
        }

        [Local]
        void OnOpenMap(AbstractMapOpenEvent ev)
        {
            if (!MeetingHud.Instance && ev is MapOpenNormalEvent)
            {
                if (!mapLayer)
                {
                    mapLayer = UnityHelper.CreateObject<FireWorkMapLayer>("FireWorkLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                    mapLayer.MyOwner = this;
                    Marks.Do(m => mapLayer.AddMark(m, () => Marks.Remove(m)));
                    this.Bind(mapLayer.gameObject);
                }
                mapLayer.gameObject.SetActive(true);
            }
            else
            {
                if (mapLayer) mapLayer.gameObject.SetActive(false);
            }
        }

        internal void Explode(Vector2 pos, NebulaSyncStandardObject obj)
        {
            RpcSetFireWorkSprite.Invoke(obj.ObjectId);
            int cnt = 0;
            foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo()!)
            {
                if (!player.IsDead && Vector2.Distance(pos, player.TruePosition) <= ExplodeRatioOption)
                {
                    MyPlayer.MurderPlayer(player, PlayerState.Exploded, EventDetail.Kill, KillParameter.RemoteKill);
                    if (player.PlayerId == MyPlayer.PlayerId)
                    {
                        new StaticAchievementToken("fireWorks.another1");
                        cnt--;
                    }
                    if (++cnt >= 5) new StaticAchievementToken("fireWorks.challenge");
                }
            }
            if(cnt == 0) new StaticAchievementToken("fireWorks.another2");
        }

        public static readonly RemoteProcess<int> RpcSetFireWorkSprite = new(
            "SetFireWorkSprite",
            (message, _) =>
            {
                var mark = NebulaSyncStandardObject.GetObject<FireWorkMark>(message);
                if(mark == null) return;
                mark.Sprite = explodeSprite.GetSprite();
                mark.CanSeeInShadow = true;
            });
    }
}
