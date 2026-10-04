using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;

namespace PlayerSettings
{
    internal class SettingsApi : ISettingsApi
    {
        private readonly ConcurrentDictionary<int, CPlayerSettings> settings = [];
        private readonly Lock _actionsLock = new();
        internal List<Action<CCSPlayerController>> actions = [];

        public string GetPlayerSettingsValue(CCSPlayerController player, string param, string default_value) => FindUser(player).GetValue(param, default_value);
        public void SetPlayerSettingsValue(CCSPlayerController player, string param, string value) => FindUser(player).SetValue(param, value);
        public void AddHook(Action<CCSPlayerController> action){ lock (_actionsLock) actions.Add(action);}
        public void RemHook(Action<CCSPlayerController> action){ lock (_actionsLock) actions.RemoveAll(x => x == action);}
        public void RegisterTogglableSetting(string name, string viewName) => SettingItems.AddTogglable(name, viewName);
        public void RegisterSelectingSetting(string name, string viewName, Dictionary<string, string> values) => SettingItems.AddSelecting(name, viewName, values);
        public List<SettingItem> GetSettingItems() => [..SettingItems.Items];

        private CPlayerSettings FindUser(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) throw new ArgumentNullException(nameof(player), "Player not valid!");

            return settings.GetOrAdd(player.Slot, (slot) => new CPlayerSettings(player));
        }

        internal void LoadOnConnect(int slot)
        {
            if (Utilities.GetPlayerFromSlot(slot) is { } player)
            {

                var user = FindUser(player);

                Task.Run(async () =>
                {
                    int userId = await user.GetUserIdAsync();
                    if (userId <= 0) return;

                    List<Action<CCSPlayerController>> actionsCopy;
                    lock (_actionsLock) actionsCopy = [..actions];

                    Storage.LoadSettings(userId, (vars) => user.ParseLoadedSettings(slot, vars, actionsCopy));
                });
            }
        }

        internal void RemoveUser(int slot) => settings.TryRemove(slot, out _);
    }
}
