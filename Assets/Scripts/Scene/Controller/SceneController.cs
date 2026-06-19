using SM.Contracts.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 씬 공통사항
/// </summary>
public class SceneController : MonoBehaviour
{
    private readonly Dictionary<Type, Action<ResponsePacket>> _packetHandlers = new();

    protected virtual void Awake()
    {
        Client.Instance.SceneMgr.RegisterController(this);
    }

    public virtual void OnSceneExit() { }

    protected void RegisterPacketHandler<T>(Action<T> handler) where T : ResponsePacket
    {
        _packetHandlers[typeof(T)] = packet => handler((T)packet);
    }

    public void ReceivePendingPacket(Type type, ResponsePacket packet)
    {
        if (_packetHandlers.TryGetValue(type, out var handler))
        {
            handler(packet);
        }
        else
        {
            Debug.LogWarning($"처리할 핸들러 없음: {type.Name}");
        }
    }
}
