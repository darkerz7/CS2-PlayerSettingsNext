namespace PlayerSettings
{
    public class SettingItem
    {
        public string Name;
        public string ViewName;
        public Dictionary<string, string>? Values;
        public SettingType Type;

        public SettingItem(SettingType type, string name, string viewName, Dictionary<string, string>? values = null)
        {
            if(type != SettingType.Togglable && values == null)
                throw new ArgumentException("Non-togglable settings must have values");

            Type = type;
            Name = name;
            ViewName = viewName;
            Values = values;
        }
    }

    public enum SettingType
    {
        Togglable,
        Selecting
    }
}
