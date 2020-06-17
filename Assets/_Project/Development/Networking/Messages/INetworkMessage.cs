using Unity.Networking.Transport;

public interface INetworkMessage
{
    uint id { get; }

    void Serialize(ref DataStreamWriter writer);
    void Deserialize(ref DataStreamReader reader);
}
