using AnyBaseLibNext;
using CounterStrikeSharp.API.Core;

namespace PlayerSettings
{
    internal static class Storage
    {
        private static IAnyBaseNext? db;
        private static string table = "settings_";
        private static bool isSQLite = false;

        public static void Init(PluginConfig Config, string ModuleDirectory)
        {
            table = Config.DatabaseParams.Table;
            if (Config.DatabaseParams.IsLocal())
            {
                isSQLite = true;
                db = CAnyBaseNext.Base("sqlite");
                db.Set(Path.Combine(ModuleDirectory, "settings"));
            }
            else
            {
                isSQLite = false;
                db = CAnyBaseNext.Base("mysql");
                db.Set(Config.DatabaseParams.Name, Config.DatabaseParams.Host, Config.DatabaseParams.User, Config.DatabaseParams.Password);
            }

            db.QueryAsync($"CREATE TABLE IF NOT EXISTS `{table}users` (`id` INTEGER PRIMARY KEY AUTO_INCREMENT, `steam` VARCHAR(255) NOT NULL)", null, (_) =>
            {
                db.QueryAsync($"CREATE TABLE IF NOT EXISTS `{table}values` (`user_id` INT, `param` VARCHAR(255) NOT NULL, `value` VARCHAR(255) NOT NULL, UNIQUE(`user_id`, `param`))", null, (_) =>
                {
                    ApplyDatabaseMigration();
                }, true, true);
            }, true, true);
        }

        private static void ApplyDatabaseMigration()
        {
            if (isSQLite)
            {
                db?.QueryAsync($"DELETE FROM `{table}values` WHERE rowid NOT IN (SELECT MIN(rowid) FROM `{table}values` GROUP BY `user_id`, `param`)", null, (_) =>
                {
                    db.QueryAsync($"CREATE UNIQUE INDEX IF NOT EXISTS `uniq_user_param` ON `{table}values` (`user_id`, `param`)", null, (_) => { }, true, true);
                }, true, true);
            } else
            {
                db?.QueryAsync($"SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}values' AND NON_UNIQUE = 0 LIMIT 1", null, (data) =>
                {
                    if (data == null || data.Count == 0)
                    {
                        db?.QueryAsync($"DELETE t1 FROM `{table}values` t1 INNER JOIN `{table}values` t2 WHERE t1.user_id = t2.user_id AND t1.param = t2.param AND t1.value != t2.value", null, (_) =>
                        {
                            db?.QueryAsync($"ALTER TABLE `{table}values` ADD CONSTRAINT `unique_user_param` UNIQUE (`user_id`, `param`)", null, (_) => { }, true, true);
                        }, true, true);
                    }
                }, true, true);
            }
        }

        public static void GetUserIdAsync(CCSPlayerController player, Action<int> callback)
        {
            var steamid = player.SteamID.ToString();
            db?.QueryAsync("SELECT `id` FROM `" + table + "users` WHERE `steam` = {ARG}", new List<string>([steamid]), (data) =>
            {
                if (data != null && data.Count > 0 && data[0] is { } d && d[0] is { } sSteamID)
                {
                    callback(int.Parse(sSteamID));
                    return;
                }

                db.QueryAsync("INSERT INTO `" + table + "users` (`steam`) VALUES ({ARG}); SELECT `id` FROM `" + table + "users` WHERE `steam` = {ARG}", new List<string>([steamid, steamid]), (datainsert) =>
                {
                    if (datainsert != null && datainsert.Count > 0 && datainsert[0] is { } di && di[0] is { } sSteamIDInsert) callback(int.Parse(sSteamIDInsert));
                });
            });
        }

        internal static void LoadSettings(int userid, Action<List<List<string?>>?>? action)
        {
            if (userid <= 0) return;
            db?.QueryAsync("SELECT `param`, `value` FROM `" + table + "values` WHERE `user_id` = {ARG}", new List<string>([userid.ToString()]), action);
        }

        public static void SetUserSettingValue(int userid, string param, string value)
        {
            if (userid <= 0) return;

            if (isSQLite)
                db?.QueryAsync($"INSERT OR REPLACE INTO `{table}values` (`user_id`, `param`, `value`) VALUES ({{ARG}}, {{ARG}}, {{ARG}})", new List<string>([userid.ToString(), param, value]), null, true);
            else
                db?.QueryAsync($"INSERT INTO `{table}values` (`user_id`, `param`, `value`) VALUES ({{ARG}}, {{ARG}}, {{ARG}}) ON DUPLICATE KEY UPDATE `value` = {{ARG}}", new List<string>([userid.ToString(), param, value, value]), null, true);

        }

        public static void Close() => db?.UnSet();
    }
}
