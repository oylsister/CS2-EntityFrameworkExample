namespace EntityFrameworkExample.Entities;

public class UserConnectData
{
    public DateTime ConnectTime { get; set; }
    public DateTime DisconnectTime { get; set; }
    public int TotalPlayed { get; set; }

    public int GetCurrentPlayTime()
    {
        return (int)(DisconnectTime - ConnectTime).TotalMinutes;
    }

    public int GetTotalPlayTime()
    {
        return TotalPlayed + GetCurrentPlayTime();
    }
}
