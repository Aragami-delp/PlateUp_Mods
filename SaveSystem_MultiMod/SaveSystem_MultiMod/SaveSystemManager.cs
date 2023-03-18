using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Kitchen;
using SaveSystem_SteamWorkshop;
using SaveSystem_MultiMod;
using System.Reflection;
using System.Xml.Linq;

namespace SaveSystem
{
    public class SaveSystemManager
    {
        private static SaveSystemManager instance;
        public static SaveSystemManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                instance = new SaveSystemManager();
                instance.Init();
                return instance;
            }
        }

        public List<SaveEntry> Saves = new List<SaveEntry>();
        public string SaveFolderPath { get; private set; }
        private readonly string GameSaveFolderPath = Application.persistentDataPath + "/Full";
        private string GameSaveFolderPathSlot(int _slot) => GameSaveFolderPath + "/" + _slot.ToString();
        public SaveSettingManager Settings;

        private void Init()
        {
            SaveFolderPath = Application.persistentDataPath + "/SaveSystem";
            LoadCurrentSetup();
            AddUntrackedSaves();
            RemoveEmptySaveEntrys();
            SaveCurrentSetup();

            Settings = new SaveSettingManager(SaveFolderPath);
        }

        /// <summary>
        /// Whether the currently loaded run is already saved in the SaveSystem
        /// </summary>
        public bool GetCurrentRunAlreadySaved(int _slot)
        {
            if (GetLoadedSaveID(_slot, out uint _id))
            {
                SaveEntry tmp = GetSaveEntryForCurrentlyLoadedRun(_slot);
                if (tmp != null)
                {
                    return tmp.HasID(_id);
                }
            }
            return false;
        }

        public bool GetCurrentRunHasPreviousSaves(int _slot)
        {
            return GetSaveEntryForCurrentlyLoadedRun(_slot) != null;
        }

        /// <summary>
        /// Name of the current run; if no run currently loaded its null
        /// </summary>
        public string GetCurrentRunName(int _slot)
        {
            return GetSaveEntryForCurrentlyLoadedRun(_slot)?.Name;
        }

        /// <summary>
        /// Localized DateTime string representation of unix timestamp of current run; if no run currently loaded its null
        /// </summary>
        public string GetCurrentRunDateTime(int _slot)
        {
            return GetSaveEntryForCurrentlyLoadedRun(_slot)?.GetDateTime;
        }

        public bool SaveAlreadyExists(params string[] _wantedNames)
        {
            foreach (SaveEntry saveEntry in Saves)
            {
                foreach (string _wantedName in _wantedNames)
                {
                    if (_wantedName == saveEntry.Name || _wantedName == saveEntry.FolderName)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the ID of the currently loaded run
        /// </summary>
        /// <param name="_result">ID of the run</param>
        /// <returns>Whether there is currently a run</returns>
        private bool GetLoadedSaveID(int _slot, out uint _result)
        {
            bool retVal = GetRunUnixUIntAtPath(GameSaveFolderPathSlot(_slot), out uint result);
            _result = result;
            return retVal;
        }

        /// <summary>
        /// Checks whether the given timestamp is a vaild unix timestamp
        /// </summary>
        /// <param name="_timestamp">Timestamp to check</param>
        /// <returns>True if valid, otherwise false</returns>
        public static bool IsUnixTimestamp(string _timestamp)
        {
            return Regex.IsMatch(_timestamp, "^[0-9]+$"); // Is unix timestamp
        }

        /// <summary>
        /// Converts a (string) unix timestamp into a localized string representation
        /// </summary>
        /// <param name="_text">Timestamp to convert</param>
        /// <returns>Localized timestamp</returns>
        public static string UnixTimeToLocalDateTimeFormat(string _text)
        {
            DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            double seconds = double.Parse(_text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(seconds).ToLocalTime().ToString(formatInfo);
        }

        /// <summary>
        /// Converts a unix timestamp into a localized string representation
        /// </summary>
        /// <param name="_id">Timestamp to convert</param>
        /// <returns>Localized timestamp</returns>
        public static string UnixTimeToLocalDateTimeFormat(uint _id)
        {
            DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            return Epoch.AddSeconds(_id).ToLocalTime().ToString(formatInfo);
        }

        /// <summary>
        /// Gets the newest save file at the given path
        /// </summary>
        /// <param name="_path">Path to search for save files</param>
        /// <param name="_result">ID of the run</param>
        /// <returns>Whether a run was found</returns>
        public static bool GetRunUnixUIntAtPath(string _path, out uint _result)
        {
            List<string> foundFullPathNames;
            _result = 0;
            if (Directory.Exists(_path))
            {
                foundFullPathNames = Directory.GetFiles(_path, "*.plateupsave").ToList();
            }
            else
            {
                return false;
            }
            List<string> foundNames = new List<string>();
            foreach (string fullpath in foundFullPathNames)
            {
                foundNames.Add(Path.GetFileNameWithoutExtension(fullpath));
            }
            List<uint> convertedNames = new List<uint>();
            foreach (string name in foundNames)
            {
                if (Regex.IsMatch(name, "^[0-9]+$"))
                {
                    convertedNames.Add(uint.Parse(name));
                }
            }
            if (convertedNames.Count > 0)
            {
                _result = convertedNames.Max();
                return true;
            }
            return false;
        }

        public List<string> GetCurrentPlayerNames => Players.Main.All().Select(x => x.Name).ToList();

        /// <summary>
        /// Saves the currently loaded save or creates a new SaveEntry if there is no exisitng save yet
        /// </summary>
        /// <param name="_name">Name of new save; If adding to existing save, this will be ignored</param>
        /// <returns>True if already saved or already exisiting identical save id; Otherwise false</returns>
        public bool SaveCurrentSave(int _slot, string _name = null)
        {
            if (GetLoadedSaveID(_slot, out uint _currentLoadedID)) // if any run loaded
            {
                SaveEntry save = GetSaveEntryForCurrentlyLoadedRun(_slot);
                if (save != null)
                {
                    if (save.NewestID == _currentLoadedID)
                    {
                        SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogInfo("Save: " + save.GetDisplayName + " already exists!\nskipping saving.");
                        return true;
                    }

                    File.Copy(GameSaveFolderPathSlot(_slot) + "/" + _currentLoadedID.ToString() + ".plateupsave", save.FolderPath + "/" + _currentLoadedID.ToString() + ".plateupsave");
                    save.RefreshPreviousIDs();
                    string NameplateName = Helper.GetNameplateName; // TODO: Cant be done in lobby
                    save.NameplateName = !string.IsNullOrWhiteSpace(NameplateName) ? NameplateName : string.Empty;
                    save.PlayerNames = GetCurrentPlayerNames;
                    save.Mods = SteamWorkshopModManager.GetCurrentWorkshopIDs;
                    SaveCurrentSetup();
                    return true;
                }
                else if (_name != null)
                {
                    // Test for duplicate name - already tested in main, just to be sure
                    string newFolderName = _currentLoadedID.ToString();
                    if (SaveAlreadyExists(newFolderName, _name))
                    {
                        SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning("Save: " + _name + " already exists!\nskipping saving. This should be a duplicate warning");
                        return false;
                    }
                    Directory.CreateDirectory(SaveFolderPath + "/" + newFolderName);
                    string NameplateName = Helper.GetNameplateName;
                    SaveEntry newSaveEntry = new SaveEntry(newFolderName, _name, !string.IsNullOrWhiteSpace(NameplateName) ? NameplateName : string.Empty, GetCurrentPlayerNames, SaveSystem_SteamWorkshop.SteamWorkshopModManager.GetCurrentWorkshopIDs);
                    File.Copy(GameSaveFolderPathSlot(_slot) + "/" + _currentLoadedID.ToString() + ".plateupsave", newSaveEntry.FolderPath + "/" + _currentLoadedID.ToString() + ".plateupsave");
                    newSaveEntry.RefreshPreviousIDs();
                    Saves.Add(newSaveEntry);
                    SaveCurrentSetup();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds the SaveEntry that is currently associated with the current run that is in the game, if available
        /// </summary>
        /// <returns>SaveEntry associated with the current run, otherwise null</returns>
        public SaveEntry GetSaveEntryForCurrentlyLoadedRun(int _slot)
        {
            string[] currentFiles = Directory.GetFiles(GameSaveFolderPathSlot(_slot), "*.plateupsave");
            foreach (SaveEntry save in Saves)
            {
                foreach (string currentFile in currentFiles)
                {
                    if (save.HasID(uint.Parse(Path.GetFileNameWithoutExtension(currentFile))))
                    {
                        return save;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Saves the currently knowm SaveEntry's to a JSON
        /// </summary>
        private void SaveCurrentSetup()
        {
            string currentJson = JsonConvert.SerializeObject(Saves, Formatting.Indented);
            File.WriteAllText(SaveFolderPath + "/SaveInfo.json", currentJson);
        }

        /// <summary>
        /// Loads the currently knowm SaveEntry's from a JSON
        /// </summary>
        private void LoadCurrentSetup()
        {
            try
            {
                try
                {
                    string text = System.IO.File.ReadAllText(SaveFolderPath + "/SaveInfo.json");
                    Saves = JsonConvert.DeserializeObject<List<SaveEntry>>(text);
                }
                catch (FileNotFoundException _fileEx)
                {
                    SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning("No info file to load, probably started for the first time.");
                    Saves = new List<SaveEntry>();
                }
            }
            catch (Exception _e)
            {
                SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning(_e.Message);
            }
        }

        /// <summary>
        /// Loads a specific save and removes all already existing previously loaded run files
        /// </summary>
        /// <param name="_name">Save to load</param>
        public void LoadSave(int _slot, string _name)
        {
            // TODO: Check for already loaded
            foreach (SaveEntry saveEntry in Saves)
            {
                if (!saveEntry.HasSaves) continue;
                if (saveEntry.Name == _name)
                {
                    // TODO: maybe put all removed files inside a "deleted" folder which get cleaned up on game start
                    RemoveAllFiles(GameSaveFolderPathSlot(_slot), false); // TODO: Persistence.ClearSaves<FullWorldSaveSystem>();
                    //Type type = typeof(Persistence);
                    //PropertyInfo info = type.GetProperty("WorldBackup", BindingFlags.NonPublic | BindingFlags.Static);
                    //WorldBackupSystem value = (WorldBackupSystem)info.GetValue(null);
                    //value.ClearBackup();
                    try
                    {
                        File.Copy(saveEntry.NewsetFilePath, Path.Combine(GameSaveFolderPathSlot(_slot), Path.GetFileName(saveEntry.NewsetFilePath)));
                    }
                    catch (DirectoryNotFoundException _dirEx)
                    {
                        SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogError("There was never a run on this local machine, do a run for an entire day first.");
                    }
                }
            }
        }

        /// <summary>
        /// Deletes a save (Entry and files)
        /// </summary>
        /// <param name="_name">Save to delete</param>
        public void DeleteSave(string _name)
        {
            SaveEntry entryToRemove = null;
            foreach (SaveEntry saveEntry in Saves)
            {
                if (saveEntry.Name == _name)
                {
                    // TODO: maybe put all removed files inside a "deleted" folder which get cleaned up on game start
                    RemoveAllFiles(saveEntry.FolderPath);
                    entryToRemove = saveEntry;
                }
            }
            Saves.Remove(entryToRemove);
            SaveCurrentSetup();
        }

        /// <summary>
        /// Removes all files at a given path including the directory itself
        /// </summary>
        /// <param name="_path">Path to delete files from</param>
        private static void RemoveAllFiles(string _path, bool _folderAsWell = true)
        {
            try
            {
                if (_folderAsWell)
                    Directory.Delete(_path, true);
                else
                {
                    foreach (string deleteFile in Directory.GetFiles(_path))
                    {
                        File.Delete(deleteFile);
                    }
                }
            }
            catch (DirectoryNotFoundException _dirEx)
            {
                SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogError($"Can't find Path: {_path} to delete");
            }
        }

        /// <summary>
        /// Renames a save
        /// </summary>
        /// <param name="_oldName">Save to rename</param>
        /// <param name="_newName">New name for save</param>
        public void RenameSave(string _oldName, string _newName) // TODO: Also rename folder, so a new save can have its name
        {
            foreach (SaveEntry save in Saves)
            {
                if (save.Name == _oldName)
                {
                    save.ChangeName(_newName);
                    SaveCurrentSetup();
                    return;
                }
            }
        }

        /// <summary>
        /// Adds untracked save files to the system
        /// </summary>
        private void AddUntrackedSaves()
        {
            if (!Directory.Exists(SaveFolderPath))
                Directory.CreateDirectory(SaveFolderPath);
            List<string> allSaveFolderNames = new List<string>();
            foreach (string d in Directory.GetDirectories(SaveFolderPath))
            {
                allSaveFolderNames.Add(new DirectoryInfo(d).Name);
            }
            foreach (SaveEntry saveEntry in Saves)
            {
                if (allSaveFolderNames.Contains(saveEntry.FolderName))
                {
                    allSaveFolderNames.Remove(saveEntry.FolderName);
                }
            }
            foreach (string untrackedSaves in allSaveFolderNames)
            {
                Saves.Add(new SaveEntry(untrackedSaves, untrackedSaves, String.Empty, new List<string>(0), new List<long>(0)));
            }
        }

        /// <summary>
        /// Removes SaveEntrys that have not save files
        /// </summary>
        private void RemoveEmptySaveEntrys()
        {
            List<SaveEntry> emptyEntrys = new List<SaveEntry>();
            foreach (SaveEntry saveEntry in Saves)
            {
                saveEntry.RefreshPreviousIDs();
                if (!saveEntry.HasSaves)
                {
                    emptyEntrys.Add(saveEntry);
                }
            }
            foreach (SaveEntry emptyEntry in emptyEntrys)
            {
                RemoveAllFiles(emptyEntry.FolderPath);
                Saves.Remove(emptyEntry);
            }
        }

        /// <summary>
        /// List of all names of SaveEntrys
        /// </summary>
        /// <returns></returns>
        public List<string> GetSaveNamesList()
        {
            List<string> saveNames = new List<string>();
            foreach (SaveEntry saveEntry in Saves)
            {
                saveNames.Add(saveEntry.Name);
            }
            return saveNames;
        }
        /// <summary>
        /// List of all display names of SaveEntrys;
        /// </summary>
        /// <returns>List of names, each unix name will be converted into a localized Time/Date string</returns>
        public List<string> GetSaveDisplayNamesList()
        {
            List<string> saveDisplayNames = new List<string>();
            foreach (SaveEntry saveEntry in Saves)
            {
                saveDisplayNames.Add(saveEntry.GetDisplayName);
            }
            return saveDisplayNames;
        }
        /// <summary>
        /// List of all display names of SaveEntrys;
        /// </summary>
        /// <returns>List of DateTime localized string representation, each with its newset timestamp</returns>
        public List<SaveSelectDescription> GetDescriptionList()
        {
            List<SaveSelectDescription> saveDescriptionNames = new List<SaveSelectDescription>();
            foreach (SaveEntry saveEntry in Saves)
            {
                saveDescriptionNames.Add(new SaveSelectDescription(
                    saveEntry.GetDateTime,
                    saveEntry.GetNameplateName,
                    (saveEntry.PlayerNames?.Count > 0)
                    ? saveEntry.PlayerNames
                    : new List<string>(),
                    ((bool)SaveSystemManager.Instance.Settings["showsavemods"].GetValue(SaveSetting.SettingType.boolValue)
                        && saveEntry.Mods != null
                        && saveEntry.Mods.Count > 0)
                    ? SteamWorkshopModManager.GetWorkshopNames(saveEntry.Mods)
                    : new List<string>()));
            }
            return saveDescriptionNames;
        }
        /// <summary>
        /// Whether there are any runs in the SaveSystem saved
        /// </summary>
        public bool HasSavedRuns => Saves.Count > 0;
    }

    public struct SaveSelectDescription
    {
        public string DateTime;
        public string NameplateName;
        private List<string> PlayerNames;
        public string PlayerNamesFormat => PlayerNames.Count > 0 ? string.Join(", ", PlayerNames.ToArray()) : string.Empty;
        private List<string> Mods;
        public string ModsFormat => Mods.Count > 0 ? string.Join(", ", Mods.ToArray()) : string.Empty;

        public SaveSelectDescription(string _dateTime, string _nameplateName, List<string> _playerNames, List<string> _mods)
        {
            DateTime = _dateTime;
            NameplateName = _nameplateName;
            PlayerNames = _playerNames;
            Mods = _mods;
        }
    }
}
