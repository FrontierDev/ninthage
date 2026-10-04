using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Static class to manage client-specific settings. 
    /// This can include graphics quality, audio volume, keybindings, 
    /// and other preferences that are relevant to the client side of the game.
    /// 
    /// Settings are persisted locally using PlayerPrefs.
    /// </summary>
    public static class ClientSettingsManager
    {
        private static Dictionary<string, object> settings = new Dictionary<string, object>();
        private const string SETTINGS_PREFIX = "setting_";

        public static Action<string, object> onSettingChanged;
        public static Action onSettingsLoaded;

        /// <summary>
        /// Initialize settings from PlayerPrefs.
        /// </summary>
        public static void Initialize()
        {
            settings.Clear();
            onSettingsLoaded?.Invoke();
        }

        /// <summary>
        /// Get a setting value. Returns default value if not set.
        /// </summary>
        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            if (settings.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            string prefKey = SETTINGS_PREFIX + key;

            if (typeof(T) == typeof(int))
            {
                if (PlayerPrefs.HasKey(prefKey))
                {
                    var result = (T)(object)PlayerPrefs.GetInt(prefKey);
                    settings[key] = result;
                    return result;
                }
            }
            else if (typeof(T) == typeof(float))
            {
                if (PlayerPrefs.HasKey(prefKey))
                {
                    var result = (T)(object)PlayerPrefs.GetFloat(prefKey);
                    settings[key] = result;
                    return result;
                }
            }
            else if (typeof(T) == typeof(string))
            {
                if (PlayerPrefs.HasKey(prefKey))
                {
                    var result = (T)(object)PlayerPrefs.GetString(prefKey);
                    settings[key] = result;
                    return result;
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (PlayerPrefs.HasKey(prefKey))
                {
                    var result = (T)(object)(PlayerPrefs.GetInt(prefKey) == 1);
                    settings[key] = result;
                    return result;
                }
            }

            settings[key] = defaultValue;
            return defaultValue;
        }

        /// <summary>
        /// Set a setting value and persist it.
        /// </summary>
        public static void SetSetting<T>(string key, T value)
        {
            settings[key] = value;

            string prefKey = SETTINGS_PREFIX + key;

            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(prefKey, (int)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(prefKey, (float)(object)value);
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(prefKey, (string)(object)value);
            }
            else if (typeof(T) == typeof(bool))
            {
                PlayerPrefs.SetInt(prefKey, (bool)(object)value ? 1 : 0);
            }

            PlayerPrefs.Save();
            onSettingChanged?.Invoke(key, value);
        }

        /// <summary>
        /// Check if a setting key exists.
        /// </summary>
        public static bool HasSetting(string key)
        {
            return PlayerPrefs.HasKey(SETTINGS_PREFIX + key);
        }

        /// <summary>
        /// Delete a specific setting.
        /// </summary>
        public static void DeleteSetting(string key)
        {
            settings.Remove(key);
            PlayerPrefs.DeleteKey(SETTINGS_PREFIX + key);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clear all settings.
        /// </summary>
        public static void ClearAllSettings()
        {
            settings.Clear();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}