using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Grid Settings", menuName = "Cave Quest/Grid Settings")]
public class GridSettings : ScriptableObject
{
    public int2 size;
    public int treasures;
    public ushort minCoinsPerTreasure;
    public ushort maxCoinsPerTreasure;
    public uint coinWorth;
    public int monsters;
    public ushort minMonsterHealth;
    public ushort maxMonsterHealth;
}
