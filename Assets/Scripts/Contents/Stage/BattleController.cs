using Cysharp.Threading.Tasks;
using SMDevLibrary.Command;
using SMDevLibrary.Network.Utility;
using SM.Contracts.TurnRPG;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private float _enemyTurnDelay = 1.5f;

    public event Action OnTileConfirmed;

    private BattleFieldView _battleFieldView;
    private bool _playerActed;
    private CancellationTokenSource _cts;
    private readonly CommandSequencer _sequencer = new CommandSequencer();

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

    private async UniTaskVoid RunTurnLoop(CancellationToken ct)
    {
        if (UnityNetworkBridge.Instance.IsConnected)
            await RunOnlineTurnLoop(ct);
        else
            await RunOfflineTurnLoop(ct);
    }

    /// <summary>
    /// 온라인: 서버가 매 턴 다음 행동 유닛을 결정합니다.
    /// ResponseNextTurn이 도착할 때까지 대기 후 다음 턴을 시작합니다.
    /// </summary>
    private async UniTask RunOnlineTurnLoop(CancellationToken ct)
    {
        string currentActorId = _firstActorId;

        while (!ct.IsCancellationRequested)
        {
            await UniTask.WaitUntil(
                () => UnitManager.Instance.EnemyCount > 0,
                cancellationToken: ct);

            var actor = UnitManager.Instance.ActivateTurnFor(currentActorId);
            if (actor == null)
            {
                await UniTask.NextFrame(ct);
                continue;
            }

            actor.OnTurnStart();

            if (actor.Team == EUnitTeam.Ally)
            {
                _playerActed = false;
                await UniTask.WaitUntil(() => _playerActed, cancellationToken: ct);
            }
            else
            {
                await RunEnemyTurn(actor, ct);
            }

            actor.OnTurnEnd();

            // 계약 BattleTurnEndRequestPacket은 BattleId만 운반(서버가 현재 턴 유닛 권위 보유).
            UnityNetworkBridge.Instance.SendPacket(new BattleTurnEndRequestPacket { BattleId = Client.Instance.ActiveBattleId });
            currentActorId = await WaitForNextActorIdAsync(ct);
            if (currentActorId == null) break;
        }
    }

    /// <summary>
    /// 오프라인: 로컬 TurnOrderCalculator가 매 턴 다음 행동 유닛을 결정합니다.
    /// </summary>
    private async UniTask RunOfflineTurnLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await UniTask.WaitUntil(
                () => UnitManager.Instance.EnemyCount > 0,
                cancellationToken: ct);

            var actor = UnitManager.Instance.GetNextActingUnit();
            if (actor == null)
            {
                await UniTask.NextFrame(ct);
                continue;
            }

            actor.OnTurnStart();

            if (actor.Team == EUnitTeam.Ally)
            {
                _playerActed = false;
                await UniTask.WaitUntil(() => _playerActed, cancellationToken: ct);
            }
            else
            {
                await RunEnemyTurn(actor, ct);
            }

            actor.OnTurnEnd();
        }
    }

    // ── 적 턴 ────────────────────────────────────────────────────────────────

    private async UniTask RunEnemyTurn(ITurnActor actor, CancellationToken ct)
    {
        // 스킬 유무와 무관하게 항상 딜레이를 적용합니다.
        // 스킬이 없어도 즉시 리턴하면 RequestTurnEnd가 연속 발송되는 문제를 방지합니다.
        await UniTask.WaitForSeconds(_enemyTurnDelay, cancellationToken: ct);

        if (actor is not EnemyUnitController unit) return;

        var skill = unit.SelectSkill();
        if (skill == null) return;

        var commands = new List<ICommand>
        {
            new PlayAnimationCommand(unit),
            new ApplySkillCommand(unit, skill),
        };

        await _sequencer.RunAsync(commands, ct);
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
