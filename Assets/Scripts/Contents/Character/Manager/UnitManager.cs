using SMDevLibrary.Generics;
using System;
using System.Collections.Generic;

public class UnitManager : LazySingleton<UnitManager>
{
    public event Action<ITurnActor> OnTurnStart;
    public event Action OnOrderChanged;
    public event Action OnEnemiesCleared;

    public TurnOrderCalculator TurnOrder { get; } = new TurnOrderCalculator();
    public BattleFieldManager BattleField { get; private set; }

    public int EnemyCount { get; private set; }

    private readonly List<IUnit> _units = new List<IUnit>();
    private readonly List<ITurnActor> _turnActorBuffer = new List<ITurnActor>();

    public void Initialize(TileGroup grid)
    {
        Clear();
        BattleField = new BattleFieldManager(grid);
    }

    public void Clear()
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] is ICombatant combatant)
                combatant.OnDied -= OnUnitDied;
        }

        _units.Clear();
        EnemyCount = 0;
        TurnOrder.Initialize(Array.Empty<ITurnActor>());
        BattleField = null;
    }

    public void AddUnit(IPlaceable unit, int tileIndex)
    {
        if (_units.Contains(unit)) return;

        if (BattleField.TryPlaceUnit(unit, tileIndex))
        {
            _units.Add(unit);

            if (unit.Team == EUnitTeam.Enemy) EnemyCount++;

            if (unit is ICombatant combatant)
                combatant.OnDied += OnUnitDied;

            RefreshTurnOrder();
        }
    }

    /// <summary>유닛을 다른 타일로 이동(서버 권위 BattleEnemyActionPush.MovedToTileIndex). 시각=BattleFieldView가 OnTileChanged로 갱신.</summary>
    public bool MoveUnit(IPlaceable unit, int toIndex)
        => BattleField != null && BattleField.Grid.TryMove(unit, toIndex);

    public void RemoveUnit(IPlaceable unit)
    {
        if (!_units.Contains(unit))
            return;

        if (unit is ICombatant combatant)
            combatant.OnDied -= OnUnitDied;

        if (unit.Team == EUnitTeam.Enemy)
        {
            EnemyCount--;
            if (EnemyCount == 0)
                OnEnemiesCleared?.Invoke();
        }

        _units.Remove(unit);
        BattleField.RemoveUnit(unit);
        RefreshTurnOrder();
    }

    private void OnUnitDied(ICombatant combatant)
    {
        if (combatant is IPlaceable placeable)
            RemoveUnit(placeable);
    }

    private void RefreshTurnOrder()
    {
        _turnActorBuffer.Clear();
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] is ITurnActor actor)
                _turnActorBuffer.Add(actor);
        }
        TurnOrder.Initialize(_turnActorBuffer);
        NotifyOrderChanged();
    }

    public ITurnActor GetNextActingUnit()
    {
        var unit = TurnOrder.GetNextUnit();
        if (unit != null) OnTurnStart?.Invoke(unit);

        NotifyOrderChanged();

        return unit;
    }

    /// <summary>서버가 지정한 unitId의 유닛을 찾아 턴 시작 이벤트를 발생시킵니다.</summary>
    public ITurnActor ActivateTurnFor(string unitId)
    {
        var unit = GetUnit(unitId) as ITurnActor;
        if (unit != null)
        {
            OnTurnStart?.Invoke(unit);
            NotifyOrderChanged();
        }
        return unit;
    }

    private void NotifyOrderChanged() => OnOrderChanged?.Invoke();

    public IUnit GetUnit(string unitId)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].UnitId == unitId) return _units[i];
        }

        return null;
    }

    public List<IUnit> GetAllies()
    {
        var result = new List<IUnit>();
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].Team == EUnitTeam.Ally)
                result.Add(_units[i]);
        }

        return result;
    }

    public List<IUnit> GetEnemies()
    {
        var result = new List<IUnit>();
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].Team == EUnitTeam.Enemy) result.Add(_units[i]);
        }
            
        return result;
    }
}
