using Cysharp.Threading.Tasks;
using SMDevLibrary.Network.Utility;
using SM.Contracts.TurnRPG;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public event Action OnTileConfirmed;

    private BattleFieldView _battleFieldView;
    private bool _playerActed;
    private CancellationTokenSource _cts;

    private string _firstActorId;
    private UniTaskCompletionSource<string> _nextActorTcs;

    private void OnDestroy()
    {
        StopLoop();
        if (_battleFieldView != null)
            _battleFieldView.OnTileConfirmed -= HandleTileConfirmed;
    }

    public void SetBattleFieldView(BattleFieldView view)
    {
        if (_battleFieldView != null)
            _battleFieldView.OnTileConfirmed -= HandleTileConfirmed;

        _battleFieldView = view;

        if (_battleFieldView != null)
            _battleFieldView.OnTileConfirmed += HandleTileConfirmed;
    }

    private void HandleTileConfirmed() => OnTileConfirmed?.Invoke();

    public void StartLoop(string firstActorId)
    {
        _firstActorId = firstActorId;
        StopLoop();
        _cts = new CancellationTokenSource();
        RunTurnLoop(_cts.Token).Forget();
    }

    public void StopLoop()
    {
        _nextActorTcs?.TrySetCanceled();
        _nextActorTcs = null;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>BattleNextTurnResponsePacket 수신 시 InGameSceneController에서 호출합니다.</summary>
    public void NotifyNextActor(string nextUnitId)
    {
        _nextActorTcs?.TrySetResult(nextUnitId);
    }

    public void SignalPlayerActed() => _playerActed = true;

    public void PreviewSkillRange(SkillRangeData rangeData, ICombatant caster, SkillData skillData)
    {
        if (_battleFieldView != null)
            _battleFieldView.PreviewSkillRange(rangeData, caster, skillData);
        else
            Debug.LogWarning("BattleController: BattleFieldView가 설정되지 않았습니다.");
    }

    public void ClearSkillPreview()
    {
        if (_battleFieldView != null)
            _battleFieldView.ClearSkillPreview();
    }

    public List<string> GetConfirmedTargetIds()
        => _battleFieldView != null
            ? _battleFieldView.GetConfirmedTargetUnitIds()
            : new List<string>();

    // ── 턴 루프 ──────────────────────────────────────────────────────────────

    // INFO :: 오프라인 전투 폐지(2026-07-02) — 전투는 서버 권위 온라인 단일 모델.
    private async UniTaskVoid RunTurnLoop(CancellationToken ct)
    {
        if (!UnityNetworkBridge.Instance.IsConnected)
        {
            Debug.LogWarning("<color=#CE93D8>[Contents/BattleController]</color> :> 전투는 서버 연결 필요(오프라인 전투 폐지).");
            return;
        }

        await RunOnlineTurnLoop(ct);
    }

    /// <summary>
    /// 서버가 매 턴 다음 행동 유닛을 결정. ResponseNextTurn 도착까지 대기 후 다음 턴.
    /// </summary>
    private async UniTask RunOnlineTurnLoop(CancellationToken ct)
    {
        string currentActorId = _firstActorId;

        while (!ct.IsCancellationRequested)
        {
            await UniTask.WaitUntil(
                () => UnitManager.Instance.EnemyCount > 0,
                cancellationToken: ct);

            var actor = UnitManager.Instance.GetUnit(currentActorId) as ITurnActor;

            // 아군 턴만 클라가 능동 처리(입력 → SkillUse/Move → TurnEnd).
            // 적 턴 = 완전 서버 주도(적 행동/이동/진행=BattleEnemyActionPush). 클라 입력·TurnEnd 없음 →
            // 적 페이즈는 여기서 아무것도 안 하고 WaitForNextActorIdAsync에서 대기(적 푸시가 종료 시 아군 actor 통지).
            if (actor != null && actor.Team == EUnitTeam.Ally)
            {
                UnitManager.Instance.ActivateTurnFor(currentActorId);
                actor.OnTurnStart();

                _playerActed = false;
                await UniTask.WaitUntil(() => _playerActed, cancellationToken: ct);

                actor.OnTurnEnd();

                // 계약 BattleTurnEndRequestPacket은 BattleId만 운반(서버가 현재 턴 유닛 권위 보유).
                UnityNetworkBridge.Instance.SendPacket(new BattleTurnEndRequestPacket { BattleId = Client.Instance.ActiveBattleId });
            }

            currentActorId = await WaitForNextActorIdAsync(ct);
            if (currentActorId == null) break;
        }
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    private async UniTask<string> WaitForNextActorIdAsync(CancellationToken ct)
    {
        _nextActorTcs = new UniTaskCompletionSource<string>();
        var reg = ct.Register(() => _nextActorTcs.TrySetCanceled());
        try
        {
            return await _nextActorTcs.Task;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            reg.Dispose();
        }
    }
}
