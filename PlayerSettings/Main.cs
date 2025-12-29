using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;
using System.Text.Json.Serialization;

namespace PlayerSettings;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DatabaseParams")] public DatabaseParams DatabaseParams { get; set; } = new();
}
public class PlayerSettingsCore : BasePlugin, IPluginConfig<PluginConfig>
{
    public PluginConfig Config { get; set; } = new();

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        Storage.Init(Config, ModuleDirectory);
    }

    public override string ModuleName => "[Core]PlayerSettingsNext";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Nick Fox, DarkerZ [RUS]";
    public override string ModuleDescription => "One storage for player's settings (aka ClientCookies)";

    private SettingsApi? _api;
    private readonly PluginCapability<ISettingsApi?> _pluginCapability = new("settings:nfcore");
    public override void Load(bool hotReload)
    {
        _api = new SettingsApi();
        Capabilities.RegisterPluginCapability(_pluginCapability, () => _api);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);

        if (hotReload)
            foreach (var player in Utilities.GetPlayers())
                if (player.AuthorizedSteamID != null) OnClientAuthorized(player.Slot, player.AuthorizedSteamID);
    }

    public override void Unload(bool hotReload) => Storage.Close();

    private void OnClientAuthorized(int slot, SteamID steamID)
    {
        if(Utilities.GetPlayerFromSlot(slot) is { } player) _api?.LoadOnConnect(player);
    }
}

public struct DatabaseParams
{
    public string Host { get; set; }
    public string Name { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string Table { get; set; }
    public DatabaseParams()
    {
        Host = "127.0.0.1:3306";
        Name = "";
        User = "";
        Password = "";
        Table = "settings_";
    }
    public readonly bool IsLocal() => (Host == "127.0.0.1:3306" && Name == "" && User == "" && Password == "") || Host == "";
}