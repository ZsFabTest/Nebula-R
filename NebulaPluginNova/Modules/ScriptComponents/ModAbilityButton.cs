﻿using AmongUs.GameOptions;
using Epic.OnlineServices.Lobby;
using Nebula.Commands.Variations;
using System.Reflection.Emit;
using UnityEngine;
using Virial.Command;
using Virial.Compat;
using Virial.Components;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;

namespace Nebula.Modules.ScriptComponents;

[NebulaPreLoad]
public class ModAbilityButton : INebulaScriptComponent, Virial.Components.AbilityButton, IGameEntity
{
    public class AbilityButtonStructure
    {
        public bool isLeftSide = false; 
        public bool showAlways = false;
        public bool arrangedAsKillButton = false;
        public int priority = 0;
        public IExecutable? onClick = null;
        public IExecutable? onSubClick = null;
        public TextComponent? label = null;
        public Image? image = null;
    }
    public static void Load()
    {
        EntityCommand.RegisterEntityDefinition("button", EntityCommand.GenerateDefinition(
            new Virial.Command.CommandStructureConverter<AbilityButtonStructure>()
            .Add<bool>("isLeftSide", (structure, val) => structure.isLeftSide = val)
            .Add<bool>("isRightSide", (structure, val) => structure.isLeftSide = !val)
            .Add<bool>("always", (structure, val) => structure.showAlways = val)
            .Add<bool>("asKillButton", (structure, val) => structure.arrangedAsKillButton = val)
            .Add<int>("priority", (structure, val) => structure.priority = Mathf.Clamp(val, 0, 9999))
            .Add<string>("rawLabel", (structure, val) => structure.label = new RawTextComponent(val))
            .Add<string>("localizedLabel", (structure, val) => structure.label = new TranslateTextComponent(val))
            .Add<IExecutable>("action", (structure, val) => structure.onClick = val)
            .Add<IExecutable>("aidAction", (structure, val) => structure.onSubClick = val)
            .Add<string>("icon", (structure, val) => structure.image = NebulaResourceManager.GetResource(val)?.AsImage(115f))
            , () => new AbilityButtonStructure(), val =>
            {
                var button = new ModAbilityButton(val.isLeftSide, val.arrangedAsKillButton, val.priority, val.showAlways);
                if(val.onClick != null) button.OnClick = b => NebulaManager.Instance.StartCoroutine(val.onClick!.CoExecute([]).CoWait().HighSpeedEnumerator().WrapToIl2Cpp());
                if(val.onSubClick != null) button.OnSubAction = b => NebulaManager.Instance.StartCoroutine(val.onSubClick!.CoExecute([]).CoWait().HighSpeedEnumerator().WrapToIl2Cpp());
                button.SetRawLabel(val.label?.GetString() ?? "button");
                if(val.image != null)button.SetSprite(val.image.GetSprite());
                return button;
            }));
    }

    public ActionButton VanillaButton { get; private set; }

    public Timer? CoolDownTimer;
    public Timer? EffectTimer;
    public Timer? CurrentTimer => (EffectActive && (EffectTimer?.IsInProcess ?? false)) ? EffectTimer : CoolDownTimer;
    public bool EffectActive = false;

    public float CoolDownOnGameStart = 10f;

    public Action<ModAbilityButton>? OnEffectStart { get; set; } = null;
    public Action<ModAbilityButton>? OnEffectEnd { get; set; } = null;
    public Action<ModAbilityButton>? OnUpdate { get; set; } = null;
    public Action<ModAbilityButton>? OnClick { get; set; } = null;
    public Action<ModAbilityButton>? OnSubAction { get; set; } = null;
    public Action<ModAbilityButton>? OnMeeting { get; set; } = null;
    public Action<ModAbilityButton>? OnStartTaskPhase { get; set; } = null;
    public Predicate<ModAbilityButton>? Availability { get; set; } = null;
    public Predicate<ModAbilityButton>? Visibility { get; set; } = null;
    private VirtualInput? keyCode { get; set; } = null;
    private VirtualInput? subKeyCode { get; set; } = null;

