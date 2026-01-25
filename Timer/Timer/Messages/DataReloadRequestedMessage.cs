using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Timer.Messages;

/// <summary>
/// 请求重新加载某类数据（适用于批量变更：导入/批量分配/批量删除等）。
/// </summary>
public sealed class DataReloadRequestedMessage : ValueChangedMessage<DataDomain>
{
    public DataReloadRequestedMessage(DataDomain value) : base(value)
    {
    }
}



