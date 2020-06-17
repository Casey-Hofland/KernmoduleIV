using UnityEngine;

public static class ColorExtensions
{
    public static uint ToUInt(this Color color) => ((Color32)color).ToUInt();
    public static Color FromUInt(this Color color, uint value) => ((Color32)color).FromUInt(value);

    public static uint ToUInt(this Color32 color32)
    {
        return (uint)((color32[0] << 24) | (color32[1] << 16) | (color32[2] << 8) | color32[3]);
    }

    public static Color32 FromUInt(this Color32 color32, uint value)
    {
        color32[0] = (byte)((value >> 24) & 0xFF);
        color32[1] = (byte)((value >> 16) & 0xFF);
        color32[2] = (byte)((value >> 8) & 0xFF);
        color32[3] = (byte)((value) & 0xFF);

        return color32;
    }
}
