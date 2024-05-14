﻿using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;

namespace Nebula.Roles.Crewmate;


public class Comet : DefinedRoleTemplate, DefinedRole
{
    static public Comet MyRole = new Comet();

    public Comet() : base("comet", new(121,175,206), RoleCategory.CrewmateRole, Crewmate.MyTeam, [BlazeCoolDownOption, BlazeDurationOption, BlazeSpeedOption, BlazeVisionOption, BlazeScreenOption])
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagBeginner);
    }
    
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private FloatConfiguration BlazeCoolDownOption = new FloatConfigurationImpl("role.comet.blazeCooldown", ArrayHelper.Selection(5f,60f,2.5f),20f).DecorateAsSecConfiguration();
    static private FloatConfiguration BlazeSpeedOption = new FloatConfigurationImpl("role.comet.blazeSpeed", ArrayHelper.Selection(0.5f, 3f, 0.125f), 1.5f).DecorateAsRatioConfiguration();
    static private FloatConfiguration BlazeDurationOption = new FloatConfigurationImpl("role.comet.blazeDuration", ArrayHelper.Selection(5f, 60f, 2.5f), 15f).DecorateAsSecConfiguration();
    static private FloatConfiguration BlazeVisionOption = new FloatConfigurationImpl("role.comet.blazeVisionRate", ArrayHelper.Selection(1f, 3f, 0.125f), 1.5f).DecorateAsRatioConfiguration();
    static private FloatConfiguration BlazeScreenOption = new FloatConfigurationImpl("role.comet.blazeScreenRate", ArrayHelper.Selection(1f, 2f, 0.125f), 1.125f).DecorateAsRatioConfiguration();

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BoostButton.png", 115f);
        private ModAbilityButton? boostButton = null;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                AchievementToken<bool> acTokenCommon = new((AmongUsUtil.CurrentMapId is 0 or 4) ? "comet.common1" : "comet.common2", false, (val, _) => val);
                AchievementToken<(Vector2 pos, bool cleared)>? acTokenCommon2 = null;

                if(BlazeDurationOption <= 15f && BlazeSpeedOption <= 2.5f)
                    acTokenCommon2 = new("comet.common3", (Vector2.zero, false), (val, _) => val.Item2);

                boostButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                boostButton.SetSprite(buttonSprite.GetSprite());
                boostButton.Availability = (button) => MyPlayer.CanMove;
                boostButton.Visibility = (button) => !MyPlayer.IsDead;
                boostButton.OnClick = (button) => button.ActivateEffect();
                boostButton.OnEffectStart = (button) => {
                    using (RPCRouter.CreateSection("CometBlaze"))
                    {
                        PlayerModInfo.RpcAttrModulator.Invoke(new(MyPlayer.PlayerId, new SpeedModulator(BlazeSpeedOption, Vector2.one, true, BlazeDurationOption, false, 100, "nebula::comet"), true));
                        PlayerModInfo.RpcAttrModulator.Invoke(new(MyPlayer.PlayerId, new AttributeModulator(PlayerAttributes.Invisible, BlazeDurationOption, false, 100, "nebula::comet"), true));
                        if (BlazeVisionOption > 1f) PlayerModInfo.RpcAttrModulator.Invoke(new(MyPlayer.PlayerId, new FloatModulator(PlayerAttributes.Eyesight, BlazeVisionOption, BlazeDurationOption, false, 100, "nebula::comet"), true));
                        if(BlazeScreenOption > 1f) PlayerModInfo.RpcAttrModulator.Invoke(new(MyPlayer.PlayerId, new FloatModulator(PlayerAttributes.ScreenSize, BlazeScreenOption, BlazeDurationOption, false, 100, "nebula::comet"), true));

                    }
                    acTokenCommon.Value = true;
                    if(acTokenCommon2 != null) acTokenCommon2.Value.pos = MyPlayer.VanillaPlayer.GetTruePosition();
                };
                boostButton.OnEffectEnd = (button) =>
                {
                    boostButton.StartCoolDown();

                    //緊急会議招集による移動は除外
                    if(!MeetingHud.Instance && acTokenCommon2 != null)
                        acTokenCommon2.Value.cleared |= MyPlayer.VanillaPlayer.GetTruePosition().Distance(acTokenCommon2.Value.pos) > 45f;
                };
                boostButton.CoolDownTimer = Bind(new Timer(0f, BlazeCoolDownOption).SetAsAbilityCoolDown().Start());
                boostButton.EffectTimer = Bind(new Timer(0f, BlazeDurationOption));
                boostButton.SetLabel("blaze");
            }
        }

        bool RuntimeRole.IgnoreBlackout => true;

        [Local]
        void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if (MyPlayer.HasAttribute(PlayerAttributes.Invisible))
            {
                if (!Helpers.AnyNonTriggersBetween(MyPlayer.VanillaPlayer.GetTruePosition(), ev.Dead.VanillaPlayer.GetTruePosition(), out var vec) &&
                    vec.magnitude < BlazeVisionOption * 0.75f)
                    new StaticAchievementToken("comet.challenge");
            }
        }
    }
}

