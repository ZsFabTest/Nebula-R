﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Game;
using Virial.Media;
using Virial.Text;

namespace Virial.Configuration;

public enum FloatConfigurationDecorator
{
    /// <summary>
    /// 値は修飾されません。
    /// </summary>
    None,
    /// <summary>
    /// 倍率を設定するオプションとして値を修飾します。
    /// </summary>
    Ratio,
    /// <summary>
    /// 秒数を設定するオプションとして値を修飾します。
    /// </summary>
    Second
}

/// <summary>
/// ベント設定オプションのエディタです。
/// </summary>
public interface IVentConfiguration : IConfiguration
{
    /// <summary>
    /// ベントの使用可能回数を返します。
    /// </summary>
    public int Uses { get; }

    /// <summary>
    /// ベント使用のクールダウンを返します。
    /// </summary>
    public float CoolDown { get; }

    /// <summary>
    /// ベントに潜伏できる時間を返します。
    /// </summary>
    public float Duration { get; }

    /// <summary>
    /// ベントを使用できるかどうかを取得します。
    /// </summary>
    public bool CanUseVent { get; }
}

/// <summary>
/// クールダウン設定で使用する、クールダウンの設定方法です。
/// </summary>
public enum CoolDownType
{
    Immediate = 0,
    Relative = 1,
    Ratio = 2
}


/// <summary>
/// クールダウン設定のエディタです。
/// </summary>
public interface IRelativeCoolDownConfiguration : IConfiguration{
    /// <summary>
    /// 現在のクールダウンです。
    /// </summary>
    float CoolDown { get; }
}

public interface Configurations
{
    /// <summary>
    /// 真偽値型の共有変数を生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    IOrderedSharableVariable<bool> SharableVariable(string id, bool defaultValue);

    /// <summary>
    /// 0から指定された最大値までの値のいずれかを格納できる整数型の共有変数を生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="defaultValue"></param>
    /// <param name="maxValueExcluded">最大値です。指定された値は含まれません。</param>
    /// <returns></returns>
    ISharableVariable<int> SharableVariable(string id, int defaultValue, int maxValueExcluded);

    /// <summary>
    /// 指定された値のいずれかを格納できる実数型の共有変数を生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    IOrderedSharableVariable<float> SharableVariable(string id, float[] values, float defaultValue);

    /// <summary>
    /// 指定された値のいずれかを格納できる整数型の共有変数を生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    IOrderedSharableVariable<int> SharableVariable(string id, int[] values, int defaultValue);

    /// <summary>
    /// ホルダを生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tabs"></param>
    /// <param name="gamemodes"></param>
    /// <returns></returns>
    IConfigurationHolder Holder(string id, IEnumerable<ConfigurationTab> tabs, IEnumerable<GameModeDefinition> gamemodes);

    /// <summary>
    /// モディファイアフィルタを生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    ModifierFilter ModifierFilter(string id);

    /// <summary>
    /// 幽霊役職フィルタを生成します。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    GhostRoleFilter GhostRoleFilter(string id);

    BoolConfiguration Configuration(string id, bool defaultValue, Func<bool>? predicate = null);
    IntegerConfiguration Configuration(string id, int[] selection, int defaultValue, Func<bool>? predicate = null);
    FloatConfiguration FloatOption(string id, float[] selection, float defaultValue, Func<bool>? predicate) => FloatOption(id, selection, defaultValue, predicate: predicate);
    FloatConfiguration Configuration(string id, float[] selection, float defaultValue, FloatConfigurationDecorator decorator = FloatConfigurationDecorator.None, Func<bool>? predicate = null);
    ValueConfiguration<int> Configuration(string id, string[] selection, int defualtIndex, Func<bool>? predicate = null);
    IConfiguration Configuration(Func<string?> displayShower, GUIWidgetSupplier editor, Func<bool>? predicate = null);

    IVentConfiguration VentConfiguration(string id, bool isOptional, int[]? usesSelection, int usesDefaultValue, float[]? coolDownSelection, float coolDownDefaultValue, float[]? durationSelection, float durationDefaultValue);
    IRelativeCoolDownConfiguration KillConfiguration(string id, CoolDownType defaultType, float[] immediateSelection, float immediateDefaultValue, float[] relativeSelection, float relativeDefaultValue, float[] ratioSelection, float ratioDefaultValue)
        => KillConfiguration(NebulaAPI.GUI.LocalizedTextComponent(id), id, defaultType, immediateSelection, immediateDefaultValue, relativeSelection, relativeDefaultValue, ratioSelection, ratioDefaultValue);
    IRelativeCoolDownConfiguration KillConfiguration(TextComponent title, string id, CoolDownType defaultType, float[] immediateSelection, float immediateDefaultValue, float[] relativeSelection, float relativeDefaultValue, float[] ratioSelection, float ratioDefaultValue);

    /// <summary>
    /// ゲーム内設定画面を開いているとき、画面の更新を要求します。
    /// </summary>
    void RequireUpdateSettingScreen();
}
