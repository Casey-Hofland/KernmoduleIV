using System;
using Unity.Networking.Transport;

public struct RoomInfo : INetworkMessage
{
    public uint id { get; private set; }
    public Direction moveDirections;
    public ushort treasureInRoom;
    public bool containsMonster;
    public bool containsExit;
    public byte numberOfOtherPlayers;
    public int[] otherPlayerIDs;

    public RoomInfo(uint id) : this() { this.id = id; }
    public RoomInfo(uint id, Direction moveDirections, ushort treasureInRoom, bool containsMonster, bool containsExit, byte numberOfOtherPlayers, int[] otherPlayerIDs)
    {
        this.id = id;
        this.moveDirections = moveDirections;
        this.treasureInRoom = treasureInRoom;
        this.containsMonster = containsMonster;
        this.containsExit = containsExit;
        this.numberOfOtherPlayers = numberOfOtherPlayers;
        this.otherPlayerIDs = otherPlayerIDs;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        moveDirections = (Direction)reader.ReadByte();
        treasureInRoom = reader.ReadUShort();
        containsMonster = Convert.ToBoolean(reader.ReadByte());
        containsExit = Convert.ToBoolean(reader.ReadByte());
        numberOfOtherPlayers = reader.ReadByte();

        otherPlayerIDs = new int[numberOfOtherPlayers];
        for(int i = 0; i < numberOfOtherPlayers; i++)
        {
            otherPlayerIDs[i] = reader.ReadInt();
        }
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.RoomInfo).Serialize(ref writer);

        writer.WriteByte((byte)moveDirections);
        writer.WriteUShort(treasureInRoom);
        writer.WriteByte(Convert.ToByte(containsMonster));
        writer.WriteByte(Convert.ToByte(containsExit));
        writer.WriteByte(numberOfOtherPlayers);

        for(int i = 0; i < numberOfOtherPlayers; i++)
        {
            writer.WriteInt(otherPlayerIDs[i]);
        }
    }
}
