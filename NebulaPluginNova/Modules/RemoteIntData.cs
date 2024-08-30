using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using static Il2CppSystem.Xml.Schema.XsdDuration;

namespace Nebula.Modules;

internal enum RemoteIntDataId
{
    DefaultIntData,
    KnightDataBase = 100,
}

[NebulaRPCHolder]
internal class RemoteIntData
{
    private static List<RemoteIntData?> RemoteIntDatas = new();

    private int value;
    private int id;
    Action<int> update { get; init; }
    public RemoteIntData(int id, int data, bool skipInit = false) 
    { 
        value = data;
        this.id = id;
        update = (data) => { value = data; };
        RemoteIntDatas.Add(this);
        if (!skipInit) RpcInitRemoteData.Invoke((this.id, value));
    }
    // 防止编译器误报CS8618警告
    #pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public RemoteIntData(RemoteIntDataId id, int data) => new RemoteIntData((int)id, data);
    #pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

    /// <summary>
    /// 若没有对应值返回 int.MinValue.
    /// </summary>
    /// <param name="id"></param>
    public static int Get(int id)
    {
        foreach (var rid in RemoteIntDatas)
        {
            //Debug.LogWarning(rid?.id);
            //Debug.LogWarning(rid?.value);
            //Debug.LogWarning("");
            if ((rid?.id ?? int.MinValue) == id)
            {
                return rid!.value;
            }
        }
        return int.MinValue;
    }
    public static int Get(RemoteIntDataId id) => Get((int)id);
    public int Get() => value;

    /// <summary>
    /// 若成功更新则返回 ture 否则返回 false.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    public static bool Update(int id, int data)
    {
        RpcUpdateRemoteIntData.Invoke((id, data));
        foreach (var rid in RemoteIntDatas)
        {
            if ((rid?.id ?? int.MinValue) == id)
            {
                rid!.update.Invoke(data);
                return true;
            }
        }
        return false;
    }
    public static bool Update(RemoteIntDataId id, int data) => Update((int)id, data);
    public bool Update(int data) => Update(id, data);

    public static RemoteIntData? GetRemoteIntData(int id)
    {
        for (int i = 0; i < RemoteIntDatas.Count; i++)
        {
            if ((RemoteIntDatas[i]?.id ?? int.MinValue) == id)
            {
                return RemoteIntDatas[i];
            }
        }
        return null;
    }
    public static RemoteIntData? GetRemoteIntData(RemoteIntDataId id) => GetRemoteIntData((int)id);

    private static readonly RemoteProcess<(int id, int data)> RpcInitRemoteData = new RemoteProcess<(int id, int data)>(
        "InitRemoteData",
        (message, _) => {
            for (int i = 0; i < RemoteIntDatas.Count; i++)
            {
                if ((RemoteIntDatas[i]?.id ?? int.MinValue) == message.id)
                {
                    RemoteIntDatas[i] = new RemoteIntData(message.id, message.data, true);
                    return;
                }
            }
            RemoteIntDatas.Add(new RemoteIntData(message.id, message.data, true));
        });

    public static implicit operator int(RemoteIntData RemoteData) => RemoteData.value;

    public static implicit operator RemoteIntData(int data) => new(RemoteIntDataId.DefaultIntData, data);

    private static readonly RemoteProcess<(int id, int data)> RpcUpdateRemoteIntData = new RemoteProcess<(int id, int data)>(
        "UpdateRemoteIntData",
        (writer, message) => { 
            writer.Write(message.id);
            writer.Write(message.data);
        },
        (reader) => (reader.ReadInt32(), reader.ReadInt32()),
        (message, _) =>
        {
            foreach (var rid in RemoteIntDatas)
            {
                if ((rid?.id ?? int.MinValue) == message.id)
                {
                    rid!.update.Invoke(message.data);
                    break;
                }
            }
        });
}
