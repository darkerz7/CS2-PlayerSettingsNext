using AnyBaseLibNext;
using CounterStrikeSharp.API.Core;

namespace PlayerSettings
{
    internal static class Storage
    {
        private static IAnyBaseNext? db;
        private static string table = "settings_";

        public static void Init(PluginConfig Config, string ModuleDirectory)
        {
            table = Config.DatabaseParams.Table;
            if (Config.DatabaseParams.IsLocal())
            {
                db = CAnyBaseNext.Base("sqlite");
                db.Set(Path.Combine(ModuleDirectory, "settings"));
            }
            else
            {
                db = CAnyBaseNext.Base("mysql");
                db.Set(Config.DatabaseParams.Name, Config.DatabaseParams.Host, Config.DatabaseParams.User, Config.DatabaseParams.Password);
            }

            db.QueryAsync($"CREATE TABLE IF NOT EXISTS `{table}users` (`id` INTEGER PRIMARY KEY AUTO_INCREMENT, `steam` VARCHAR(255) NOT NULL)", null, (_) =>
            {
                db.QueryAsync($"CREATE TABLE IF NOT EXISTS `{table}values` (`user_id` INT, `param` VARCHAR(255) NOT NULL, `value` VARCHAR(255) NOT NULL)", null, (_) => { }, true, true);
            }, true, true);
        }

        public static void GetUserIdAsync(CCSPlayerController player, Action<int> callback)
        {
            var steamid = player.SteamID;
            db?.QueryAsync("SELECT `id` FROM `" + table + "users` WHERE `steam` = {ARG}", new List<string>([steamid.ToString()]), (data) =>
            {
                if (data != null)
                {
                    if (data.Count > 0)
                    {
                        if (data[0] is { } d && d[0] is { } sSteamID) callback(int.Parse(sSteamID));
                    }
                    else db.QueryAsync("INSERT INTO `" + table + "users` (`steam`) VALUES ({ARG}); SELECT `id` FROM `" + table + "users` WHERE `steam` = {ARG}", new List<string>([steamid.ToString(), steamid.ToString()]), (datainsert) =>
                    {
                        if (datainsert != null && datainsert[0] is { } di && di[0] is { } sSteamIDInsert) callback(int.Parse(sSteamIDInsert));
                    });
                }
            });
        }

        internal static void LoadSettings(int userid, Action<List<List<string?>>?>? action) => db?.QueryAsync("SELECT `param`, `value` FROM `" + table + "values` WHERE `user_id` = {ARG}", new List<string>([userid.ToString()]), action);
        public static void SetUserSettingValue(int userid, string param, string value) => db?.QueryAsync("SELECT `value` FROM `" + table + "values` WHERE `user_id` = {ARG} AND `param` = {ARG}", new List<string>([userid.ToString(), param]), (data) =>
            {
                if (data != null) SetUserSettingValuePost(userid, param, value, data.Count);
            });

        private static void SetUserSettingValuePost(int userid, string param, string value, int co)
        {
            if (co == 0) db?.QueryAsync("INSERT INTO `" + table + "values` (`user_id`, `param`, `value`) VALUES ({ARG}, {ARG}, {ARG})", new List<string>([userid.ToString(), param, value]), null, true);
            else db?.QueryAsync("UPDATE `" + table + "values` SET `value` = {ARG} WHERE `user_id` = {ARG} AND `param` = {ARG}", new List<string>([value, userid.ToString(), param]), null, true);
        }

        public static void Close() => db?.UnSet();
    }
}
