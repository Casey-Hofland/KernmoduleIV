using System.ComponentModel;
using Unity.Networking.Transport;

public struct AttackMessage : INetworkMessage
{
    public uint id { get; private set; }
    public MessageType messageType;
    public int playerID;
    public ushort value;

    public AttackMessage(uint id, MessageType messageType) : this(id, messageType, default, default) { }
    public AttackMessage(uint id, MessageType messageType, int playerID, ushort value)
    {
        switch(messageType)
        {
            case MessageType.HitMonster:
            case MessageType.HitByMonster:
            case MessageType.PlayerDefends:
                break;
            default:
                throw new InvalidEnumArgumentException(typeof(MessageType).Name + " value invalid.", (int)messageType, typeof(MessageType));
        }

        this.id = id;
        this.messageType = messageType;
        this.playerID = playerID;
        this.value = value;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerID = reader.ReadInt();
        value = reader.ReadUShort();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, messageType).Serialize(ref writer);

        writer.WriteInt(playerID);
        writer.WriteUShort(value);
    }
}
