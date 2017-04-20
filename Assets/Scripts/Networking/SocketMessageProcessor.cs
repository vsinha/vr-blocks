using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class SocketMessageProcessor
{
    private Dictionary<string, HandlerDescriptor> handlers = new Dictionary<string, HandlerDescriptor>();

    public void Process(string data)
    {
        var action = JsonUtility.FromJson<MessageActionBase>(data);

        if (action == null || action.action == null)
        {
            Debug.LogWarning("Failed to deserialize message as action");
            return;
        }

        HandlerDescriptor handler;
        if (handlers.TryGetValue(action.action, out handler))
        { 
            this.Dispatch(() => handler.handler(data));       
        }
        else
        {
            Debug.LogWarning("No handler registered for " + action.action);
        }
    }

    private void Dispatch(Action action)
    {
        MainThreadDispatcher.Instance.Enqueue(action);
    }

    internal void RegisterActionHandler<T>(string action, Action<MessageAction<T>> handler, Func<bool> predicate = null) where T : class
    {
        this.handlers.Add(action, new HandlerDescriptor()
        {
            messageType = typeof(MessageAction<T>),
            handler = (data) =>
            {
                object deserialized = JsonUtility.FromJson(data, typeof(MessageAction<T>));

                if (deserialized == null)
                {
                    Debug.LogWarningFormat("Failed to deserialize message {0} as {1}", action, typeof(MessageAction<T>));
                }


                if (predicate == null || predicate())
                {
                    Debug.LogFormat("Dispatching to handler for {0}", action);
                    handler((MessageAction<T>)deserialized);
                }
                else
                {
                    Debug.LogFormat("Skipped dispatch to handler for {0} due to predicate filter", action);
                }
            }
        });
    }

    private class HandlerDescriptor
    {
        internal Type messageType;
        internal Action<string> handler;
    }

}

