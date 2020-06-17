using Unity.Networking.Transport;

public struct MoveRequest : INetworkMessage
{
    public uint id { get; private set; }
    public Direction direction;

    public MoveRequest(uint id) : this(id, default) { }
    public MoveRequest(uint id, Direction direction)
    {
        this.id = id;
        this.direction = direction;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        direction = (Direction)reader.ReadByte();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.MoveRequest).Serialize(ref writer);

        writer.WriteByte((byte)direction);
    }
}
