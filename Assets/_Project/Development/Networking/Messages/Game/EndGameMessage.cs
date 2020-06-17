using System.Collections.Generic;
using Unity.Networking.Transport;

public struct EndGameMessage : INetworkMessage
{
    public uint id { get; private set; }
    public byte numberOfScores;
    public KeyValuePair<int, ushort>[] highScores;

    public EndGameMessage(uint id) : this(id, default, default) { }
    public EndGameMessage(uint id, byte numberOfScores, KeyValuePair<int, ushort>[] highScores)
    {
        this.id = id;
        this.numberOfScores = numberOfScores;
        this.highScores = highScores;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        numberOfScores = reader.ReadByte();

        highScores = new KeyValuePair<int, ushort>[numberOfScores];
        for(int i = 0; i < numberOfScores; i++)
        {
            highScores[i] = new KeyValuePair<int, ushort>(reader.ReadInt(), reader.ReadUShort());
        }
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.EndGame).Serialize(ref writer);

        writer.WriteByte(numberOfScores);

        for(int i = 0; i < numberOfScores; i++)
        {
            writer.WriteInt(highScores[i].Key);
            writer.WriteUShort(highScores[i].Value);
        }
    }
}
