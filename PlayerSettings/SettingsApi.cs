using CounterStrikeSharp.API.Core;

namespace PlayerSettings
{
    internal class SettingsApi : ISettingsApi
    {
        private CPlayerSettings[] settings = [];
        internal List<Action<CCSPlayerController>> actions = [];

        public string GetPlayerSettingsValue(CCSPlayerController player, string param, string default_value) => FindUser(player).GetValue(param, default_value);
        public void SetPlayerSettingsValue(CCSPlayerController player, string param, string value) => FindUser(player).SetValue(param, value);
        public void AddHook(Action<CCSPlayerController> action) => actions.Add(action);
        public void RemHook(Action<CCSPlayerController> action) => actions.RemoveAll(x => x == action);
        public void RegisterTogglableSetting(string name, string viewName) => SettingItems.AddTogglable(name, viewName);
        public void RegisterSelectingSetting(string name, string viewName, Dictionary<string, string> values) => SettingItems.AddSelecting(name, viewName, values);
        public List<SettingItem> GetSettingItems()
        {
            var list = new List<SettingItem>();
            foreach (var item in SettingItems.Items)
                list.Add(item);
            return list;
        }

        private CPlayerSettings FindUser(CCSPlayerController player)
        {
            foreach (var item in this.settings)
                if (item.EqualPlayer(player)) return item;
            
            var newInst = new CPlayerSettings(player);
            AddPlayerInst(newInst);
            return newInst;
        }
        private void AddPlayerInst(CPlayerSettings inst)
        {
            Array.Resize(ref settings, settings.Length + 1);
            settings[^1] = inst;
        }
        internal void LoadOnConnect(CCSPlayerController player)
        {
            var user = FindUser(player);

            Task.Run(() => { while (user.UserId() == -1) Task.Delay(50).Wait(); }).ContinueWith((_) =>
                Storage.LoadSettings(user.UserId(), (vars) => user.ParseLoadedSettings(vars, actions))
            );
        }
        
    }
}
