using Unity.Networking.Transport;

public struct ObtainTreasure : INetworkMessage
{
    public uint id { get; private set; }
    public ushort amount;

    public ObtainTreasure(uint id) : this(id, default) { }
    public ObtainTreasure(uint id, ushort amount)
    {
        this.id = id;
        this.amount = amount;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        amount = reader.ReadUShort();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.ObtainTreasure).Serialize(ref writer);

        writer.WriteUShort(amount);
    }
}
