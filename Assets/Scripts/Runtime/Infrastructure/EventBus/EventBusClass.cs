using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件总线
/// 先订阅事件，在发布事件的时候去执行订阅了事件的函数
/// </summary>
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
    /// 订阅事件，需要最先执行，把函数内容注册到字典中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (eventHandlers.TryGetValue(type, out var existing))
        {
            // 如果已有订阅者，将新回调合并到委托链中
            eventHandlers[type] = Delegate.Combine(existing, handler);
        }
        else
        {
            // 如果是第一个订阅者，直接存入
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
            // 从委托链中移除指定的回调
            var current = Delegate.Remove(existing, handler);

            if (current == null)
                eventHandlers.Remove(type); // 如果链空了，清理字典键，防止内存泄漏
            else
                eventHandlers[type] = current;
        }
    }

    /// <summary>
    /// 发布事件，在发布事件的时候去执行订阅了事件的函数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventData"></param>
    public void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (eventHandlers.TryGetValue(type, out var del))
        {
            // 将基类 Delegate 强转回具体的 Action<T>
            var callback = del as Action<T>;
            callback?.Invoke(eventData); // 安全调用
        }
    }
}
