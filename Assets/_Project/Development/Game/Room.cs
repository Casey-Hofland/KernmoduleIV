public struct Room
{
    public ushort treasure;
    public ushort monsterHealth;
    public bool exit;

    public bool monster => monsterHealth > 0;
}