    internal ModAbilityButton(bool isLeftSideButton = false, bool isArrangedAsKillButton = false,int priority = 0, bool alwaysShow = false)
    {

        VanillaButton = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton, HudManager.Instance.KillButton.transform.parent);
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));
        VanillaButton.cooldownTimerText.gameObject.SetActive(true);

        VanillaButton.buttonLabelText.GetComponent<TextTranslatorTMP>().enabled = false;
        var passiveButton = VanillaButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        passiveButton.OnClick.AddListener(() => DoClick());

        var gridContent = VanillaButton.gameObject.GetComponent<HudContent>();
        gridContent.UpdateSubPriority();
        gridContent.MarkAsKillButtonContent(isArrangedAsKillButton);
        gridContent.SetPriority(priority);
        gridContent.IsStaticContent = alwaysShow;
        NebulaGameManager.Instance?.HudGrid.RegisterContent(gridContent, isLeftSideButton);

        SetLabelType(Virial.Components.AbilityButton.LabelType.Standard);

    }

    void IGameEntity.OnReleased()
    {
        if (VanillaButton) UnityEngine.Object.Destroy(VanillaButton.gameObject);
    }

    private bool CheckMouseClick()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            var dis = (UnityEngine.Vector2)Input.mousePosition - new UnityEngine.Vector2(Screen.width, Screen.height) * 0.5f;
            return dis.magnitude < 280f;
        }
        return false;
    }

    public bool IsVisible => Visibility?.Invoke(this) ?? true;
    public bool IsAvailable => IsVisible && IsAvailableUnsafe;
    private bool IsAvailableUnsafe => Availability?.Invoke(this) ?? true;

    void IGameEntity.HudUpdate()
    {
        //表示・非表示切替
        VanillaButton.gameObject.SetActive(IsVisible);
        //使用可能性切替
        if (IsAvailableUnsafe)
            VanillaButton.SetEnabled();
        else
            VanillaButton.SetDisabled();

        OnUpdate?.Invoke(this);

        if (EffectActive && (EffectTimer == null || !EffectTimer.IsInProcess)) InactivateEffect();

        VanillaButton.SetCooldownFill(CurrentTimer?.Percentage ?? 0f);

        string timerText = "";
        if (CurrentTimer?.IsInProcess ?? false) timerText = Mathf.CeilToInt(CurrentTimer.CurrentTime).ToString();
        VanillaButton.cooldownTimerText.text = timerText;
        VanillaButton.cooldownTimerText.color = EffectActive ? Color.green : Color.white;

        if ((keyCode?.KeyDownInGame ?? false) || (canUseByMouseClick && CheckMouseClick())) DoClick();
        if (subKeyCode?.KeyDownInGame ?? false) DoSubClick();
        
    }

    void IGameEntity.OnMeetingStart()
    {
        OnMeeting?.Invoke(this);
    }


    public ModAbilityButton InactivateEffect()
    {
        if (!EffectActive) return this;
        EffectActive = false;
        OnEffectEnd?.Invoke(this);
        return this;
    }

    public ModAbilityButton ToggleEffect()
    {
        if (EffectActive)
            InactivateEffect();
        else
            ActivateEffect();

        return this;
    }

    public ModAbilityButton ActivateEffect()
    {
        if (EffectActive) return this;
        EffectActive = true;
        EffectTimer?.Start();
        OnEffectStart?.Invoke(this);
        return this;
    }

    public ModAbilityButton StartCoolDown()
    {
        CoolDownTimer?.Start();
        return this;
    }

    public bool UseCoolDownSupport { get; set; } = true;

    void IGameEntity.OnGameReenabled() {
        if (UseCoolDownSupport) StartCoolDown();
        OnStartTaskPhase?.Invoke(this);
    }

    void IGameEntity.OnGameStart() {
        if (UseCoolDownSupport && CoolDownTimer != null) CoolDownTimer!.Start(Mathf.Min(CoolDownTimer!.Max, CoolDownOnGameStart));
        OnStartTaskPhase?.Invoke(this);
    }

    public ModAbilityButton DoClick()
    {
        //効果中でなく、クールダウン中ならばなにもしない
        if (!EffectActive && (CoolDownTimer?.IsInProcess ?? false)) return this;
        //使用可能でないかを判定 (ボタン発火のタイミングと可視性更新のタイミングにずれが生じうるためここで再計算)
        if (!IsAvailable) return this;

        OnClick?.Invoke(this);
        return this;
    }

    public ModAbilityButton DoSubClick()
    {
        //見えないボタンは使用させない
        if (!(Visibility?.Invoke(this) ?? true)) return this;

        OnSubAction?.Invoke(this);
        return this;
    }

    public ModAbilityButton SetSprite(Sprite? sprite)
    {
        VanillaButton.graphic.sprite = sprite;
        if (sprite != null) VanillaButton.graphic.SetCooldownNormalizedUvs();
        return this;
    }

    public ModAbilityButton SetLabel(string translationKey) => SetRawLabel(Language.Translate("button.label." + translationKey));
    
    public ModAbilityButton SetRawLabel(string rawText)
    {
        VanillaButton.buttonLabelText.text = rawText;
        return this;
    }

    public ModAbilityButton SetLabelType(Virial.Components.AbilityButton.LabelType labelType)
    {
        Material? material = null;
        switch (labelType)
        {
            case Virial.Components.AbilityButton.LabelType.Standard:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial;
                break;
            case Virial.Components.AbilityButton.LabelType.Utility:
                material = HudManager.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton].FontMaterial;
                break;
            case Virial.Components.AbilityButton.LabelType.Impostor:
                material = RoleManager.Instance.GetRole(RoleTypes.Shapeshifter).Ability.FontMaterial;
                break;
            case Virial.Components.AbilityButton.LabelType.Crewmate:
                material = RoleManager.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial;
                break;

        }
        if (material != null) VanillaButton.buttonLabelText.SetSharedMaterial(material);
        return this;
    }

    public TMPro.TextMeshPro ShowUsesIcon(int iconVariation)
    {
        ButtonEffect.ShowUsesIcon(VanillaButton,iconVariation,out var text);
        return text;
    }

    public ModAbilityButton ResetKeyBind()
    {
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));
        keyCode = null;
        subKeyCode = null;
        return this;
    }

    public ModAbilityButton KeyBind(Virial.Compat.VirtualKeyInput keyCode) => KeyBind(NebulaInput.GetInput(keyCode));
    public ModAbilityButton KeyBind(VirtualInput keyCode)
    {
        VanillaButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));

        this.keyCode= keyCode;
        ButtonEffect.SetKeyGuide(VanillaButton.gameObject, keyCode.TypicalKey);
        return this;
    }

    private static SpriteLoader aidActionSprite = SpriteLoader.FromResource("Nebula.Resources.KeyBindOption.png", 100f);
    public ModAbilityButton SubKeyBind(Virial.Compat.VirtualKeyInput keyCode) => SubKeyBind(NebulaInput.GetInput(keyCode));
    public ModAbilityButton SubKeyBind(VirtualInput keyCode)
    {
        this.subKeyCode = keyCode;
        var guideObj = ButtonEffect.SetSubKeyGuide(VanillaButton.gameObject, keyCode.TypicalKey, false);

        if (guideObj != null)
        {
            var renderer = UnityHelper.CreateObject<SpriteRenderer>("HotKeyOption", guideObj.transform, new UnityEngine.Vector3(0.12f, 0.07f, -2f));
            renderer.sprite = aidActionSprite.GetSprite();
        }

        return this;
    }

    private bool canUseByMouseClick = false;
    public ModAbilityButton SetCanUseByMouseClick(bool onlyLook = false)
    {
        if(!onlyLook)canUseByMouseClick = true;
        ButtonEffect.SetMouseActionIcon(VanillaButton.gameObject, true);
        return this;
    }

    Virial.Components.AbilityButton Virial.Components.AbilityButton.SetImage(Image image) => SetSprite(image.GetSprite());
    Virial.Components.AbilityButton Virial.Components.AbilityButton.SetLabel(string translationKey) => SetLabel(translationKey);

    Virial.Components.AbilityButton Virial.Components.AbilityButton.SetCoolDownTimer(GameTimer timer)
    {
        CoolDownTimer = timer as Timer;
        return this;
    }

    Virial.Components.AbilityButton Virial.Components.AbilityButton.SetEffectTimer(GameTimer timer)
    {
        EffectTimer = timer as Timer;
        return this;
    }

    GameTimer? Virial.Components.AbilityButton.GetCoolDownTimer() => CoolDownTimer;

    GameTimer? Virial.Components.AbilityButton.GetEffectTimer() => EffectTimer;

    Virial.Components.AbilityButton Virial.Components.AbilityButton.StartCoolDown() => StartCoolDown();

    Virial.Components.AbilityButton Virial.Components.AbilityButton.StartEffect() => ActivateEffect();

    Virial.Components.AbilityButton Virial.Components.AbilityButton.InterruptEffect() => InactivateEffect();

    Virial.Components.AbilityButton Virial.Components.AbilityButton.SetLabelType(Virial.Components.AbilityButton.LabelType type) => SetLabelType(type);

    Predicate<Virial.Components.AbilityButton> Virial.Components.AbilityButton.Availability { set => Availability = value; }
    Predicate<Virial.Components.AbilityButton> Virial.Components.AbilityButton.Visibility { set => Visibility = value; }
    Action<Virial.Components.AbilityButton> Virial.Components.AbilityButton.OnUpdate { set => OnUpdate = value; }
    Action<Virial.Components.AbilityButton> Virial.Components.AbilityButton.OnClick { set => OnClick = value; }
    Action<Virial.Components.AbilityButton> Virial.Components.AbilityButton.OnSubAction { set => OnSubAction = value; }
    Action<Virial.Components.AbilityButton> Virial.Components.AbilityButton.OnEffectStart { set => OnEffectStart = value; }
    Action<Virial.Components.AbilityButton> Virial.Components.AbilityButton.OnEffectEnd { set => OnEffectEnd = value; }

    Virial.Components.AbilityButton Virial.Components.AbilityButton.BindKey(VirtualKeyInput input) => KeyBind(input);
    Virial.Components.AbilityButton Virial.Components.AbilityButton.BindSubKey(VirtualKeyInput input) => SubKeyBind(input);

    public Virial.Components.AbilityButton SetUpAsAbilityButton(Virial.IBinder binder, float coolDown, Action onClick) => SetUpAsAbilityButton(binder,null,null,coolDown,onClick);

    public Virial.Components.AbilityButton SetUpAsAbilityButton(Virial.IBinder binder, Func<bool>? canUseAbilityNow, Func<bool>? hasAbilityCharge, float coolDown, Action onClick)
    {
        KeyBind(Virial.Compat.VirtualKeyInput.Ability);
        Availability = button => (canUseAbilityNow?.Invoke() ?? true) && PlayerControl.LocalPlayer.CanMove;
        Visibility = button => (hasAbilityCharge?.Invoke() ?? true) && !PlayerControl.LocalPlayer.Data.IsDead;
        OnClick = button => { onClick(); CoolDownTimer?.Start(); };
        CoolDownTimer = binder.Bind(new Timer(coolDown)).SetAsAbilityCoolDown().Start();
        OnEffectEnd = button => StartCoolDown();

        return this;
    }

    public Virial.Components.AbilityButton SetUpAsEffectButton(Virial.IBinder binder, float coolDown, float duration, Action onEffectStart, Action? onEffectEnd = null) => SetUpAsEffectButton(binder, null, null, coolDown, duration, onEffectStart, onEffectEnd);
    public Virial.Components.AbilityButton SetUpAsEffectButton(Virial.IBinder binder, System.Func<bool>? canUseAbilityNow, System.Func<bool>? hasAbilityCharge, float coolDown, float duration, Action onEffectStart, Action? onEffectEnd = null)
    {
        KeyBind(Virial.Compat.VirtualKeyInput.Ability);
        Availability = button => (canUseAbilityNow?.Invoke() ?? true) && PlayerControl.LocalPlayer.CanMove;
        Visibility = button => (hasAbilityCharge?.Invoke() ?? true)  && !PlayerControl.LocalPlayer.Data.IsDead;
        OnClick = button => button.ActivateEffect();
        OnEffectStart = (button) =>
        {
            onEffectStart.Invoke();
        };
        OnEffectEnd = (button) =>
        {
            onEffectEnd?.Invoke();
            StartCoolDown();
        };
        OnMeeting = button => button.StartCoolDown();
        EffectTimer = binder.Bind(new Timer(duration));
        CoolDownTimer = binder.Bind(new Timer(coolDown)).SetAsAbilityCoolDown().Start();
        OnEffectEnd = button => StartCoolDown();

        return this;
    }

    public PoolablePlayer? GeneratePlayerIcon(GamePlayer? player)
    {
        if (player == null) return null;
        return AmongUsUtil.GetPlayerIcon(player.DefaultOutfit.outfit, VanillaButton.transform, new UnityEngine.Vector3(-0.4f, 0.35f, -0.5f), new(0.3f, 0.3f)).SetAlpha(0.5f);
    }
}

