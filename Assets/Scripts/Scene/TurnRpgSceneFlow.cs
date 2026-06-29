using Cysharp.Threading.Tasks;
using SM.Contracts.Core;
using SMDevLibrary.Network.Utility;
using SMDevLibrary.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// turnrpg 씬 플로우 — engine-sdk SceneFlowManager 베이스(로드/FadeOut/CTS/훅·패킷-agnostic) 위에
// 네트워크 패킷 큐 정책만 잔류: 인터셉터 큐·요청 송신·컨트롤러 flush·FadeIn-after-flush.
public class TurnRpgSceneFlow : SceneFlowManager<TurnRpgSceneFlow>
{
    [SerializeField]
    private SceneTransition _transition;

    private readonly Queue<(Type type, ResponsePacket packet)> _pendingPackets = new();

    private RequestPacket _pendingRequest;

    protected override ISceneTransition Transition => _transition;

    // 패킷 운반 씬 전환: 요청 보관 후 베이스 로드 → OnBeforeLoadAsync에서 송신.
    public UniTask LoadScene<TRequest>(string sceneName, TRequest requestPacket) where TRequest : RequestPacket
    {
        _pendingRequest = requestPacket;
        return LoadScene(sceneName);
    }

    // INFO :: 로드 직전 — 리소스 정리 + (요청 있으면) 인터셉터 큐잉 + 요청 송신
    protected override UniTask OnBeforeLoadAsync(CancellationToken ct)
    {
        _pendingPackets.Clear();
        ResourceManager.Instance.Reset();

        if (_pendingRequest != null)
        {
            ClientPacketRegistry.SetInterceptor(EnqueuePacket);
            UnityNetworkBridge.Instance.SendPacket(_pendingRequest);
            _pendingRequest = null;
        }

        return UniTask.CompletedTask;
    }

    // INFO :: 컨트롤러 등록 시(조건1 트리거) — 큐 flush 후 FadeIn(조건2: 빈 씬 노출 방지)
    protected override void OnControllerRegistered(ISceneController controller)
    {
        FlushPendingPacketsAsync(controller as SceneController, CurrentToken).Forget();
    }

    private void EnqueuePacket(ResponsePacket packet)
    {
        _pendingPackets.Enqueue((packet.GetType(), packet));
    }

    private async UniTaskVoid FlushPendingPacketsAsync(SceneController controller, CancellationToken ct)
    {
        if (controller == null) return;

        try
        {
            await UniTask.NextFrame(ct);
        }
        catch (OperationCanceledException)
        {
            // 씬 재전환 시 새 LoadScene이 인터셉터를 재설정 → 여기선 미관여
            return;
        }

        // 로드 이후 도착하는 라이브 패킷도 컨트롤러로 라우팅하도록 인터셉터 유지
        ClientPacketRegistry.SetInterceptor(packet =>
            controller.ReceivePendingPacket(packet.GetType(), packet));

        while (_pendingPackets.TryDequeue(out var response))
        {
            controller.ReceivePendingPacket(response.type, response.packet);
        }

        if (_transition == null) return;

        try
        {
            await _transition.FadeInAsync().AttachExternalCancellation(ct);
        }
        catch (OperationCanceledException)
        {
            // 다음 LoadScene의 FadeOut이 DOKill로 정리
        }
    }
}
