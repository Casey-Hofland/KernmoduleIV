using System.ComponentModel;
using Unity.Networking.Transport;

public struct PlayerMessage : INetworkMessage
{
    public uint id { get; private set; }
    public MessageType messageType;
    public int playerID;

    public PlayerMessage(uint id, MessageType messageType) : this(id, messageType, default) { }
    public PlayerMessage(uint id, MessageType messageType, int playerID)
    {
        switch(messageType)
        {
            case MessageType.PlayerLeft:
            case MessageType.PlayerTurn:
            case MessageType.PlayerEnterRoom:
            case MessageType.PlayerLeaveRoom:
            case MessageType.PlayerLeftDungeon:
            case MessageType.PlayerDies:
                break;
            default:
                throw new InvalidEnumArgumentException(typeof(MessageType).Name + " value invalid.", (int)messageType, typeof(MessageType));
        }

        this.id = id;
        this.messageType = messageType;
        this.playerID = playerID;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerID = reader.ReadInt();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, messageType).Serialize(ref writer);

        writer.WriteInt(playerID);
    }
}
