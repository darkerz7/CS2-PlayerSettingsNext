using System.Collections.Concurrent;

namespace PlayerSettings
{
    internal static class SettingItems
    {
        private static readonly ConcurrentDictionary<string, SettingItem> _items = new();
        internal static ICollection<SettingItem> Items => _items.Values;

        internal static void AddTogglable(string name, string viewName)
        {
            var newItem = new SettingItem(SettingType.Togglable, name, viewName);
            _items.AddOrUpdate(name, newItem, (key, oldValue) => newItem);
        }

        internal static void AddSelecting(string name, string viewName, Dictionary<string, string> values)
        {
            var newItem = new SettingItem(SettingType.Selecting, name, viewName, values);
            _items.AddOrUpdate(name, newItem, (key, oldValue) => newItem);
        }
    }
}
