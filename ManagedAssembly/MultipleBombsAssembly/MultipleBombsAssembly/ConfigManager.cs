using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public static class ConfigManager
    {
        public static string ConfigFilePath = Application.persistentDataPath + "/Modsettings/MultipleBombs-settings.txt";
        private static Dictionary<string, string> pairs;

        static ConfigManager()
        {
            if (!File.Exists(ConfigFilePath))
                File.WriteAllText(ConfigFilePath, AssemblyResources.modSettings);
            pairs = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(ConfigFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("#"))
                    continue;
                string[] kv = line.Split('=');
                if (kv.Length < 2)
                {
                    Debug.Log("[MultipleBombs]Error in config file at line " + i + ": couldn't parse key=value pair");
                    continue;
                }
                if (pairs.ContainsKey(kv[0]))
                {
                    Debug.Log("[MultipleBombs]Error in config file at line " + i + ": key \"" + kv[0] + "\" already defined");
                    continue;
                }
                pairs.Add(kv[0], kv[1]);
            }
        }

        public static string GetValue(string key)
        {
            return GetValue(key, null);
        }

        public static string GetValue(string key, string defaultValue)
        {
            if (pairs.ContainsKey(key))
                return pairs[key];
            return defaultValue;
        }

        public static void SetValue(string key, string value)
        {
            if (pairs.ContainsKey(key))
                pairs[key] = value;
            else
                pairs.Add(key, value);
        }

        public static void Save()
        {
            List<string> lines = File.ReadAllLines(ConfigFilePath).ToList();
            foreach (KeyValuePair<string, string> kv in pairs)
            {
                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(kv.Key + "="))
                    {
                        lines[i] = kv.Key + "=" + kv.Value;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    lines.Add(kv.Key + "=" + kv.Value);
                }
            }
            File.WriteAllLines(ConfigFilePath, lines.ToArray());
        }
    }
}
