using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public struct NewPlayerMessage : INetworkMessage
{
    public uint id { get; private set; }
    public int playerID;
    public Color color;
    public NativeString64 _name;
    public string name
    {
        get => _name.ToString();
        set => _name = value;
    }

    public NewPlayerMessage(uint id) : this(id, default, default, default) { }
    public NewPlayerMessage(uint id, int playerID, Color color, string name) : this(id, playerID, color, (NativeString64)name) { }
    public NewPlayerMessage(uint id, int playerID, Color color, NativeString64 name)
    {
        this.id = id;
        this.playerID = playerID;
        this.color = color;
        this._name = name;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerID = reader.ReadInt();
        color = color.FromUInt(reader.ReadUInt());
        _name = reader.ReadString();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.NewPlayer).Serialize(ref writer);

        writer.WriteInt(playerID);
        writer.WriteUInt(color.ToUInt());
        writer.WriteString(_name);
    }
}
