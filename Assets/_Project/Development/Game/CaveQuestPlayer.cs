using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Client))]
public class CaveQuestPlayer : MonoBehaviour
{
    public const ushort maxHealth = 10;
    public const ushort healPower = 1;
    public const ushort attackDamage = 1;

    #region Components
    private Client _client;
    private Client client => _client ? _client : (_client = GetComponent<Client>());
    #endregion

    #region UI
    private GameObject controlsPanel;
    private GameObject waitPanel;
    private TMP_Text up;
    private TMP_Text right;
    private TMP_Text down;
    private TMP_Text left;
    private TMP_Text primary;
    private TMP_Text secondary;
    private TMP_Text healthDisplay;
    private TMP_Text coinDisplay;

    private void UpdateUI(bool turn)
    {
        controlsPanel.SetActive(turn);
        waitPanel.SetActive(!turn);

        if(turn)
        {
            up.gameObject.SetActive(canMoveNorth);
            right.gameObject.SetActive(canMoveEast);
            down.gameObject.SetActive(CanMoveSouth);
            left.gameObject.SetActive(canMoveWest);

            primary.gameObject.SetActive(canPrimary);
            secondary.gameObject.SetActive(canSecondary);
        }
    }
    #endregion

    public int playerID => client.playerID;

    private bool turn = false;
    private RoomInfo currentRoomInfo;

    private int playerDrawCall;

    public void Initialize(ushort startHP)
    {
        // Get UI before turning off the controlsPanel!
        controlsPanel = GameObject.Find("Controls Panel");
        waitPanel = GameObject.Find("Wait Panel");
        up = GameObject.Find("Up").GetComponent<TMP_Text>();
        right = GameObject.Find("Right").GetComponent<TMP_Text>();
        down = GameObject.Find("Down").GetComponent<TMP_Text>();
        left = GameObject.Find("Left").GetComponent<TMP_Text>();
        primary = GameObject.Find("Primary").GetComponent<TMP_Text>();
        secondary = GameObject.Find("Secondary").GetComponent<TMP_Text>();
        healthDisplay = GameObject.Find("Health Display").GetComponent<TMP_Text>();
        coinDisplay = GameObject.Find("Coin Display").GetComponent<TMP_Text>();

        // Set start values.
        controlsPanel.SetActive(false);
        waitPanel.SetActive(true);

        healthDisplay.text = math.min((uint)startHP, maxHealth).ToString();
        coinDisplay.text = "0";
        client.readMessage = ReadCaveQuestMessage;
    }

