using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Assertions;

using Random = Unity.Mathematics.Random;

[RequireComponent(typeof(Server))]
public class CaveQuestManager : MonoBehaviour
{
    public const ushort enemyAttack = 2;
    
    #region Components
    private Server _server;
    public Server server => _server ? _server : (_server = GetComponent<Server>());
    #endregion

    [SerializeField] private GridSettings gridSettings;

    private Room[,] rooms;

    private NativeList<NetworkConnection> connections => server.connections;

    #region Player Information
    private NativeList<NetworkConnection> players;
    private NetworkConnection currentPlayer => (currentPlayerIndex >= 0 && currentPlayerIndex < players.Length) ? players[currentPlayerIndex] : default;
    public int currentPlayerID => currentPlayer.InternalId;
    private int _currentPlayerIndex;
    private int currentPlayerIndex
    {
        get => _currentPlayerIndex;
        set
        {
            var playersLength = players.Length;
            if(playersLength != 0)
            {
                _currentPlayerIndex = (value + playersLength) % playersLength;
            }
        }
    }

    private NativeHashMap<int, int2> playerPositions;
    private NativeHashMap<int, ushort> healthPerPlayer;
    private NativeHashMap<int, bool> defending;
    private NativeHashMap<int, ushort> coinPerPlayer;

    private NetworkConnection[] GetPlayersInRoom(int2 position)
    {
        var playersInRoom = new NativeList<NetworkConnection>(Allocator.Temp);

        for(int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            Assert.IsTrue(player.IsCreated);

            if(playerPositions[player.InternalId].Equals(position))
            {
                playersInRoom.Add(player);
            }
        }

        var array = playersInRoom.ToArray();
        playersInRoom.Dispose();

        return array;
    }
    #endregion

    // Generates a grid for the game and populates it with monsters and treasure.
    public void GenerateGrid(GridSettings gridSettings, uint seed = 1) => GenerateGrid(gridSettings, new Random(seed));
    public void GenerateGrid(GridSettings gridSettings, Random random)
    {
        server.readMessage = ReadCaveQuestMessages;
        server.onConnectionDisconnected += PlayerDisconnected;

        this.gridSettings = gridSettings;

        var size = gridSettings.size;
        rooms = new Room[size.x, size.y];

        // Place the exit.
        var exitIndex = random.NextInt2(size);
        rooms[exitIndex.x, exitIndex.y].exit = true;

        // Get an array of all open tiles.
        var range = new NativeArray<int2>(rooms.Length - 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                if(x == exitIndex.x && y == exitIndex.y)
                {
                    continue;
                }

                var i = y * size.x + x;
                if(y > exitIndex.y || (y == exitIndex.y && x > exitIndex.x))
                {
                    i--;
                }
                range[i] = new int2(x, y);
            }
        }
        var rangeCopy = new NativeList<int2>(Allocator.Temp);

        // Place the treasures on distinct tiles without the exit O(n) style.
        rangeCopy.AddRange(range);
        for(int number = 0; number < gridSettings.treasures && rangeCopy.Length > 0; number++)
        {
            var rangeIndex = random.NextInt(rangeCopy.Length);
            var treasureIndex = rangeCopy[rangeIndex];
            rooms[treasureIndex.x, treasureIndex.y].treasure = (ushort)(random.NextUInt(gridSettings.minCoinsPerTreasure, gridSettings.maxCoinsPerTreasure) * gridSettings.coinWorth);
            rangeCopy.RemoveAtSwapBack(rangeIndex);
        }
        rangeCopy.ResizeUninitialized(0);

        // Place the enemies on distinct tiles without the exit O(n) style.
        rangeCopy.AddRange(range);
        for(int number = 0; number < gridSettings.monsters && rangeCopy.Length > 0; number++)
        {
            var rangeIndex = random.NextInt(rangeCopy.Length);
            var enemyIndex = rangeCopy[rangeIndex];
            rooms[enemyIndex.x, enemyIndex.y].monsterHealth = (ushort)random.NextUInt(gridSettings.minMonsterHealth, gridSettings.maxMonsterHealth);
            rangeCopy.RemoveAtSwapBack(rangeIndex);
        }
        rangeCopy.Dispose();

        range.Dispose();

