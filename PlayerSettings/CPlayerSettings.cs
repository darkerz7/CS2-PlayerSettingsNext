using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace PlayerSettings
{
    internal class CPlayerSettings
    {
        private int userid;
        private readonly CCSPlayerController player;
        private readonly Dictionary<string, string> cached_values;                

        public CPlayerSettings(CCSPlayerController _player)
        {
            player = _player;
            userid = -1;
            Storage.GetUserIdAsync(player, (userid) => this.userid = userid);
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

        public void SetValue(string param, string value)
        {
            cached_values[param] = value;
            Storage.SetUserSettingValue(userid, param, value);
        }

        public int UserId()
        {
            return userid;
        }

        public bool EqualPlayer(CCSPlayerController _player)
        {
            return player == _player;
        }

        internal void ParseLoadedSettings(List<List<string?>>? rows, List<Action<CCSPlayerController>> actions)
        {
            if (rows != null)
            {
                Task.Run(() =>
                {
                    foreach (var row in rows)
                    {
                        if (row[0] is { } r0 && row[1] is { } r1) cached_values[r0] = r1;
                    }
                }).ContinueWith((_) =>
                {
                    foreach (var action in actions)
                        Server.NextWorldUpdateAsync(() => action(player));
                });
            }
        }

    }
}
