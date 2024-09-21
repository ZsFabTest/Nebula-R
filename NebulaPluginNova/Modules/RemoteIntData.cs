using Microsoft.CodeAnalysis.CSharp.Syntax;
using MS.Internal.Xml.XPath;

namespace Nebula.Modules;

/// <summary>
/// 保存对应的RemoteIntDataId或RemoteIntDataBase
/// </summary>
internal enum RemoteIntDataId
{
    DefaultIntData,
    AmnesiacItemExistData,
    KnightDataBase = 100,
}

/// <summary>
/// 自动进行远程同步的int数据
/// </summary>
[NebulaRPCHolder]
internal class RemoteIntData
{
    /// <summary>
    /// RemoteIntData池 用于保存所有同步的RemoteIntData 不可从外部修改
    /// </summary>
    private static List<RemoteIntData?> RemoteIntDatas = new();

    /// <summary>
    /// RemoteIntData值 默认不可直接访问 有Get()方法和隐式转换
    /// </summary>
    private int value;
    /// <summary>
    /// RemoteIntData id 默认不可直接访问 初始化后不可修改
    /// </summary>
    private int Id { get; init; }
    /// <summary>
    /// 构建RemoteIntData并判断是否需要进行同步
    /// </summary>
    /// <param name="id">int值</param>
    /// <param name="data">初始数据</param>
    /// <param name="skipInit">是否跳过初始化同步</param>
    private RemoteIntData(int id, int data, bool skipInit) 
    { 
        value = data;
        Id = id;
        RemoteIntDatas.Add(this);
        if (!skipInit) InitRemoteData(id, data);
        Debug.Log($"RD: RemoteDataId - {Id}");
    }
    /// <summary>
    /// 构建RemoteIntData
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    /// <param name="data">初始数据</param>
    public RemoteIntData(int id, int data) : this(id, data, false) { }
    /// <summary>
    /// 构建RemoteIntData
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    /// <param name="data">初始数据</param>
    public RemoteIntData(RemoteIntDataId id, int data) : this((int)id, data, false) { }

    /// <summary>
    /// 若没有对应值返回 int.MinValue.
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    public static int Get(int id)
    {
        foreach (var rid in RemoteIntDatas)
        {
            //Debug.LogWarning(rid?.id);
            //Debug.LogWarning(rid?.value);
            //Debug.LogWarning("");
            if ((rid?.Id ?? int.MinValue) == id)
            {
                return rid!.value;
            }
        }
        return int.MinValue;
    }
    /// <summary>
    /// 若没有对应值返回 int.MinValue.
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    public static int Get(RemoteIntDataId id) => Get((int)id);
    /// <summary>
    /// 返回RemoteIntData值 建议使用隐式转换
    /// </summary>
    public int Get() => value;

    /// <summary>
    /// 若成功更新则返回 ture 否则返回 false.
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    /// <param name="data">需要修改为的数据</param>
    public static bool Update(int id, int data)
    {
        UpdateRemoteIntData(id, data);
        Debug.Log($"Find: {id}; Remote Datas:");
        foreach (var rid in RemoteIntDatas)
        {
            Debug.Log(rid?.Id ?? int.MinValue);
            if ((rid?.Id ?? int.MinValue) == id)
            {
                rid!.value = data;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 若成功更新则返回 ture 否则返回 false.
    /// </summary>
    /// <param name="id">可以使用int或RemoteIntDataId</param>
    /// <param name="data">需要修改为的数据</param>
    public static bool Update(RemoteIntDataId id, int data) => Update((int)id, data);
    /// <summary>
    /// 若成功更新则返回 ture 否则返回 false.
    /// </summary>
    /// <param name="data">需要修改为的数据</param>
    public bool Update(int data)
    {
        Debug.Log($"RID Update Event: {Id}");
        return Update(Id, data);
    }

    /// <summary>
    /// 获取对应id的RemoteIntData
    /// </summary>
    /// <param name="id">需要查寻的id 可以使用int或RemoteIntDataId</param>
    /// <returns></returns>
    public static RemoteIntData? GetRemoteIntData(int id)
    {
        return RemoteIntDatas.FirstOrDefault((d) => (d?.Id ?? int.MinValue) == id);
    }
    /// <summary>
    /// 获取对应id的RemoteIntData
    /// </summary>
    /// <param name="id">需要查寻的id 可以使用int或RemoteIntDataId</param>
    /// <returns></returns>
    public static RemoteIntData? GetRemoteIntData(RemoteIntDataId id) => GetRemoteIntData((int)id);

    /// <summary>
    /// 同步初始化RemoteIntData的Rpc
    /// </summary>
    private static readonly RemoteProcess<(int id, int data)> RpcInitRemoteData = new RemoteProcess<(int id, int data)>(
        "InitRemoteData",
        (message, isCallByMe) => {
            if (isCallByMe) return;
            for (int i = 0; i < RemoteIntDatas.Count; i++)
            {
                if ((RemoteIntDatas[i]?.Id ?? int.MinValue) == message.id)
                {
                    RemoteIntDatas[i] = new RemoteIntData(message.id, message.data, true);
                    return;
                }
            }
            RemoteIntDatas.Add(new RemoteIntData(message.id, message.data, true));
        });
    /// <summary>
    /// 同步初始化RemoteIntData的Rpc
    /// </summary>
    /// <param name="id">int值</param>
    /// <param name="data">初始值</param>
    private static void InitRemoteData(int id, int data) => RpcInitRemoteData.Invoke((id, data));

    /// <summary>
    /// 同步更新RemoteIntData的Rpc
    /// </summary>
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
                if ((rid?.Id ?? int.MinValue) == message.id)
                {
                    rid!.value = message.data;
                    break;
                }
            }
        });
    /// <summary>
    /// 同步初始化RemoteIntData的Rpc
    /// </summary>
    /// <param name="id">int值</param>
    /// <param name="data">所需更新的值</param>
    private static void UpdateRemoteIntData(int id, int data) => RpcUpdateRemoteIntData.Invoke((id, data));

    // 隐式从RemoteIntData转为int
    public static implicit operator int(RemoteIntData RemoteData) => RemoteData.value;
    //public static implicit operator RemoteIntData(int Data) => new RemoteIntData(RemoteIntDataId.DefaultIntData, Data);
    
    /// <summary>
    /// 返回更改了id的数据相同的新RemoteIntData
    /// </summary>
    /// <param name="new_id"></param>
    /// <returns></returns>
    public RemoteIntData ModifyId(int new_id) => new RemoteIntData(new_id, value);
    public RemoteIntData ModifyId(RemoteIntDataId new_id) => ModifyId((int)new_id);
    // 隐式从int转为RemoteIntData id为 RemoteIntDataId.DefaultIntData(0)
    //public static implicit operator RemoteIntData(int data) => new(RemoteIntDataId.DefaultIntData, data);

    public int rid_id => Id;
}

/// <summary>
/// 提供提前准备好的已经被创建的RemoteIntData避免端与端之间不同步并简化名称
/// </summary>
public static class StaticRemoteIntData
{
    internal static RemoteIntData ToRemoteData(this int value, int id) => new RemoteIntData(id, value);
    internal static RemoteIntData AmnesiacIsExists = new(RemoteIntDataId.AmnesiacItemExistData, 0);
}