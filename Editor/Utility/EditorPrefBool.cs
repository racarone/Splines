﻿using UnityEditor;

namespace Splines
{
    readonly struct EditorPrefBool
    {
        readonly string m_Key;

        public bool value
        {
            get => EditorPrefs.GetBool(m_Key);
            set => EditorPrefs.SetBool(m_Key, value);
        }

        public EditorPrefBool(string key, bool defaultValue = false)
        {
            m_Key = key;

            if (!EditorPrefs.HasKey(m_Key))
            {
                EditorPrefs.SetBool(m_Key, defaultValue);
            }
        }
    }
}