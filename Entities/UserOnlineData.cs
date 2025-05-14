namespace EntityFrameworkExample.Entities;

public class UserOnlineData
{
    public int Id { get; set; }
    public ulong SteamId { get; set; }
    public DateTime LastLogin { get; set; }
    public int PlayTime { get; set; }
}
