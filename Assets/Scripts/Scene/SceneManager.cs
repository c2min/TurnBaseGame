using Cysharp.Threading.Tasks;
using SM.Contracts.Core;
using SMDevLibrary.Network.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    [SerializeField]
    private SceneTransition _transition;

    private readonly Queue<(Type type, ResponsePacket packet)> _pendingPackets = new Queue<(Type type, ResponsePacket packet)>();

    private SceneController _currentController;

    private CancellationTokenSource _cts;

    protected void Awake()
    {
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
        _cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void ResetCts()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    public void LoadScene<TRequest>(string sceneName, TRequest requestPacket) where TRequest : RequestPacket
    {
        ResetCts();
        _pendingPackets.Clear();

        LoadSceneAsync(sceneName, requestPacket, _cts.Token).Forget();
    }

    private async UniTaskVoid LoadSceneAsync<TRequest>(string sceneName, TRequest requestPacket, CancellationToken ct) where TRequest : RequestPacket
    {
        // FadeOut은 취소하지 않음 — 취소 시 화면이 반쯤 어두운 채로 멈추는 것을 방지
        await _transition.FadeOutAsync();
        if (ct.IsCancellationRequested) return;

        _currentController?.OnSceneExit();
        _currentController = null;
        ResourceManager.Instance.Reset();

        ClientPacketRegistry.SetInterceptor(EnqueuePacket);
        UnityNetworkBridge.Instance.SendPacket(requestPacket);

        await UnitySceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);
    }

    public void LoadScene(string sceneName)
    {
        ResetCts();
        _pendingPackets.Clear();

        LoadSceneSimpleAsync(sceneName, _cts.Token).Forget();
    }

    private async UniTaskVoid LoadSceneSimpleAsync(string sceneName, CancellationToken ct)
    {
        await _transition.FadeOutAsync();
        if (ct.IsCancellationRequested) return;

        _currentController?.OnSceneExit();
        _currentController = null;
        ResourceManager.Instance.Reset();

        await UnitySceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);
    }

    public void RegisterController(SceneController controller)
    {
        _currentController = controller;
        FlushPendingPacketsAsync(_cts.Token).Forget();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {

    }

    private void EnqueuePacket(ResponsePacket packet)
    {
        _pendingPackets.Enqueue((packet.GetType(), packet));
    }

    private async UniTaskVoid FlushPendingPacketsAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.NextFrame(ct);
        }
        catch (OperationCanceledException)
        {
            // 씬 재전환 시 새 LoadScene이 인터셉터를 재설정하므로 여기서는 건드리지 않음
            return;
        }

        // 인터셉터를 null 대신 현재 컨트롤러로 라우팅하도록 유지
        // → 씬 로드 이후 도착하는 라이브 패킷도 _packetHandlers로 전달됨
        ClientPacketRegistry.SetInterceptor(packet =>
            _currentController?.ReceivePendingPacket(packet.GetType(), packet));

        while (_pendingPackets.TryDequeue(out var response))
        {
            _currentController.ReceivePendingPacket(response.type, response.packet);
        }

        try
        {
            await _transition.FadeInAsync().AttachExternalCancellation(ct);
        }
        catch (OperationCanceledException)
        {
            // 다음 LoadScene에서 FadeOut이 DOKill로 정리
        }
    }
}