    private INetworkMessage ReadCaveQuestMessage(DataStreamReader reader, Message message)
    {
        switch(message.messageType)
        {
            case MessageType.PlayerTurn:
                var playerTurnMessage = new PlayerMessage(message.id, message.messageType);
                playerTurnMessage.Deserialize(ref reader);

                turn = playerTurnMessage.playerID == playerID;

                UpdateUI(turn);

                return playerTurnMessage;
            case MessageType.RoomInfo:
                var roomInfo = new RoomInfo(message.id);
                roomInfo.Deserialize(ref reader);

                currentRoomInfo = roomInfo;
                StartCoroutine(DrawRoom(roomInfo));

                return roomInfo;
            case MessageType.PlayerEnterRoom:
                var playerEnterRoom = new PlayerMessage(message.id, message.messageType);
                playerEnterRoom.Deserialize(ref reader);

                foreach(var textMesh in FindObjectsOfType<TextMesh>())
                {
                    if(textMesh.text == playerEnterRoom.playerID.ToString())
                    {
                        Destroy(textMesh.transform.parent.gameObject);
                        break;
                    }
                }

                currentRoomInfo.numberOfOtherPlayers++;
                var otherPlayerIDs = new NativeArray<int>(currentRoomInfo.numberOfOtherPlayers, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for(int i = 0; i < currentRoomInfo.otherPlayerIDs.Length; i++)
                {
                    var otherPlayerID = currentRoomInfo.otherPlayerIDs[i];
                    otherPlayerIDs[i] = otherPlayerID;
                }
                otherPlayerIDs[currentRoomInfo.numberOfOtherPlayers - 1] = playerEnterRoom.playerID;
                currentRoomInfo.otherPlayerIDs = otherPlayerIDs.ToArray();
                otherPlayerIDs.Dispose();

                DrawPlayer(playerEnterRoom.playerID);

                return playerEnterRoom;
            case MessageType.PlayerLeaveRoom:
                var playerLeaveRoom = new PlayerMessage(message.id, message.messageType);
                playerLeaveRoom.Deserialize(ref reader);

                foreach(var textMesh in FindObjectsOfType<TextMesh>())
                {
                    if(textMesh.text == playerLeaveRoom.playerID.ToString())
                    {
                        Destroy(textMesh.transform.parent.gameObject);
                        break;
                    }
                }

                currentRoomInfo.numberOfOtherPlayers--;
                otherPlayerIDs = new NativeArray<int>(currentRoomInfo.numberOfOtherPlayers, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                bool tempBool = false;
                for(int i = 0; i < currentRoomInfo.otherPlayerIDs.Length; i++)
                {
                    var otherPlayerID = currentRoomInfo.otherPlayerIDs[i];
                    if(otherPlayerID == playerLeaveRoom.playerID)
                    {
                        tempBool = true;
                        continue;
                    }

                    var i2 = (tempBool) ? i - 1 : i;
                    otherPlayerIDs[i2] = otherPlayerID;
                }
                currentRoomInfo.otherPlayerIDs = otherPlayerIDs.ToArray();
                otherPlayerIDs.Dispose();

                return playerLeaveRoom;
            case MessageType.ObtainTreasure:
                var obtainTreasureMessage = new ObtainTreasure(message.id);
                obtainTreasureMessage.Deserialize(ref reader);

                coinDisplay.text = (int.Parse(coinDisplay.text) + obtainTreasureMessage.amount).ToString();
                currentRoomInfo.treasureInRoom = 0;

                foreach(var hit in Physics2D.BoxCastAll(Vector2.zero, Vector2.one, 0, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
                {
                    if(hit.transform.name == "Treasure(Clone)")
                    {
                        Destroy(hit.transform.gameObject);
                        break;
                    }
                }

                return obtainTreasureMessage;
            case MessageType.HitMonster:
                var hitMonsterMessage = new AttackMessage(message.id, message.messageType);
                hitMonsterMessage.Deserialize(ref reader);

                if(hitMonsterMessage.playerID == playerID || currentRoomInfo.otherPlayerIDs.Contains(hitMonsterMessage.playerID))
                {
                    if(!(currentRoomInfo.containsMonster = false))
                    {
                        foreach(var hit in Physics2D.BoxCastAll(Vector2.zero, Vector2.one, 0, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
                        {
                            if(hit.transform.name == "Monster(Clone)")
                            {
                                Destroy(hit.transform.gameObject);
                                break;
                            }
                        }
                    }
                }

                return hitMonsterMessage;
            case MessageType.HitByMonster:
                var hitByMonsterMessage = new AttackMessage(message.id, message.messageType);
                hitByMonsterMessage.Deserialize(ref reader);

                if(playerID == hitByMonsterMessage.playerID)
                {
                    healthDisplay.text = hitByMonsterMessage.value.ToString();
                }

                return hitByMonsterMessage;
            case MessageType.PlayerDefends:
                var playerDefendMessage = new AttackMessage(message.id, message.messageType);
                playerDefendMessage.Deserialize(ref reader);

                return playerDefendMessage;
            case MessageType.PlayerLeftDungeon:
                var playerLeftDungeon = new PlayerMessage(message.id, message.messageType);
                playerLeftDungeon.Deserialize(ref reader);

                foreach(var textMesh in FindObjectsOfType<TextMesh>())
                {
                    if(textMesh.text == playerLeftDungeon.playerID.ToString())
                    {
                        Destroy(textMesh.transform.parent.gameObject);
                        break;
                    }
                }

                return playerLeftDungeon;
            case MessageType.PlayerDies:
                var playerDiesMessage = new PlayerMessage(message.id, message.messageType);
                playerDiesMessage.Deserialize(ref reader);

                return playerDiesMessage;
            case MessageType.EndGame:
                var endGameMessage = new EndGameMessage(message.id);
                endGameMessage.Deserialize(ref reader);

                SceneManager.LoadSceneAsync("Ending Screen").completed += (asyncOperation) =>
                {
                    foreach(var highScore in endGameMessage.highScores.OrderByDescending(pair => pair.Value))
                    {
                        var endingScreenUIBinder = FindObjectOfType<EndingScreenUIBinder>();
                        endingScreenUIBinder.SpawnHighScoreUIField(highScore.Key, highScore.Value);
                    }
                };
                Destroy(this);

                return endGameMessage;
            case MessageType.RequestDenied:
                var requestDeniedMessage = new RequestDeniedMessage(message.id);
                requestDeniedMessage.Deserialize(ref reader);

                return requestDeniedMessage;
            case MessageType.PlayerLeft:
                var playerLeftMessage = new PlayerMessage(message.id, message.messageType);
                playerLeftMessage.Deserialize(ref reader);

                foreach(var textMesh in FindObjectsOfType<TextMesh>())
                {
                    if(textMesh.text == playerLeftMessage.playerID.ToString())
                    {
                        Destroy(textMesh.transform.parent.gameObject);
                        break;
                    }
                }

                return playerLeftMessage;
            default:
                return null;
        }
    }

    #region Actions
    private bool canMove => !currentRoomInfo.containsMonster;
    private bool CanMoveTo(Direction direction) => canMove && currentRoomInfo.moveDirections.HasFlag(direction);
    private bool canMoveNorth => CanMoveTo(Direction.North);
    private bool canMoveEast => CanMoveTo(Direction.East);
    private bool CanMoveSouth => CanMoveTo(Direction.South);
    private bool canMoveWest => CanMoveTo(Direction.West);
    private bool canPrimary => currentRoomInfo.containsMonster || currentRoomInfo.treasureInRoom > 0 || currentRoomInfo.containsExit;
    private bool canSecondary => currentRoomInfo.containsMonster;

    private void Update()
    {
        if(turn)
        {
            if(Input.GetKeyUp(KeyCode.W) && canMoveNorth)
            {
                Move(Direction.North);
            }
            else if(Input.GetKeyUp(KeyCode.D) && canMoveEast)
            {
                Move(Direction.East);
            }
            else if(Input.GetKeyUp(KeyCode.S) && CanMoveSouth)
            {
                Move(Direction.South);
            }
            else if(Input.GetKeyUp(KeyCode.A) && canMoveWest)
            {
                Move(Direction.West);
            }
            else if(Input.GetKeyUp(KeyCode.Z) && canPrimary)
            {
                Primary();
            }
            else if(Input.GetKeyUp(KeyCode.X) && canSecondary)
            {
                Secondary();
            }
        }
    }

    // Perform a move action.
    private void Move(Direction direction)
    {
        // Get the movement.
        var spriteTranslation = Vector2.zero;
        switch(direction)
        {
            case Direction.North:
                spriteTranslation = Vector2.down;
                break;
            case Direction.East:
                spriteTranslation = Vector2.left;
                break;
            case Direction.South:
                spriteTranslation = Vector2.up;
                break;
            case Direction.West:
                spriteTranslation = Vector2.right;
                break;
        }

        // Move all sprites except for the player 1 unit in the opposite direction. This will make it seem like the player moved to the left, while actually the 'world' moved to the right. And yes I am aware how bad this code is but it saves time on camera stuff and it didn't seem all that consequential in the grander scheme of this project.
        foreach(var spriteRenderer in FindObjectsOfType<SpriteRenderer>())
        {
            if(spriteRenderer.GetComponentInChildren<TextMesh>() && spriteRenderer.GetComponentInChildren<TextMesh>().text == playerID.ToString())
            {
                continue;
            }

            spriteRenderer.transform.Translate(spriteTranslation);
            spriteRenderer.color = Color.gray;
        }

        // Send the moveRequest.
        var moveRequest = new MoveRequest(MessageID.nextID, direction);
        client.Send(moveRequest);

        turn = false;
    }

    // Perform a primary action, which can be attacking, picking up treasure, or leaving, depending on the currentRoom information.
    private void Primary()
    {
        MessageType actionRequestType = MessageType.None;
        if(currentRoomInfo.containsMonster)
        {
            actionRequestType = MessageType.AttackRequest;
        }
        else if(currentRoomInfo.treasureInRoom > 0)
        {
            actionRequestType = MessageType.ClaimTreasureRequest;
        }
        else if(currentRoomInfo.containsExit)
        {
            actionRequestType = MessageType.LeaveDungeonRequest;
        }

        var primaryRequest = new ActionRequest(MessageID.nextID, actionRequestType);
        client.Send(primaryRequest);

        turn = false;
    }

    // Perform a secondary action, which can be defending.
    private void Secondary()
    {
        var defendRequest = new ActionRequest(MessageID.nextID, MessageType.DefendRequest);
        client.Send(defendRequest);
        
        turn = false;
    }
    #endregion

    #region Drawing
    // Draw the room after 1 FixedUpdate. We are using more bad code here to get rid of everything in the center using raycasts and we have to wait 1 FixedUpdate until everything has moved (from our previous move action) before we do it, else our raycast might pick up the wrong stuff.
    private IEnumerator DrawRoom(RoomInfo roomInfo)
    {
        yield return new WaitForFixedUpdate();

        // Destroy everything within (-0.5, -0.5) to (0.5, 0.5). This is where we will draw the new room.
        foreach(var hit in Physics2D.BoxCastAll(Vector2.zero, Vector2.one, 0, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
        {
            Destroy(hit.transform.gameObject);
        }

        // Just load everything from Resources, it's quick, it works, it's aight. 
        var groundTile = Resources.Load("GroundTile");
        Instantiate(groundTile);

        // Draw a room up north if there is a room up north. Do the same for every other direction.
        if(roomInfo.moveDirections.HasFlag(Direction.North))
        {
            if(!Physics2D.Raycast(Vector2.up, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
            {
                var groundTileGO = (GameObject)Instantiate(groundTile, Vector2.up, Quaternion.identity);
                groundTileGO.GetComponent<SpriteRenderer>().color = Color.gray;
            }
        }
        if(roomInfo.moveDirections.HasFlag(Direction.East))
        {
            if(!Physics2D.Raycast(Vector2.right, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
            {
                var groundTileGO = (GameObject)Instantiate(groundTile, Vector2.right, Quaternion.identity);
                groundTileGO.GetComponent<SpriteRenderer>().color = Color.gray;
            }
        }
        if(roomInfo.moveDirections.HasFlag(Direction.South))
        {
            if(!Physics2D.Raycast(Vector2.down, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
            {
                var groundTileGO = (GameObject)Instantiate(groundTile, Vector2.down, Quaternion.identity);
                groundTileGO.GetComponent<SpriteRenderer>().color = Color.gray;
            }
        }
        if(roomInfo.moveDirections.HasFlag(Direction.West))
        {
            if(!Physics2D.Raycast(Vector2.left, Vector2.zero, float.PositiveInfinity, LayerMask.GetMask("Raycast Target")))
            {
                var groundTileGO = (GameObject)Instantiate(groundTile, Vector2.left, Quaternion.identity);
                groundTileGO.GetComponent<SpriteRenderer>().color = Color.gray;
            }
        }

        // Draw the exit, monster or treasure if those things are present.
        if(roomInfo.containsExit)
        {
            var exit = Resources.Load("Exit");
            Instantiate(exit);
        }
        else
        {
            if(roomInfo.containsMonster)
            {
                var monster = Resources.Load("Monster");
                Instantiate(monster);
            }
            if(roomInfo.treasureInRoom > 0)
            {
                var treasure = Resources.Load("Treasure");
                Instantiate(treasure);
            }
        }

        // Draw the players occupying the room.
        playerDrawCall = 0;

        DrawPlayer(playerID);
        for(int i = 0; i < roomInfo.numberOfOtherPlayers; i++)
        {
            DrawPlayer(roomInfo.otherPlayerIDs[i]);
        }
    }

    private void DrawPlayer(int playerID)
    {
        // Draw the player in a slightly different position depending on which number of player it is.
        var drawPosition = Vector2.zero;
        switch(playerDrawCall)
        {
            case 0:
                drawPosition = new Vector2(-0.2f, 0.2f);
                break;
            case 1:
                drawPosition = new Vector2(0.2f, 0.2f);
                break;
            case 2:
                drawPosition = new Vector2(-0.2f, -0.2f);
                break;
            case 3:
                drawPosition = new Vector2(0.2f, -0.2f);
                break;
        }

        // Draw the player and sets it textmesh to reflect the playerID.
        var player = Resources.Load("Player");
        var playerGO = (GameObject)Instantiate(player, drawPosition, Quaternion.identity);
        var textMesh = playerGO.GetComponentInChildren<TextMesh>();
        textMesh.GetComponent<MeshRenderer>().sortingLayerName = "Player";
        textMesh.GetComponent<MeshRenderer>().sortingOrder = 1;
        textMesh.text = playerID.ToString();

        playerDrawCall++;
    }
    #endregion

    private void OnDestroy()
    {
        if(client.driver.IsCreated)
        {
            client.readMessage = client.ReadJoingingAreaMessage;
        }
    }
}
