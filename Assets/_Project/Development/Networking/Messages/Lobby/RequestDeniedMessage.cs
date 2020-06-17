using Unity.Networking.Transport;

public struct RequestDeniedMessage : INetworkMessage
{
    public uint id { get; private set; }
    public uint deniedMessageID;

    public RequestDeniedMessage(uint id) : this(id, default) { }
    public RequestDeniedMessage(uint id, uint deniedMessageID)
    {
        this.id = id;
        this.deniedMessageID = deniedMessageID;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        deniedMessageID = reader.ReadUInt();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.RequestDenied).Serialize(ref writer);

        writer.WriteUInt(deniedMessageID);
    }
}