        PreparePlayers(random);
        SendPlayerTurnMessage();
    }

    private void PreparePlayers(Random random)
    {
        if(players.IsCreated)
        {
            players.Dispose();
        }

        players = new NativeList<NetworkConnection>(server.connectionsLength, Allocator.Persistent);
        players.AddRangeNoResize(server.connections);

        currentPlayerIndex = random.NextInt(players.Length);

        // Create NativeHashMaps to store player data.
        // TODO: put the data into a single struct.
        if(playerPositions.IsCreated)
        {
            playerPositions.Dispose();
        }
        if(healthPerPlayer.IsCreated)
        {
            healthPerPlayer.Dispose();
        }
        if(defending.IsCreated)
        {
            defending.Dispose();
        }
        if(coinPerPlayer.IsCreated)
        {
            coinPerPlayer.Dispose();
        }
        playerPositions = new NativeHashMap<int, int2>(players.Length, Allocator.Persistent);
        healthPerPlayer = new NativeHashMap<int, ushort>(players.Length, Allocator.Persistent);
        defending = new NativeHashMap<int, bool>(players.Length, Allocator.Persistent);
        coinPerPlayer = new NativeHashMap<int, ushort>(players.Length, Allocator.Persistent);

        // Place the players.
        for(int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            Assert.IsTrue(player.IsCreated);

            var randomPosition = random.NextInt2(gridSettings.size);
            playerPositions.Add(player.InternalId, randomPosition);
            healthPerPlayer.Add(player.InternalId, CaveQuestPlayer.maxHealth);
            defending.Add(player.InternalId, true);
            coinPerPlayer.Add(player.InternalId, 0);
        }

        // Send each player information about the room they're in.
        var roomInfo = new RoomInfo(MessageID.nextID);
        for(int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            Assert.IsTrue(player.IsCreated);

            GetRoomInfo(ref roomInfo, player.InternalId);

            server.Send(player, roomInfo);
        }
    }

    // Get the RoomInfo about the room a player is in.
    private RoomInfo GetRoomInfo(int playerID)
    {
        var roomInfo = new RoomInfo(MessageID.nextID);
        GetRoomInfo(ref roomInfo, playerID);
        return roomInfo;
    }
    private void GetRoomInfo(ref RoomInfo roomInfo, int playerID)
    {
        var playerPosition = playerPositions[playerID];

        var room = rooms[playerPosition.x, playerPosition.y];
        roomInfo.moveDirections = Directions(playerPosition);
        roomInfo.treasureInRoom = room.treasure;
        roomInfo.containsMonster = room.monster;
        roomInfo.containsExit = room.exit;

        // Find the other players in the room.
        // TODO: use GetPlayersInRoom.
        var otherPlayerIDs = new NativeList<int>(Allocator.Temp);
        for(int o = 0; o < players.Length; o++)
        {
            var otherPlayer = players[o];
            Assert.IsTrue(otherPlayer.IsCreated);

            if(otherPlayer.InternalId == playerID)
            {
                continue;
            }

            var otherPlayerPosition = playerPositions[otherPlayer.InternalId];
            if(playerPosition.Equals(otherPlayerPosition))
            {
                otherPlayerIDs.Add(otherPlayer.InternalId);
            }
        }
        roomInfo.numberOfOtherPlayers = (byte)otherPlayerIDs.Length;
        roomInfo.otherPlayerIDs = otherPlayerIDs.ToArray();
        otherPlayerIDs.Dispose();
    }

    // Get Directions from a room.
    private Direction Directions(int2 position) => Directions(position.x, position.y);
    private Direction Directions(int x, int y)
    {
        Direction directions = 0;

        if(y < rooms.GetLength(1) - 1)
        {
            directions |= Direction.North;
        }
        if(x < rooms.GetLength(0) - 1)
        {
            directions |= Direction.East;
        }
        if(y > 0)
        {
            directions |= Direction.South;
        }
        if(x > 0)
        {
            directions |= Direction.West;
        }

        return directions;
    }

    private void AdvanceTurn()
    {
        currentPlayerIndex++;

        // Handle enemy attacking by sending an attack message and (possibly) killing the player.
        var position = playerPositions[currentPlayerID];
        if(rooms[position.x, position.y].monster)
        {
            var newHealth = healthPerPlayer[currentPlayerID] -= (defending[currentPlayerID] ? (ushort)0 : enemyAttack);

            var hitByMonsterMessage = new AttackMessage(MessageID.nextID, MessageType.HitByMonster, currentPlayerID, newHealth);
            for(int i = 0; i < connections.Length; i++)
            {
                var connection = connections[i];
                server.Send(connection, hitByMonsterMessage);
            }

            if(newHealth <= 0)
            {
                RemovePlayer(currentPlayerID);

                var playerDiedMessage = new PlayerMessage(MessageID.nextID, MessageType.PlayerDies, currentPlayerID);
                for(int i = 0; i < connections.Length; i++)
                {
                    var connection = connections[i];
                    server.Send(connection, playerDiedMessage);
                }
            }
        }
        defending[currentPlayerID] = false;

        SendPlayerTurnMessage();
    }

    private void SendPlayerTurnMessage()
    {
        var playerTurnMessage = new PlayerMessage(MessageID.nextID, MessageType.PlayerTurn, currentPlayerID);
        for(int i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            server.Send(connection, playerTurnMessage);
        }
    }

    private INetworkMessage ReadCaveQuestMessages(NetworkConnection player, DataStreamReader reader, Message message)
    {
        var currentConnectionID = player.InternalId;

        switch(message.messageType)
        {
            case MessageType.None:
                server.Send(player, new Message(0, MessageType.None));
                return null;
            case MessageType.MoveRequest:
                var moveRequest = new MoveRequest(message.id);
                moveRequest.Deserialize(ref reader);

                // Calculate new position.
                var newPosition = playerPositions[currentConnectionID];
                switch(moveRequest.direction)
                {
                    case Direction.North:
                        newPosition += new int2(0, 1);
                        break;
                    case Direction.East:
                        newPosition += new int2(1, 0);
                        break;
                    case Direction.South:
                        newPosition += new int2(0, -1);
                        break;
                    case Direction.West:
                        newPosition += new int2(-1, 0);
                        break;
                }

                // Deny if new position is invalid.
                if(newPosition.x < 0 || newPosition.x >= rooms.GetLength(0) || newPosition.y < 0 || newPosition.y >= rooms.GetLength(1))
                {
                    var requestDenied = new RequestDeniedMessage(MessageID.nextID, moveRequest.id);
                    server.Send(player, requestDenied);
                    return moveRequest;
                }

                // Move Player.
                var oldPosition = playerPositions[currentConnectionID];
                var playersInNewRoom = GetPlayersInRoom(newPosition);
                playerPositions[currentConnectionID] = newPosition;

                // Send Player Left Message.
                var playerLeftRoom = new PlayerMessage(MessageID.nextID, MessageType.PlayerLeaveRoom, currentConnectionID);
                var playersInOldRoom = GetPlayersInRoom(oldPosition);
                for(int i = 0; i < playersInOldRoom.Length; i++)
                {
                    var otherPlayer = playersInOldRoom[i];
                    server.Send(otherPlayer, playerLeftRoom);
                }

                // Send Room Message.
                var roomInfo = GetRoomInfo(currentConnectionID);
                server.Send(player, roomInfo);

                // Send Player Entered Message.
                var playerEnteredRoom = new PlayerMessage(MessageID.nextID, MessageType.PlayerEnterRoom, currentConnectionID);
                for(int i = 0; i < playersInNewRoom.Length; i++)
                {
                    var otherPlayer = playersInNewRoom[i];
                    server.Send(otherPlayer, playerEnteredRoom);
                }

                AdvanceTurn();
                return moveRequest;
            case MessageType.AttackRequest:
                var attackRequest = new ActionRequest(message.id, message.messageType);
                attackRequest.Deserialize(ref reader);

                var position = playerPositions[currentConnectionID];

                // Deny if room does not have a monster.
                if(!rooms[position.x, position.y].monster)
                {
                    var requestDenied = new RequestDeniedMessage(MessageID.nextID, attackRequest.id);
                    server.Send(player, requestDenied);
                    return attackRequest;
                }

                // Attack the monster.
                rooms[position.x, position.y].monsterHealth -= CaveQuestPlayer.attackDamage;

                var hitMonster = new AttackMessage(MessageID.nextID, MessageType.HitMonster, currentConnectionID, CaveQuestPlayer.attackDamage);
                for(int i = 0; i < connections.Length; i++)
                {
                    var otherConnection = connections[i];
                    server.Send(otherConnection, hitMonster);
                }

                AdvanceTurn();
                return attackRequest;
            case MessageType.DefendRequest:
                var defendRequest = new ActionRequest(message.id, message.messageType);
                defendRequest.Deserialize(ref reader);

                // Deny if room has no monster.
                position = playerPositions[currentConnectionID];
                if(!rooms[position.x, position.y].monster)
                {
                    var requestDenied = new RequestDeniedMessage(MessageID.nextID, defendRequest.id);
                    server.Send(player, requestDenied);
                    return defendRequest;
                }

                // Heal and defend until the players next turn.
                defending[currentConnectionID] = true;
                var newHealth = healthPerPlayer[currentConnectionID] = (ushort)math.min(healthPerPlayer[currentConnectionID] + CaveQuestPlayer.healPower, CaveQuestPlayer.maxHealth);

                var defendMessage = new AttackMessage(MessageID.nextID, MessageType.PlayerDefends, currentConnectionID, newHealth);
                for(int i = 0; i < players.Length; i++)
                {
                    var otherPlayer = players[i];
                    if(otherPlayer.InternalId == currentConnectionID)
                    {
                        continue;
                    }

                    server.Send(otherPlayer, defendMessage);
                }

                AdvanceTurn();
                return defendRequest;
            case MessageType.ClaimTreasureRequest:
                var claimTreasureRequest = new ActionRequest(message.id, message.messageType);
                claimTreasureRequest.Deserialize(ref reader);

                position = playerPositions[currentConnectionID];

                // Deny if room has no treasure.
                if(rooms[position.x, position.y].treasure <= 0)
                {
                    var requestDenied = new RequestDeniedMessage(MessageID.nextID, claimTreasureRequest.id);
                    server.Send(player, requestDenied);
                    return claimTreasureRequest;
                }

                // Get all the players in the room to divide the treasure between.
                var playersInRoom = GetPlayersInRoom(position);
                var room = rooms[position.x, position.y];
                var treasure = (ushort)(room.treasure / playersInRoom.Length);

                // Send ObtainTreasure message.
                var obtainTreasure = new ObtainTreasure(MessageID.nextID, treasure);
                for(int i = 0; i < playersInRoom.Length; i++)
                {
                    var playerInRoom = playersInRoom[i];
                    coinPerPlayer[playerInRoom.InternalId] += treasure;
                    server.Send(playerInRoom, obtainTreasure);
                }
                rooms[position.x, position.y].treasure = 0;

                AdvanceTurn();
                return claimTreasureRequest;
            case MessageType.LeaveDungeonRequest:
                var leaveDungeonRequest = new ActionRequest(message.id, message.messageType);
                leaveDungeonRequest.Deserialize(ref reader);

                // Deny if room has no exit.
                position = playerPositions[currentConnectionID];
                if(!rooms[position.x, position.y].exit)
                {
                    var requestDenied = new RequestDeniedMessage(MessageID.nextID, leaveDungeonRequest.id);
                    server.Send(player, requestDenied);
                    return leaveDungeonRequest;
                }

                // Remove the player and notify the other players.
                RemovePlayer(currentConnectionID);

                var playerLeftDungeonMessage = new PlayerMessage(MessageID.nextID, MessageType.PlayerLeftDungeon, currentConnectionID);
                for(int i = connections.Length - 1; i >= 0; i--)
                {
                    var connection = connections[i];
                    server.Send(connection, playerLeftDungeonMessage);
                }

                // Send the highscore and end the game if there are no more players left.
                if(players.Length == 0)
                {
                    DoAsyncStuff();

                    async void DoAsyncStuff()
                    {
                        for(int i = 0; i < connections.Length; i++)
                        {
                            var connection = connections[i];
                            var playerInfo = server.playerInfoByID[connection.InternalId];
                            var score = coinPerPlayer[connection.InternalId];
                            await DatabaseHelper.InsertScore(playerInfo.name, score);
                        }

                        var highScores = new KeyValuePair<int, ushort>[connections.Length];
                        for(int i = 0; i < highScores.Length; i++)
                        {
                            var connectionID = connections[i].InternalId;
                            var coin = coinPerPlayer[connectionID];

                            highScores[i] = new KeyValuePair<int, ushort>(connectionID, coin);
                        }

                        var endGameMessage = new EndGameMessage(MessageID.nextID, (byte)connections.Length, highScores);
                        for(int i = 0; i < connections.Length; i++)
                        {
                            var connection = connections[i];
                            server.Send(connection, endGameMessage);
                        }

                        Destroy(this);
                    }
                }
                else
                {
                    AdvanceTurn();
                }
                return leaveDungeonRequest;
            default:
                return null;
        }
    }

    // Remove a player and all its data (except their score) from the game.
    private void RemovePlayer(int playerID)
    {
        for(int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if(player.InternalId == playerID)
            {
                players.RemoveAt(i);
                playerPositions.Remove(playerID);
                healthPerPlayer.Remove(playerID);
                defending.Remove(playerID);
                break;
            }
        }
        currentPlayerIndex--;
    }

    // Disconnect a player from the game, removing them completely including their score.
    private void PlayerDisconnected(NetworkConnection connection)
    {
        var currentPlayerID = this.currentPlayerID;

        RemovePlayer(connection.InternalId);
        coinPerPlayer.Remove(connection.InternalId);

        if(connection.InternalId == currentPlayerID)
        {
            AdvanceTurn();
        }
        else
        {
            currentPlayerIndex++;
        }
    }

    // Safely dispose of all thing networking.
    private void OnDestroy()
    {
        if(players.IsCreated)
        {
            players.Dispose();
        }
        if(playerPositions.IsCreated)
        {
            playerPositions.Dispose();
        }
        if(healthPerPlayer.IsCreated)
        {
            healthPerPlayer.Dispose();
        }
        if(defending.IsCreated)
        {
            defending.Dispose();
        }
        if(coinPerPlayer.IsCreated)
        {
            coinPerPlayer.Dispose();
        }

        if(server.driver.IsCreated)
        {
            server.readMessage = server.ReadJoingingAreaMessages;
        }
        server.onConnectionDisconnected -= PlayerDisconnected;
    }
}
