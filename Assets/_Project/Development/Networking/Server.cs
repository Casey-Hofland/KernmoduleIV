using System;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

using Random = UnityEngine.Random;

public class Server : MonoBehaviour
{
    public const int defaultMinConnections = 2;
    public const int defaultMaxConnections = 4;

    public NetworkDriver driver;
    public NetworkPipeline reliablePipeline;
    public NativeList<NetworkConnection> connections;
    public NativeHashMap<int, PlayerInfo> playerInfoByID;

    public Func<NetworkConnection, DataStreamReader, Message, INetworkMessage> readMessage;
    public delegate void Disconnected(NetworkConnection connection);
    public Disconnected onConnectionDisconnected;

    private int minConnections;
    private int maxConnections;
    public int connectionsLength => connections.IsCreated ? connections.Length : 0;
    public bool validConnections => connectionsLength >= minConnections && connectionsLength <= maxConnections;

    public void CreateDriver() => CreateDriver(defaultMinConnections, defaultMaxConnections);
    public void CreateDriver(int minConnections, int maxConnections)
    {
        this.minConnections = minConnections;
        this.maxConnections = maxConnections;

        DestroyDriver();

        // Create a new driver and listen for connections.
        driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        var endPoint = NetworkEndPoint.AnyIpv4;
        endPoint.Port = 9000;

        var binding = driver.Bind(endPoint);
        Assert.AreEqual(0, binding);
        driver.Listen();

        connections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
        playerInfoByID = new NativeHashMap<int, PlayerInfo>(maxConnections, Allocator.Persistent);

        readMessage = ReadJoingingAreaMessages;
    }

    public bool TrySendStartMessage()
    {
#if !UNITY_EDITOR
        if(!validConnections)
        {
            return false;
        }
#endif

        // Send messages to each connection that the game is starting.
        var startGameMessage = new StartGameMessage(MessageID.nextID, CaveQuestPlayer.maxHealth);
        for(int i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];

            Send(connection, startGameMessage);
        }

        return true;
    }

    private void Update()
    {
        if(driver.IsCreated)
        {
            driver.ScheduleUpdate().Complete();
            UpdateConnections();
            ReadConnections();
        }
    }

    private void UpdateConnections()
    {
        // Clean up Connections.
        for(int i = 0; i < connections.Length; i++)
        {
            if(!connections[i].IsCreated)
            {
                playerInfoByID.Remove(connections[i].InternalId);
                connections.RemoveAt(i);
                i--;
            }
        }

        // Accept new Connections.
        NetworkConnection connection;
        while((connection = driver.Accept()) != default)
        {
            if(connectionsLength >= maxConnections)
            {
                driver.Disconnect(connection);
            }
            else
            {
                // Send PlayerMessages and a WelcomeMessage to the new connection.
                var playerInfo = new PlayerInfo
                {
                    color = Random.ColorHSV(),
                    name = "",
                };
                playerInfoByID.Add(connection.InternalId, playerInfo);

                for(int i = 0; i < connectionsLength; i++)
                {
                    var otherConnection = connections[i];
                    var otherPlayerInfo = playerInfoByID[otherConnection.InternalId];
                    var newPlayerMessage = new NewPlayerMessage(MessageID.nextID, otherConnection.InternalId, otherPlayerInfo.color, otherPlayerInfo._name);
                    Send(connection, newPlayerMessage);
                }

                var welcomeMessage = new WelcomeMessage(MessageID.nextID, connection.InternalId, playerInfo.color);
                Send(connection, welcomeMessage);

                connections.AddNoResize(connection);
            }
        }
    }

    public INetworkMessage ReadJoingingAreaMessages(NetworkConnection connection, DataStreamReader reader, Message message)
    {
        switch(message.messageType)
        {
            // Send a stay alive message.
            case MessageType.None:
                Send(connection, new Message(0, MessageType.None));
                return null;
            // Set the name of the player and send NewPlayerMessages to all existing connections.
            case MessageType.SetName:
                var setNameMessage = new SetNameMessage(message.id);
                setNameMessage.Deserialize(ref reader);

                var playerInfo = playerInfoByID[connection.InternalId];
                playerInfo._name = setNameMessage._name;
                playerInfoByID.Remove(connection.InternalId);
                playerInfoByID.Add(connection.InternalId, playerInfo);
                var newPlayerMessage = new NewPlayerMessage(MessageID.nextID, connection.InternalId, playerInfo.color, setNameMessage.name);
                foreach(var otherConnection in connections)
                {
                    if(otherConnection == connection)
                    {
                        continue;
                    }

                    Send(otherConnection, newPlayerMessage);
                }

                return setNameMessage;
            default:
                return null;
        }
    }

    private void ReadConnections()
    {
        NetworkEvent.Type command;

        // Read Events.
        for(int i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            Assert.IsTrue(connection.IsCreated);

            while((command = driver.PopEventForConnection(connection, out var reader)) != NetworkEvent.Type.Empty)
            {
                switch(command)
                {
                    case NetworkEvent.Type.Data:
                        var message = new Message();
                        message.Deserialize(ref reader);

                        readMessage?.Invoke(connection, reader, message);

                        break;
                    // Let the other connections now which player has disconnected.
                    case NetworkEvent.Type.Disconnect:
                        var playerLeftMessage = new PlayerMessage(MessageID.nextID, MessageType.PlayerLeft, connection.InternalId);
                        foreach(var otherConnection in connections)
                        {
                            if(otherConnection == connection)
                            {
                                continue;
                            }

                            Send(otherConnection, playerLeftMessage);
                        }

                        onConnectionDisconnected?.Invoke(connection);

                        connections[i] = default;
                        break;
                }
            }
        }
    }

    public void Send(NetworkConnection connection, INetworkMessage message)
    {
        var writer = driver.BeginSend(reliablePipeline, connection);
        message.Serialize(ref writer);
        driver.EndSend(writer);
    }

    private void OnDestroy()
    {
        DestroyDriver();
    }

    // Destroys the driver safely.
    public void DestroyDriver()
    {
        if(driver.IsCreated)
        {
            for(int i = 0; i < connections.Length; i++)
            {
                driver.Disconnect(connections[i]);
            }
            driver.Dispose();

            readMessage = null;
        }
        if(connections.IsCreated)
        {
            connections.Dispose();
        }
        if(playerInfoByID.IsCreated)
        {
            playerInfoByID.Dispose();
        }
    }
}
