namespace DayZHelper.Services;

public sealed class Favorite
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public int Port { get; set; }
    public string LastPlayedUtc { get; set; } = "";
}