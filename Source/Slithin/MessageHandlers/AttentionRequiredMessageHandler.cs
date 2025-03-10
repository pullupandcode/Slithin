﻿using Slithin.Messages;
using Slithin.Core.Messaging;
using Slithin.Core;

namespace Slithin.MessageHandlers;

public class AttentionRequiredMessageHandler : IMessageHandler<AttentionRequiredMessage>
{
    public async void HandleMessage(AttentionRequiredMessage message)
    {
        var result = await DialogService.ShowDialog(message.Text);

        if (result)
        {
            message.Action(message);
        }
    }
}
