﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Nebula.Objects
{
    public class CustomButton
    {
        public static List<CustomButton> buttons = new List<CustomButton>();
        public ActionButton actionButton;
        public Vector3 PositionOffset;
        public float MaxTimer = float.MaxValue;
        public float Timer = 0f;
        private Action? OnSuspended=null;
        private Action OnClick;
        private Action OnMeetingEnds;
        private Func<bool> HasButton;
        private Func<bool> CouldUse;
        private Action OnEffectEnds;
        public bool HasEffect;
        public bool isEffectActive = false;
        public bool showButtonText = false;
        public float EffectDuration;
        public Sprite Sprite;
        private HudManager hudManager;
        private bool mirror;
        private KeyCode? hotkey;
        private string buttonText;
        private ImageNames textType;
        //ボタンの有効化フラグと、一時的な隠しフラグ
        private bool activeFlag,hideFlag;
        public bool FireOnClicked = false;
        //クールダウンの進みをインポスターキルボタンに合わせる
        private bool isImpostorKillButton=false;

        public bool IsValid { get { return activeFlag; } }
        public bool IsShown { get { return activeFlag && !hideFlag; } }

        public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool HasEffect, float EffectDuration, Action OnEffectEnds, bool mirror = false, string buttonText = "", ImageNames labelType= ImageNames.UseButton)
        {
            this.hudManager = hudManager;
            this.OnClick = OnClick;
            this.HasButton = HasButton;
            this.CouldUse = CouldUse;
            this.PositionOffset = PositionOffset;
            this.OnMeetingEnds = OnMeetingEnds;
            this.HasEffect = HasEffect;
            this.EffectDuration = EffectDuration;
            this.OnEffectEnds = OnEffectEnds;
            this.Sprite = Sprite;
            this.mirror = mirror;
            this.hotkey = hotkey;
            this.activeFlag = false;
            this.textType = labelType;

            Timer = 16.2f;
            buttons.Add(this);
            actionButton = UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
            PassiveButton button = actionButton.GetComponent<PassiveButton>();
            
            SetLabel(buttonText);
            
            button.OnClick = new Button.ButtonClickedEvent();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)onClickEvent);


            setActive(true);
        }

        public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite, Vector3 PositionOffset, HudManager hudManager, KeyCode? hotkey, bool mirror = false, string buttonText = "", ImageNames labelType = ImageNames.UseButton)
        : this(OnClick, HasButton, CouldUse, OnMeetingEnds, Sprite, PositionOffset, hudManager, hotkey, false, 0f, () => { }, mirror, buttonText,labelType) { }

        public void SetSuspendAction(Action OnSuspended)
        {
            this.OnSuspended = OnSuspended;
        }
        public void SetLabel(string label)
        {
            buttonText = label != "" ? Language.Language.GetString(label) : "";
            
            this.showButtonText = (actionButton.graphic.sprite == Sprite || buttonText != "");
        }

        public void SetButtonCoolDownOption(bool isImpostorKillButton)
        {
            this.isImpostorKillButton = isImpostorKillButton;
        }

        public CustomButton SetTimer(float timer)
        {
            this.Timer = timer;
            return this;
        }

        public void onClickEvent()
        {
            if (HasButton() && CouldUse())
            {
                if (this.Timer < 0f)
                {
                    actionButton.graphic.color = new Color(1f, 1f, 1f, 0.3f);

                    if (this.HasEffect && !this.isEffectActive)
                    {
                        this.Timer = this.EffectDuration;
                        actionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
                        this.isEffectActive = true;
                    }

                    this.OnClick();
                }
                else if(OnSuspended!=null && this.HasEffect && this.isEffectActive)
                {
                    this.OnSuspended();
                }
            }
        }
        public void Destroy()
        {
            setActive(false);
            UnityEngine.Object.Destroy(actionButton);
            actionButton = null;
        }

        public static void HudUpdate()
        {
            buttons.RemoveAll(item => item.actionButton == null);

            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from HudUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void OnMeetingEnd()
        {
            buttons.RemoveAll(item => item.actionButton == null);
            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].OnMeetingEnds();
                    buttons[i].Update();

                    buttons[i].actionButton.cooldownTimerText.color = Palette.DisabledClear;
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public static void ResetAllCooldowns()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    buttons[i].Timer = buttons[i].MaxTimer;
                    buttons[i].Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
                }
            }
        }

        public void setActive(bool isActive)
        {
            if (actionButton)
            {
                if (isActive && !hideFlag)
                {
                    actionButton.gameObject.SetActive(true);
                    actionButton.graphic.enabled = true;
                }
                else
                {
                    actionButton.gameObject.SetActive(false);
                    actionButton.graphic.enabled = false;
                }
            }
            this.activeFlag = isActive;
        }

        public void temporaryHide(bool hideFlag)
        {
            if (hideFlag)
            {
                actionButton.gameObject.SetActive(false);
                actionButton.graphic.enabled = false;
            }
            else if(activeFlag) 
            {
                actionButton.gameObject.SetActive(true);
                actionButton.graphic.enabled = true;
            }
            this.hideFlag = hideFlag;
        }

        private bool MouseClicked()
        {
            if (!Input.GetMouseButtonDown(0)) return false;

            //中心からの距離を求める
            float x = Input.mousePosition.x - (Screen.width)/2;
            float y = Input.mousePosition.y - (Screen.height)/2;

            return Mathf.Sqrt(x * x + y * y) < 280;
        }

        private void Update()
        {
            if (actionButton.cooldownTimerText.color.a != 1f)
            {
                Color c = actionButton.cooldownTimerText.color;
                actionButton.cooldownTimerText.color = new Color(c.r, c.g, c.b, 1f);
            }

            if (Timer >= 0)
            {
                if (HasEffect && isEffectActive)
                    Timer -= Time.deltaTime;
                else if (Helpers.ProceedTimer(isImpostorKillButton))
                    Timer -= Time.deltaTime;
            }

            if (Timer <= 0 && HasEffect && isEffectActive)
            {
                isEffectActive = false;
                actionButton.cooldownTimerText.color = Palette.EnabledColor;
                Timer = MaxTimer;
                OnEffectEnds();
            }

            if (PlayerControl.LocalPlayer.Data == null || !Helpers.ShowButtons || !HasButton())
            {
                temporaryHide(true);
                return;
            }
            temporaryHide(false);


            if (hideFlag) return;

            actionButton.graphic.sprite = Sprite;
            if (showButtonText && buttonText != "")
            {
                actionButton.OverrideText(buttonText);

                actionButton.buttonLabelText.SetSharedMaterial(HudManager.Instance.UseButton.fastUseSettings.get_Item(textType).FontMaterial);  
            }
            actionButton.buttonLabelText.enabled = showButtonText; // Only show the text if it's a kill button
            if (hudManager.UseButton != null)
            {
                Vector3 pos = hudManager.UseButton.transform.localPosition;
                if (mirror) pos = new Vector3(-pos.x, pos.y, pos.z);
                actionButton.transform.localPosition = pos + PositionOffset;
            }
            if (CouldUse())
            {
                actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.EnabledColor;
                actionButton.graphic.material.SetFloat("_Desat", 0f);
            }
            else
            {
                actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.DisabledClear;
                actionButton.graphic.material.SetFloat("_Desat", 1f);
            }

            actionButton.SetCoolDown(Timer, (HasEffect && isEffectActive) ? EffectDuration : MaxTimer);
            CooldownHelpers.SetCooldownNormalizedUvs(actionButton.graphic);

            // Trigger OnClickEvent if the hotkey is being pressed down
            if ((hotkey.HasValue && Input.GetKeyDown(hotkey.Value)) ||
                (FireOnClicked && MouseClicked())) onClickEvent();
        }
    }
}
