using Unity.Collections;
using UnityEngine;

public struct PlayerInfo
{
    public Color color;
    public NativeString64 _name;
    public string name
    {
        get => _name.ToString();
        set => _name = value;
    }
}
