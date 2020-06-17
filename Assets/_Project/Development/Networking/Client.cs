using System;
using System.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    public NetworkDriver driver;
    public NetworkPipeline reliablePipeline;
    public NetworkConnection connection;

    public Func<DataStreamReader, Message, INetworkMessage> readMessage;

    public delegate void MessageReceived(Client client, INetworkMessage iMessage);
    public MessageReceived onMessageReceived;

    public delegate void Disconnected(Client client);
    public Disconnected onDisconnected;

    public int playerID { get; private set; }
    public PlayerInfo playerInfo;

    private Coroutine stayAliveCoroutine;

    public void CreateDriver()
    {
        DestroyDriver();

        driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        readMessage = ReadJoingingAreaMessage;
    }

    public INetworkMessage ReadJoingingAreaMessage(DataStreamReader reader, Message message)
    {
        switch(message.messageType)
        {
            // Read a NewPlayer message.
            case MessageType.NewPlayer:
                var newPlayerMessage = new NewPlayerMessage(message.id);
                newPlayerMessage.Deserialize(ref reader);
                return newPlayerMessage;
            // Set your playedID and playerInfo.
            case MessageType.Welcome:
                var welcomeMessage = new WelcomeMessage(message.id);
                welcomeMessage.Deserialize(ref reader);

                playerID = welcomeMessage.playerID;
                playerInfo.color = welcomeMessage.color;

                Send(new SetNameMessage(MessageID.nextID, playerInfo.name));

                return welcomeMessage;
            // Read a PlayerLeft message.
            case MessageType.PlayerLeft:
                var playerLeftMessage = new PlayerMessage(message.id, message.messageType);
                playerLeftMessage.Deserialize(ref reader);
                return playerLeftMessage;
            // Load Cave Quest and initialize the players stats.
            case MessageType.StartGame:
                var startGameMessage = new StartGameMessage(message.id);
                startGameMessage.Deserialize(ref reader);

                var caveQuestPlayer = gameObject.AddComponent<CaveQuestPlayer>();
                SceneManager.LoadSceneAsync("Cave Quest").completed += (asyncOperation) => 
                {
                    caveQuestPlayer.Initialize(startGameMessage.startHP);
                };

                return startGameMessage;
            default:
                return null;
        }
    }

    // Connect to the specified address (or LoopbackIpv4) and start a coroutine continuously sending stay alive messages.
    public void Connect() => Connect(null);
    public void Connect(string address)
    {
        var endPoint = string.IsNullOrWhiteSpace(address) ? NetworkEndPoint.LoopbackIpv4 : NetworkEndPoint.Parse(address, 9000);
        endPoint.Port = 9000;

        connection = driver.Connect(endPoint);

        if(stayAliveCoroutine == null)
        {
            stayAliveCoroutine = StartCoroutine(SendStayAliveMessages());
        }
    }

    // Send a stay alive message every 5 seconds.
    private IEnumerator SendStayAliveMessages()
    {
        var stayAliveMessage = new Message(0, MessageType.None);
        var waitForSecondsRealtime = new WaitForSecondsRealtime(5f);

        while(true)
        {
            yield return waitForSecondsRealtime;
            Send(stayAliveMessage);
        }
    }

    private void Update()
    {
        if(driver.IsCreated)
        {
            driver.ScheduleUpdate().Complete();

            if(connection.IsCreated)
            {
                ReadConnection();
            }
        }
    }

    private void ReadConnection()
    {
        NetworkEvent.Type command;
        while((command = connection.PopEvent(driver, out var reader)) != NetworkEvent.Type.Empty)
        {
            switch(command)
            {
                case NetworkEvent.Type.Data:
                    var message = new Message();
                    message.Deserialize(ref reader);

                    INetworkMessage networkMessage = readMessage?.Invoke(reader, message);
                    onMessageReceived?.Invoke(this, networkMessage);
                    break;
                // Disconnect from the server, stop sending stay alive messages and load the Joining Area.
                case NetworkEvent.Type.Disconnect:
                    connection = default;
                    onDisconnected?.Invoke(this);
                    if(stayAliveCoroutine != null)
                    {
                        StopCoroutine(stayAliveCoroutine);
                        stayAliveCoroutine = null;
                    }
                    Destroy(gameObject);

                    const string joiningAreaSceneName = "Joining Area";
                    if(SceneManager.GetActiveScene().name != joiningAreaSceneName)
                    {
                        SceneManager.LoadScene(joiningAreaSceneName);
                    }
                    break;
            }
        }
    }

    public void Send(INetworkMessage message)
    {
        var writer = driver.BeginSend(reliablePipeline, connection);
        message.Serialize(ref writer);
        driver.EndSend(writer);
    }

    private void OnDestroy()
    {
        DestroyDriver();
    }

    // Destroy the driver safely.
    public void DestroyDriver()
    {
        if(driver.IsCreated)
        {
            driver.Disconnect(connection);
            connection = default;
            driver.Dispose();

            readMessage = null;
        }
    }
}
