using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public struct StartGameMessage : INetworkMessage
{
    public uint id { get; private set; }
    public ushort startHP;

    public StartGameMessage(uint id) : this(id, default) { }
    public StartGameMessage(uint id, ushort startHP)
    {
        this.id = id;
        this.startHP = startHP;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        startHP = reader.ReadUShort();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.StartGame).Serialize(ref writer);

        writer.WriteUShort(startHP);
    }
}
