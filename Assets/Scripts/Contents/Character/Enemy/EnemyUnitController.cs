public class EnemyUnitController : UnitController
{
    public override EUnitTeam Team => EUnitTeam.Enemy;

    public override void OnTurnStart()
    {
        base.OnTurnStart();
    }
}