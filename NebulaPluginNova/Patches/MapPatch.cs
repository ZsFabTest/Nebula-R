﻿using Nebula.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
public static class OpenMapCountOverlayPatch
{

    static void Prefix(MapCountOverlay __instance)
    {
        __instance.InitializeModOption();

        var timer = NebulaGameManager.Instance?.ConsoleRestriction?.ShowTimerIfNecessary(ConsoleRestriction.ConsoleType.Admin, __instance.transform, new Vector3(4.8f, 2f, -50f));
        if (timer != null) timer.transform.localScale = Vector3.one / __instance.transform.localScale.x;
    }
}


[HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
public static class CountOverlayUpdatePatch
{
    static TMPro.TextMeshPro notAvailableText = null!;

    static bool Prefix(MapCountOverlay __instance)
    {
        if (!ConsoleTimer.IsOpenedByAvailableWay())
        {
            __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
            if (!notAvailableText) {
                notAvailableText = GameObject.Instantiate(__instance.SabotageText, __instance.SabotageText.transform.parent);
                notAvailableText.text = Language.Translate("console.notAvailable");
                notAvailableText.GetComponent<AlphaBlink>().enabled = true;
            }
            notAvailableText.gameObject.SetActive(true);
            __instance.SabotageText.gameObject.SetActive(false);
            foreach (var counterArea in __instance.CountAreas) counterArea.UpdateCount(0);

            return false;
        }

        if(notAvailableText) notAvailableText.gameObject.SetActive(false);

        __instance.timer += Time.deltaTime;
        if (__instance.timer < 0.1f) return false;
        
        __instance.timer = 0f;
        
        if (!__instance.isSab && MapBehaviourExtension.AffectedByCommSab && PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
        {
            __instance.isSab = true;
            __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
            __instance.SabotageText.gameObject.SetActive(true);
        }
        if (__instance.isSab && (!MapBehaviourExtension.AffectedByCommSab || !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer)))
        {
            __instance.isSab = false;
            __instance.BackgroundColor.SetColor(MapBehaviourExtension.MapColor ?? Color.green);
            __instance.SabotageText.gameObject.SetActive(false);
        }

        if (__instance.isSab)
        {
            foreach (var counterArea in __instance.CountAreas) counterArea.UpdateCount(0);
            return false;
        }

        HashSet<int> hashSet = new HashSet<int>();
        foreach (var counterArea in __instance.CountAreas)
        {
            if (ShipStatus.Instance.FastRooms.TryGetValue(counterArea.RoomType, out var plainShipRoom) && plainShipRoom.roomArea)
            {
                int counter = 0;
                int deadBodies = 0, impostors = 0;

                if (MeetingHud.Instance)
                {
                    //会議中のアドミン (PreMeetingPointを参照する)
                    foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
                    {
                        if (!p.IsDead && plainShipRoom.roomArea.OverlapPoint(p.PreMeetingPoint) && hashSet.Add(p.PlayerId)) {
                            counter++;
                            if (p.Role.Role.Category == Virial.Assignable.RoleCategory.ImpostorRole && MapBehaviourExtension.CanIdentifyImpostors) impostors++;
                        }
                    }
                }
                else
                {
                    //タスクターン中のアドミン
                    int num = plainShipRoom.roomArea.OverlapCollider(__instance.filter, __instance.buffer);
                    for (int j = 0; j < num; j++)
                    {
                        Collider2D collider2D = __instance.buffer[j];
                        if (collider2D.CompareTag("DeadBody") && __instance.includeDeadBodies)
                        {
                            DeadBody component = collider2D.GetComponent<DeadBody>();
                            if (component != null && hashSet.Add((int)component.ParentId))
                            {
                                counter++;
                                if (MapBehaviourExtension.CanIdentifyDeadBodies) deadBodies++;
                            }
                        }
                        else if (!collider2D.isTrigger)
                        {
                            PlayerControl component2 = collider2D.GetComponent<PlayerControl>();
                            if (component2 && component2.Data != null && !component2.Data.Disconnected && !component2.Data.IsDead && (__instance.showLivePlayerPosition || !component2.AmOwner) && hashSet.Add((int)component2.PlayerId))
                            {
                                counter++;
                                if (component2.Data.Role.IsImpostor && MapBehaviourExtension.CanIdentifyImpostors) impostors++;
                            }
                        }
                    }
                }

                counterArea.UpdateCount(counter, impostors, deadBodies);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
public static class OpenNormalMapPatch
{
    //CanMoveのチェックを排除
    static bool Prefix(MapBehaviour __instance)
    {
        if (__instance.IsOpen)
        {
            __instance.Close();
            return false;
        }
        
        PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
        __instance.GenericShow();
        __instance.taskOverlay.Show();
        __instance.ColorControl.SetColor(new Color(0.05f, 0.2f, 1f, 1f));
        DestroyableSingleton<HudManager>.Instance.SetHudActive(false);

        GameEntityManager.Instance?.AllEntities.Do(e=>e.OnOpenNormalMap());
        return false;
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
public static class OpenSabotageMapPatch
{
    //CanMoveのチェックを排除
    static bool Prefix(MapBehaviour __instance)
    {
        if (__instance.IsOpen)
        {
            __instance.Close();
            return false;
        }
        if (__instance.specialInputHandler != null) __instance.specialInputHandler.disableVirtualCursor = true;
        
        PlayerControl.LocalPlayer.SetPlayerMaterialColors(__instance.HerePoint);
        __instance.GenericShow();
        __instance.infectedOverlay.gameObject.SetActive(true);
        __instance.ColorControl.SetColor(Palette.ImpostorRed);
        __instance.taskOverlay.Show();
        DestroyableSingleton<HudManager>.Instance.SetHudActive(false);
        ConsoleJoystick.SetMode_Sabotage();

        GameEntityManager.Instance?.AllEntities.Do(e => e.OnOpenSabotageMap());
        return false;
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
public static class OpenAdminMapPatch
{
    //CanMoveのチェックは元より入ってない

    static void Postfix(MapCountOverlay __instance)
    {
        GameEntityManager.Instance?.AllEntities.Do(e => e.OnOpenAdminMap());
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Awake))]
public static class InitMapPatch
{
    static void Postfix(MapBehaviour __instance)
    {
        GameEntityManager.Instance?.AllEntities.Do(e => e.OnMapInstantiated());
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow))]
static class MapBehaviourGenericShowPatch
{
    static void Postfix(MapBehaviour __instance)
    {
        __instance.transform.localPosition = new Vector3(0, 0, -30f);
    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
class MapBehaviourShowNormalMapPatch
{
    static bool Prefix(MapBehaviour __instance, [HarmonyArgument(0)] MapOptions opts)
    {
        if (__instance.IsOpen)
        {
            __instance.Close();
            return false;
        }

        if (ExileController.Instance || Minigame.Instance) return false;



        if (!PlayerControl.LocalPlayer.CanMove && !MeetingHud.Instance) {
            //会議中でなく動けないとき

            if (GeneralConfigurations.CanOpenMapWhileUsingUtilityOption) {
                //何れかのユーティリティを使用してなければ開けない
                if(!PlayerControl.LocalPlayer.inVent && !PlayerControl.LocalPlayer.inMovingPlat && !PlayerControl.LocalPlayer.walkingToVent && !PlayerControl.LocalPlayer.onLadder) return false; 
            }
            else
            {
                return false;
            }
            
        }

        switch (opts.Mode)
        {
            case MapOptions.Modes.Normal:
                __instance.ShowNormalMap();
                return false;
            case MapOptions.Modes.CountOverlay:
                __instance.ShowCountOverlay(opts.AllowMovementWhileMapOpen, opts.ShowLivePlayerPosition, opts.IncludeDeadBodies);
                return false;
            case MapOptions.Modes.Sabotage:
                __instance.ShowSabotageMap();
                return false;
        }
        return false;
    }

}
