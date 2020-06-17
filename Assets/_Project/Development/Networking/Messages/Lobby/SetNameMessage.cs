using Unity.Collections;
using Unity.Networking.Transport;

public struct SetNameMessage : INetworkMessage
{
    public uint id { get; private set; }
    public NativeString64 _name;
    public string name
    {
        get => _name.ToString();
        set => _name = value;
    }

    public SetNameMessage(uint id) : this(id, default) { }
    public SetNameMessage(uint id, string name) : this(id, (NativeString64)name) { }
    public SetNameMessage(uint id, NativeString64 name)
    {
        this.id = id;
        this._name = name;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        _name = reader.ReadString();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, MessageType.SetName).Serialize(ref writer);

        writer.WriteString(_name);
    }
}
