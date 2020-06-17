using System.ComponentModel;
using Unity.Networking.Transport;

public struct ActionRequest : INetworkMessage
{
    public uint id { get; private set; }
    public MessageType messageType;

    public ActionRequest(uint id, MessageType messageType)
    {
        switch(messageType)
        {
            case MessageType.AttackRequest:
            case MessageType.DefendRequest:
            case MessageType.ClaimTreasureRequest:
            case MessageType.LeaveDungeonRequest:
                break;
            default:
                throw new InvalidEnumArgumentException(typeof(MessageType).Name + " value invalid.", (int)messageType, typeof(MessageType));
        }

        this.id = id;
        this.messageType = messageType;
    }

    public void Deserialize(ref DataStreamReader reader)
    {
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        new Message(id, messageType).Serialize(ref writer);
    }
}
