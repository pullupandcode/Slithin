﻿using System;
using System.Collections.Generic;
using Slithin.Core.Messaging;
using Slithin.Core;

namespace Slithin.Core.Messaging;

public class MessageRouter
{
    private readonly Dictionary<Type, Action<object>> _handlers = new();

    public void Register<T>(Action<T> handler)
        where T : AsynchronousMessage
    {
        if (!_handlers.ContainsKey(typeof(T)))
        {
            _handlers.Add(typeof(T), (_) => handler((T)_));
        }
    }

    public void Register<T>(IMessageHandler<T> handler)
        where T : AsynchronousMessage
    {
        if (!_handlers.ContainsKey(typeof(T)))
        {
            _handlers.Add(typeof(T), (_) => handler.HandleMessage((T)_));
        }
    }

    public bool Route(object msg)
    {
        var msgType = msg.GetType();

        if (_handlers.ContainsKey(msgType))
        {
            _handlers[msgType](msg);

            return true;
        }

        return false;
    }
}
