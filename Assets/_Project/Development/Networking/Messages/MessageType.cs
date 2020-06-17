public enum MessageType : ushort
{
    // General
    None,

    // Lobby
    NewPlayer,
    Welcome,
    SetName,
    RequestDenied,
    PlayerLeft,
    StartGame,

    // Game
    PlayerTurn,
    RoomInfo,
    PlayerEnterRoom,
    PlayerLeaveRoom,
    ObtainTreasure,
    HitMonster,
    HitByMonster,
    PlayerDefends,
    PlayerLeftDungeon,
    PlayerDies,
    EndGame,
    MoveRequest,
    AttackRequest,
    DefendRequest,
    ClaimTreasureRequest,
    LeaveDungeonRequest,
}
