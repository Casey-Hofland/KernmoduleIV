using Unity.Networking.Transport;

public struct Message : INetworkMessage
{
    public uint id { get; private set; }
    public MessageType messageType;

    public Message(uint id) : this(id, default) { }
    public Message(uint id, MessageType messageType)
    {
        this.id = id;
        this.messageType = messageType;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        messageType = (MessageType)reader.ReadUShort();
        id = reader.ReadUInt();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUShort((ushort)messageType);
        writer.WriteUInt(id);
    }
}
