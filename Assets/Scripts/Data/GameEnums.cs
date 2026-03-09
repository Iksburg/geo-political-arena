namespace Data
{
    public enum GamePhase
    {
        GameStart,
        DrawPhase,
        ResourcePhase,
        MainPhase,
        CombatPhase,
        EndPhase,
        GameOver
    }

    public enum TurnAction
    {
        None,
        PlayedResource,
        PlayedUnits
    }
}