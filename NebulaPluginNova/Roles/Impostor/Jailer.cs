﻿using Nebula.Roles.Abilities;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;

namespace Nebula.Roles.Impostor;

public class Jailer : DefinedRoleTemplate, DefinedRole
{
    static public Jailer MyRole = new Jailer();
    private Jailer() : base("jailer", new(Palette.ImpostorRed), RoleCategory.ImpostorRole, Impostor.MyTeam, [CanMoveWithMapWatchingOption,CanIdentifyDeadBodiesOption,CanIdentifyImpostorsOption, InheritAbilityOnDyingOption]) {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagBeginner);
    }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public BoolConfiguration CanMoveWithMapWatchingOption = NebulaAPI.Configurations.Configuration("role.jailer.showTrackingTargetOnMap", false);
    static public BoolConfiguration CanIdentifyDeadBodiesOption = NebulaAPI.Configurations.Configuration("role.jailer.canIdentifyDeadBodies", false);
    static public BoolConfiguration CanIdentifyImpostorsOption = NebulaAPI.Configurations.Configuration("role.jailer.canIdentifyImpostors", false);
    static public BoolConfiguration InheritAbilityOnDyingOption = NebulaAPI.Configurations.Configuration("role.jailer.inheritAbilityOnDying", false);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }

        AchievementToken<bool>? acTokenCommon = null;
        AchievementToken<int>? acTokenChallenge = null;

        [Local]
        void OnOpenSabotageMap(MapOpenSabotageEvent ev)
        {
            acTokenCommon ??= new("jailer.common1", false, (val, _) => val);
        }

        [OnlyMyPlayer, Local]
        void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (acTokenCommon != null) acTokenCommon.Value = true;

            if (acTokenChallenge != null)
            {
                var pos = PlayerControl.LocalPlayer.GetTruePosition();
                Collider2D? room = null;
                foreach (var entry in ShipStatus.Instance.FastRooms)
                {
                    if (entry.value.roomArea.OverlapPoint(pos))
                    {
                        room = entry.value.roomArea;
                        break;
                    }
                }

                if (room != null && Helpers.AllDeadBodies().Any(d => d.ParentId != ev.Dead.PlayerId && room.OverlapPoint(d.TruePosition)))
                    acTokenChallenge!.Value++;
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                //JailerAbilityを獲得していなければ登録
                if ((GameOperatorManager.Instance?.AllOperators.All(e => e is not JailerAbility) ?? false))
                {
                    new JailerAbility(CanIdentifyImpostorsOption, CanIdentifyDeadBodiesOption, CanMoveWithMapWatchingOption).Register(this);
                }
            }
        }

        [OnlyMyPlayer]
        void InheritAbilityOnDead(PlayerDieEvent ev)
        {
            var localPlayer = Virial.NebulaAPI.CurrentGame?.LocalPlayer;

            if (localPlayer == null) return;

            //継承ジェイラーの対象で、JailerAbilityを獲得していなければ登録
            if (InheritAbilityOnDyingOption && !localPlayer.IsDead && localPlayer.IsImpostor && (GameOperatorManager.Instance?.AllOperators.All(e => e is not JailerAbility) ?? false))
            {
                new JailerAbility(CanIdentifyImpostorsOption, CanIdentifyDeadBodiesOption, CanMoveWithMapWatchingOption).Register(this);
            }

        }
    }
}
