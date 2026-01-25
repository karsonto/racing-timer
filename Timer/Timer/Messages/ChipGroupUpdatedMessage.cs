using CommunityToolkit.Mvvm.Messaging.Messages;
using Timer.Models;

namespace Timer.Messages;

/// <summary>
/// 芯片组被更新（名称/颜色等）时的消息，用于跨页面实时刷新 UI。
/// </summary>
public sealed class ChipGroupUpdatedMessage : ValueChangedMessage<ChipGroup>
{
    public ChipGroupUpdatedMessage(ChipGroup value) : base(value)
    {
    }
}


