using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Splines
{
    sealed class Pref<T> : UserSetting<T>
    {
        public Pref(string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(SplineSettings.instance, key, value, scope)
        {}

        public Pref(Settings settings, string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(settings, key, value, scope) {}

        public static implicit operator T(Pref<T> pref)
        {
            return pref.value;
        }
    }
}