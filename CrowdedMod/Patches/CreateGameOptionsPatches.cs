﻿using AmongUs.GameOptions;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace CrowdedMod.Patches;

internal static class CreateGameOptionsPatches
{
    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.Awake))]
    public static class CreateOptionsPicker_Awake
    {
        public static void Postfix(CreateOptionsPicker __instance)
        {
            if (__instance.mode != SettingsMode.Host) return;

            {
                var firstButtonRenderer = __instance.MaxPlayerButtons[0];
                firstButtonRenderer.GetComponentInChildren<TextMeshPro>().text = "-";
                firstButtonRenderer.enabled = false;

                var firstButtonButton = firstButtonRenderer.GetComponent<PassiveButton>();
                firstButtonButton.OnClick.RemoveAllListeners();
                firstButtonButton.OnClick.AddListener((Action)(() =>
                {
                    for (var i = 1; i < 11; i++)
                    {
                        var playerButton = __instance.MaxPlayerButtons[i];

                        var tmp = playerButton.GetComponentInChildren<TextMeshPro>();
                        var newValue = Mathf.Max(byte.Parse(tmp.text) - 10, byte.Parse(playerButton.name) - 2);
                        tmp.text = newValue.ToString();
                    }

                    __instance.UpdateMaxPlayersButtons(__instance.GetTargetOptions());
                }));
                UnityEngine.Object.Destroy(firstButtonRenderer);

                var lastButtonRenderer = __instance.MaxPlayerButtons[^1];
                lastButtonRenderer.GetComponentInChildren<TextMeshPro>().text = "+";
                lastButtonRenderer.enabled = false;

                var lastButtonButton = lastButtonRenderer.GetComponent<PassiveButton>();
                lastButtonButton.OnClick.RemoveAllListeners();
                lastButtonButton.OnClick.AddListener((Action)(() =>
                {
                    for (var i = 1; i < 11; i++)
                    {
                        var playerButton = __instance.MaxPlayerButtons[i];

                        var tmp = playerButton.GetComponentInChildren<TextMeshPro>();
                        var newValue = Mathf.Min(byte.Parse(tmp.text) + 10,
                            CrowdedModPlugin.MaxPlayers - 14 + byte.Parse(playerButton.name));
                        tmp.text = newValue.ToString();
                    }

                    __instance.UpdateMaxPlayersButtons(__instance.GetTargetOptions());
                }));
                UnityEngine.Object.Destroy(lastButtonRenderer);

                for (var i = 1; i < 11; i++)
                {
                    var playerButton = __instance.MaxPlayerButtons[i].GetComponent<PassiveButton>();
                    var text = playerButton.GetComponentInChildren<TextMeshPro>();

                    playerButton.OnClick.RemoveAllListeners();
                    playerButton.OnClick.AddListener((Action)(() =>
                    {
                        var maxPlayers = byte.Parse(text.text);
                        var maxImp = Mathf.Min(__instance.GetTargetOptions().NumImpostors, maxPlayers / 2);
                        __instance.GetTargetOptions().SetInt(Int32OptionNames.NumImpostors, maxImp);
                        __instance.ImpostorButtons[1].TextMesh.text = maxImp.ToString();
                        __instance.SetMaxPlayersButtons(maxPlayers);
                    }));
                }

                foreach (var button in __instance.MaxPlayerButtons)
                {
                    button.enabled = button.GetComponentInChildren<TextMeshPro>().text == __instance.GetTargetOptions().MaxPlayers.ToString();
                }
            }

            {
                var secondButton = __instance.ImpostorButtons[1];
                secondButton.SpriteRenderer.enabled = false;
                UnityEngine.Object.Destroy(secondButton.transform.FindChild("ConsoleHighlight").gameObject);
                UnityEngine.Object.Destroy(secondButton.PassiveButton);
                UnityEngine.Object.Destroy(secondButton.BoxCollider);

                var secondButtonText = secondButton.TextMesh;
                secondButtonText.text = __instance.GetTargetOptions().NumImpostors.ToString();

                var firstButton = __instance.ImpostorButtons[0];
                firstButton.SpriteRenderer.enabled = false;
                firstButton.TextMesh.text = "-";

                var firstPassiveButton = firstButton.PassiveButton;
                firstPassiveButton.OnClick.RemoveAllListeners();
                firstPassiveButton.OnClick.AddListener((Action)(() =>
                {
                    var newVal = Mathf.Clamp(
                        byte.Parse(secondButtonText.text) - 1,
                        1,
                        __instance.GetTargetOptions().MaxPlayers / 2
                    );
                    __instance.SetImpostorButtons(newVal);
                    secondButtonText.text = newVal.ToString();
                }));

                var thirdButton = __instance.ImpostorButtons[2];
                thirdButton.SpriteRenderer.enabled = false;
                thirdButton.TextMesh.text = "+";

                var thirdPassiveButton = thirdButton.PassiveButton;
                thirdPassiveButton.OnClick.RemoveAllListeners();
                thirdPassiveButton.OnClick.AddListener((Action)(() =>
                {
                    var newVal = Mathf.Clamp(
                        byte.Parse(secondButtonText.text) + 1,
                        1,
                        __instance.GetTargetOptions().MaxPlayers / 2
                    );
                    __instance.SetImpostorButtons(newVal);
                    secondButtonText.text = newVal.ToString();
                }));
            }
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.UpdateMaxPlayersButtons))]
    public static class CreateOptionsPicker_UpdateMaxPlayersButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, [HarmonyArgument(0)] IGameOptions opts)
        {
            if (__instance.CrewArea)
            {
                __instance.CrewArea.SetCrewSize(opts.MaxPlayers, opts.NumImpostors);
            }

            var selectedAsString = opts.MaxPlayers.ToString();
            for (var i = 1; i < __instance.MaxPlayerButtons.Count - 1; i++)
            {
                __instance.MaxPlayerButtons[i].enabled = __instance.MaxPlayerButtons[i].GetComponentInChildren<TextMeshPro>().text == selectedAsString;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.UpdateImpostorsButtons))]
    public static class CreateOptionsPicker_UpdateImpostorsButtons
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.SetImpostorButtons))]
    public static class CreateOptionsPicker_SetImpostorButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, int numImpostors)
        {
            IGameOptions targetOptions = __instance.GetTargetOptions();
            targetOptions.SetInt(Int32OptionNames.NumImpostors, numImpostors);
            __instance.SetTargetOptions(targetOptions);
            __instance.UpdateImpostorsButtons(numImpostors);

            return false;
        }
    }

    [HarmonyPatch(typeof(CreateOptionsPicker), nameof(CreateOptionsPicker.SetMaxPlayersButtons))]
    public static class CreateOptionsPicker_SetMaxPlayersButtons
    {
        public static bool Prefix(CreateOptionsPicker __instance, int maxPlayers)
        {
            if (DestroyableSingleton<FindAGameManager>.InstanceExists)
            {
                return true;
            }

            IGameOptions targetOptions = __instance.GetTargetOptions();
            targetOptions.SetInt(Int32OptionNames.MaxPlayers, maxPlayers);
            __instance.SetTargetOptions(targetOptions);
            __instance.UpdateMaxPlayersButtons(targetOptions);

            return false;
        }
    }
}