public static class ButtonEffect
{
    [NebulaPreLoad]
    public class KeyCodeInfo
    {
        public static string? GetKeyDisplayName(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Return)
                return "Return";
            if (AllKeyInfo.TryGetValue(keyCode, out var val)) return val.TranslationKey;
            return null;
        }

        static public Dictionary<KeyCode, KeyCodeInfo> AllKeyInfo = new();
        public KeyCode keyCode { get; private set; }
        public DividedSpriteLoader textureHolder { get; private set; }
        public int num { get; private set; }
        public string TranslationKey { get; private set; }
        public KeyCodeInfo(KeyCode keyCode, string translationKey, DividedSpriteLoader spriteLoader, int num)
        {
            this.keyCode = keyCode;
            this.TranslationKey = translationKey;
            this.textureHolder = spriteLoader;
            this.num = num;

            AllKeyInfo.Add(keyCode, this);
        }

        public Sprite Sprite => textureHolder.GetSprite(num);
        public static void Load()
        {
            DividedSpriteLoader spriteLoader;
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters0.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.Tab, "Tab", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.Space, "Space", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.Comma, "<", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.Period, ">", spriteLoader, 3);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters1.png", 100f, 18, 19, true);
            for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
                new KeyCodeInfo(key, ((char)('A' + key - KeyCode.A)).ToString(), spriteLoader, key - KeyCode.A);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters2.png", 100f, 18, 19, true);
            for (int i = 0; i < 15; i++)
                new KeyCodeInfo(KeyCode.F1 + i, "F" + (i + 1), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters3.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.RightShift, "RShift", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.LeftShift, "LShift", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.RightControl, "RControl", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.LeftControl, "LControl", spriteLoader, 3);
            new KeyCodeInfo(KeyCode.RightAlt, "RAlt", spriteLoader, 4);
            new KeyCodeInfo(KeyCode.LeftAlt, "LAlt", spriteLoader, 5);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters4.png", 100f, 18, 19, true);
            for (int i = 0; i < 6; i++)
                new KeyCodeInfo(KeyCode.Mouse1 + i, "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()), spriteLoader, i);
            spriteLoader = DividedSpriteLoader.FromResource("Nebula.Resources.KeyBindCharacters5.png", 100f, 18, 19, true);
            for (int i = 0; i < 10; i++)
                new KeyCodeInfo(KeyCode.Alpha0 + i, "0" + (i + 1), spriteLoader, i);
        }
    }

    private static IDividedSpriteLoader textureUsesIconsSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.UsesIcon.png", 100f, 10);
    static public GameObject ShowUsesIcon(this ActionButton button)
    {
        Transform template = HudManager.Instance.AbilityButton.transform.GetChild(2);
        var usesObject = GameObject.Instantiate(template.gameObject);
        usesObject.transform.SetParent(button.gameObject.transform);
        usesObject.transform.localScale = template.localScale;
        usesObject.transform.localPosition = template.localPosition * 1.2f;
        return usesObject;
    }

    static public GameObject ShowUsesIcon(this ActionButton button, int iconVariation, out TMPro.TextMeshPro text)
    {
        GameObject result = ShowUsesIcon(button);
        var renderer = result.GetComponent<SpriteRenderer>();
        renderer.sprite = textureUsesIconsSprite.GetSprite(iconVariation);
        text = result.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
        return result;
    }

    static public SpriteRenderer AddOverlay(this ActionButton button, Sprite sprite, float order)
    {
        GameObject obj = new GameObject("Overlay");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(button.gameObject.transform);
        obj.transform.localScale = new(1, 1, 1);
        obj.transform.localPosition = new(0, 0, -1f - order);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    private static SpriteLoader lockedButtonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.LockedButton.png", 100f);
    static public SpriteRenderer AddLockedOverlay(this ActionButton button) => AddOverlay(button, lockedButtonSprite.GetSprite(), 0f);
    

    static ISpriteLoader keyBindBackgroundSprite = SpriteLoader.FromResource("Nebula.Resources.KeyBindBackground.png", 100f);
    static ISpriteLoader mouseActionSprite = SpriteLoader.FromResource("Nebula.Resources.MouseActionIcon.png", 100f);

    static public GameObject? AddKeyGuide(GameObject button, KeyCode key, UnityEngine.Vector2 pos,bool removeExistingGuide)
    {
        if(removeExistingGuide)button.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)(obj => { if (obj.name == "HotKeyGuide") GameObject.Destroy(obj); }));

        Sprite? numSprite = null;
        if (KeyCodeInfo.AllKeyInfo.ContainsKey(key)) numSprite = KeyCodeInfo.AllKeyInfo[key].Sprite;
        if (numSprite == null) return null;

        GameObject obj = new GameObject();
        obj.name = "HotKeyGuide";
        obj.transform.SetParent(button.transform);
        obj.layer = button.layer;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = (UnityEngine.Vector3)pos + new UnityEngine.Vector3(0f, 0f, -10f);
        renderer.sprite = keyBindBackgroundSprite.GetSprite();

        GameObject numObj = new GameObject();
        numObj.name = "HotKeyText";
        numObj.transform.SetParent(obj.transform);
        numObj.layer = button.layer;
        renderer = numObj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = new(0, 0, -1f);
        renderer.sprite = numSprite;

        return obj;
    }
    static public GameObject? SetKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide = true)
    {
        return AddKeyGuide(button, key, new(0.48f, 0.48f),removeExistingGuide);
    }

    static public GameObject? SetSubKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide)
    {
        return AddKeyGuide(button, key, new(0.48f, 0.13f),removeExistingGuide);
    }

    static public GameObject? SetKeyGuideOnSmallButton(GameObject button, KeyCode key)
    {
        return AddKeyGuide(button, key, new(0.28f, 0.28f), true);
    }

    static public GameObject? SetMouseActionIcon(GameObject button,bool show)
    {
        if (!show)
        {
            button.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)(obj => { if (obj.name == "MouseAction") GameObject.Destroy(obj); }));
            return null;
        }
        else
        {
            GameObject obj = new GameObject();
            obj.name = "MouseAction";
            obj.transform.SetParent(button.transform);
            obj.layer = button.layer;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.transform.localPosition = new(0.48f, -0.29f, -10f);
            renderer.sprite = mouseActionSprite.GetSprite();
            return obj;
        }
        
    }
}