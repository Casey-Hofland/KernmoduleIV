using UnityEngine;

public static class MessageID
{
    private static uint _nextID = 0;
    public static uint nextID => ++_nextID;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        _nextID = 0;
    }
}
