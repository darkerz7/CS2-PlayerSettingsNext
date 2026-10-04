using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;

namespace PlayerSettings
{
    internal class CPlayerSettings
    {
        private readonly CCSPlayerController player;
        private readonly ConcurrentDictionary<string, string> cached_values;
        private readonly TaskCompletionSource<int> useridSource;

        public CPlayerSettings(CCSPlayerController _player)
        {
            player = _player;
            useridSource = new TaskCompletionSource<int>();
            Storage.GetUserIdAsync(player, (userid) => useridSource.TrySetResult(userid));
            cached_values = [];
        }

        public string GetValue(string param, string default_value)
        {
            if (!cached_values.TryGetValue(param, out string? value) || value == null)
            {
                value = default_value;
                cached_values[param] = value;
            }

            return value;
        }

        public async void SetValue(string param, string value)
        {
            cached_values[param] = value;

            int userid = await useridSource.Task;

            Storage.SetUserSettingValue(userid, param, value);
        }

        public Task<int> GetUserIdAsync()
        {
            return useridSource.Task;
        }

        internal void ParseLoadedSettings(int slot, List<List<string?>>? rows, List<Action<CCSPlayerController>> actions)
        {
            if (rows == null) return;

            var rowsCopy = rows.Select(r => r.ToList()).ToList();

            Task.Run(() =>
            {
                foreach (var row in rowsCopy)
                {
                    if (row.Count >= 2 && row[0] is string key && row[1] is string val) cached_values[key] = val;
                }
            }).ContinueWith((_) =>
            {
                Server.NextWorldUpdateAsync(() =>
                {
                    var currentPlayer = Utilities.GetPlayerFromSlot(slot);
                    if (currentPlayer == null || !currentPlayer.IsValid || currentPlayer.IsBot) return;
                    foreach (var action in actions) action(player);
                });
            });
        }

    }
}
