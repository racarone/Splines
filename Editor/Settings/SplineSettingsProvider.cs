using System;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Splines
{
    static class ProBuilderSettingsProvider
    {
        const string k_PreferencesPath = "Preferences/Splines";

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider(k_PreferencesPath,
                                                    SplineSettings.instance,
                                                    new[] {typeof(ProBuilderSettingsProvider).Assembly});

            // SplineSettings.instance.afterSettingsSaved += () =>
            // {
            //     if (SplineSettings.instance != null)
            //         SplineSettings.ReloadSettings();
            // };

            return provider;
        }
    }
}