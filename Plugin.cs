using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using EntityFrameworkExample.Context;
using EntityFrameworkExample.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace EntityFrameworkExample;

public class Plugin(ILogger<Plugin> logger) : BasePlugin
{
    public override string ModuleName => "EntityFrameWorkExample";
    public override string ModuleVersion => "1.0.0";

    private ConcurrentDictionary<CCSPlayerController, UserConnectData> _userData = [];
    private DbContextOptions<OnlineContext>? _options;

    private readonly ILogger<Plugin> _logger = logger;

    public override void Load(bool hotReload)
    {
        RegisterListener<OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<OnClientDisconnect>(OnClientDisconnect);

        var path = Path.Combine(ModuleDirectory, "mydatabase.db");

        _options = new DbContextOptionsBuilder<OnlineContext>().UseSqlite($"Data Source={path}").Options;

        var _dbContext = new OnlineContext(_options);
        _dbContext.Database.EnsureCreated();

        // Set the journal mode to DELETE, but even without it, the database will be created. only not visible in .db file.
        _dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
        _logger.LogInformation("Database is created and ready to use.");
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<OnClientPutInServer>(OnClientPutInServer);
        RemoveListener<OnClientDisconnect>(OnClientDisconnect);
    }

    public void OnClientPutInServer(int playerslot)
    {
        var client = Utilities.GetPlayerFromSlot(playerslot);

        if(client == null || client.IsBot)
            return;

        _userData.TryAdd(client, new UserConnectData
        {
            ConnectTime = DateTime.Now,
            DisconnectTime = DateTime.Now.AddMinutes(30)
        });

        var steamid = client.SteamID;
        UserOnlineData? userOnlineData;

        Task.Run(async () =>
        {
            using (var dbContext = new OnlineContext(_options!))
            {
                userOnlineData = await dbContext.Onlines.FirstOrDefaultAsync(x => x.SteamId == steamid);

                if (userOnlineData != null)
                {
                    userOnlineData.LastLogin = DateTime.Now;
                    _userData[client].TotalPlayed += userOnlineData.PlayTime;
                    dbContext.Onlines.Update(userOnlineData);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Found data of {id}", steamid);
                }

                else
                {
                    userOnlineData = new UserOnlineData
                    {
                        SteamId = steamid,
                        LastLogin = DateTime.Now,
                        PlayTime = 0
                    };

                    _userData[client].TotalPlayed = 0;

                    await dbContext.Onlines.AddAsync(userOnlineData);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Insert new data of {id}", steamid);
                }
            }
        });
    }

    public void OnClientDisconnect(int playerslot)
    {
        var client = Utilities.GetPlayerFromSlot(playerslot);

        if (client == null || client.IsBot)
            return;

        if (_userData.TryRemove(client, out var userConnectData))
        {
            if(userConnectData == null)
                return;

            userConnectData.DisconnectTime = DateTime.Now;

            // Save userConnectData to the database
            var newPlayTime = userConnectData.GetCurrentPlayTime();
            var steamid = client.SteamID;
            var lastLogin = DateTime.Now;

            Task.Run(async () =>
            {
                using (var dbContext = new OnlineContext(_options!))
                {
                    var userOnlineData = await dbContext.Onlines.FirstOrDefaultAsync(x => x.SteamId == steamid);

                    if (userOnlineData != null)
                    {
                        userOnlineData.PlayTime += newPlayTime;
                        userOnlineData.LastLogin = lastLogin;
                        dbContext.Onlines.Update(userOnlineData);
                        _logger.LogInformation("Update data of {id} after disconnected", steamid);
                    }
                    else
                    {
                        userOnlineData = new UserOnlineData
                        {
                            SteamId = steamid,
                            LastLogin = lastLogin,
                            PlayTime = newPlayTime
                        };
                        await dbContext.Onlines.AddAsync(userOnlineData);
                        _logger.LogInformation("Insert new data of {id}", steamid);
                    }
                    await dbContext.SaveChangesAsync();
                }
            });
        }

        else
            _logger.LogWarning($"Failed to remove user data for player {client.PlayerName} (SteamID: {client.SteamID}) from the dictionary.");
    }
}
