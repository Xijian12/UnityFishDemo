using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBusClass
{
    public static EventBusClass Instance = new EventBusClass();
    private readonly Dictionary<Type, Delegate> eventHandlers = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public EventBusClass()
    {
        Instance = this;
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (eventHandlers.TryGetValue(type, out var existing))
        {
            eventHandlers[type] = Delegate.Combine(existing, handler);
        }
        else
        {
            eventHandlers[type] = handler;
        }
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    public void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (eventHandlers.TryGetValue(type, out var existing))
        {
            var current = Delegate.Remove(existing, handler);

            if (current == null)
                eventHandlers.Remove(type);
            else
                eventHandlers[type] = current;
        }
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventData"></param>
    public void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (eventHandlers.TryGetValue(type, out var del))
        {
            var callback = del as Action<T>;
            callback?.Invoke(eventData);
        }
    }
}
