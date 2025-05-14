using EntityFrameworkExample.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkExample.Context;

internal class OnlineContext : DbContext
{
    public OnlineContext(DbContextOptions<OnlineContext> options) : base(options)
    {
    }

    public DbSet<UserOnlineData> Onlines { get; set; }
}
