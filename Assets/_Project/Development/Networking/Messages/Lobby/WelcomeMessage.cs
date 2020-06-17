using Unity.Networking.Transport;
using UnityEngine;

public struct WelcomeMessage : INetworkMessage
{
    public uint id { get; private set; }
    public int playerID;
    public Color color;

    public WelcomeMessage(uint id) : this(id, default, default) { }
    public WelcomeMessage(uint id, int playerID, Color color)
    {
        this.id = id;
        this.playerID = playerID;
        this.color = color;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerID = reader.ReadInt();
        color = color.FromUInt(reader.ReadUInt());
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.Welcome).Serialize(ref writer);

        writer.WriteInt(playerID);
        writer.WriteUInt(color.ToUInt());
    }
}
