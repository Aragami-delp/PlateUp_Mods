using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace SaveSystem
{
    public class SaveSettingManager
    {
        List<SaveSetting> persistentSettings = new List<SaveSetting>();
        private string settingsPath;

        public SaveSettingManager(string _settingsPath)
        {
            settingsPath = _settingsPath;

            LoadCurrentSettings();
            SaveCurrentSettings();
        }

        public SaveSetting this[string i]
        {
            get
            {
                foreach (SaveSetting setting in persistentSettings)
                {
                    if (setting.Name == i)
                    {
                        return setting;
                    }
                }
                SaveSetting newS = new SaveSetting(i, null);
                persistentSettings.Add(newS);
                return newS;
            }
        }

        public void SaveCurrentSettings()
        {
            string currentJson = JsonConvert.SerializeObject(persistentSettings, Formatting.Indented);
            File.WriteAllText(settingsPath + "/Settings.json", currentJson);
        }

        /// <summary>
        /// Loads the settings from a JSON
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                try
                {
                    string text = System.IO.File.ReadAllText(settingsPath + "/Settings.json");
                    persistentSettings = JsonConvert.DeserializeObject<List<SaveSetting>>(text);
                }
                catch (FileNotFoundException _fileEx)
                {
                    SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning("No setting file to load, probably started for the first time.");
                    persistentSettings = new List<SaveSetting>();
                }
            }
            catch (Exception _e)
            {
                SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning(_e.Message);
            }
        }
    }
}
