using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SaveSystem
{
    public class SaveEntry
    {
        public string FolderName;
        public string Name;
        private List<uint> Previous_Save_IDs = new List<uint>();

        [JsonConstructor]
        public SaveEntry(string _folderName, string _name)
        {
            FolderName = _folderName;
            Name = _name;
            RefreshPreviousIDs();
        }

        public string FolderPath => SaveSystemManager.Instance.SaveFolderPath + "\\" + FolderName;
        public uint NewestID => Previous_Save_IDs.Max();
        public string NewsetFilePath => FolderPath + "\\" + NewestID + ".plateupsave";
        public void ChangeName(string _newName) { Name = _newName; }
        public bool HasID(uint _id) { return Previous_Save_IDs.Contains(_id); }
        public bool HasSaves => Previous_Save_IDs.Count > 0;
        public string GetDisplayName => SaveSystemManager.IsUnixTimestamp(Name) ? SaveSystemManager.UnixTimeToLocalDateTimeFormat(Name) : Name;

        public void RefreshPreviousIDs()
        {
            Previous_Save_IDs.Clear();
            try
            {
                foreach (string potentialFile in Directory.GetFiles(FolderPath, "*.plateupsave"))
                {
                    if (uint.TryParse(Path.GetFileNameWithoutExtension(potentialFile), out uint _res))
                    {
                        Previous_Save_IDs.Add(_res);
                    }
                }
            }

            catch (DirectoryNotFoundException _dirEx)
            {
                SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogError($"Folder for Save {Name} not found");
            }
        }
    }

    public class SaveSystemManager
    {
        private static SaveSystemManager instance;
        public static SaveSystemManager Instance
        {
            get
            {
                return instance ?? (instance = new SaveSystemManager());
            }
        }

        public List<SaveEntry> Saves = new List<SaveEntry>();
        public string SaveFolderPath { get; private set; }
        private readonly string GameSaveFolderPath = Application.persistentDataPath + "\\Full";

        private SaveSystemManager(string _saveFolderPath = null)
        {
            if (string.IsNullOrWhiteSpace(_saveFolderPath)) { SaveFolderPath = Application.persistentDataPath + "\\SaveSystem"; }
            LoadCurrentSetup();
            AddUntrackedSaves();
            RemoveEmptySaveEntrys();
            SaveCurrentSetup();
        }

        /// <summary>
        /// Gets the ID of the currently loaded run
        /// </summary>
        /// <param name="_result">ID of the run</param>
        /// <returns>Whether there is currently a run</returns>
        private bool GetLoadedSaveID(out uint _result)
        {
            bool retVal = GetRunUnixUIntAtPath(GameSaveFolderPath, out uint result);
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
        /// <param name="text">Timestamp to convert</param>
        /// <returns>Localized timestamp</returns>
        public static string UnixTimeToLocalDateTimeFormat(string text)
        {
            DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(seconds).ToLocalTime().ToString(formatInfo);
        }

        /// <summary>
        /// Gets the newest save file at the given path
        /// </summary>
        /// <param name="_path">Path to search for save files</param>
        /// <param name="_result">ID of the run</param>
        /// <returns>Whether a run was found</returns>
        public static bool GetRunUnixUIntAtPath(string _path, out uint _result)
        {
            List<string> foundFullPathNames = new List<string>();
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

        /// <summary>
        /// Gets the name of the currently loaded run
        /// </summary>
        /// <returns>Name of the run</returns>
        public string GetLoadedSaveName()
        {
            return GetSaveEntryForCurrentlyLoadedRun().Name;
        }
        /// <summary>
        /// Gets the name of the currenlty loaded run, ID-timestamp converted to localized dateTime string representation
        /// </summary>
        /// <returns>Name of the run; if ID -> then as localized string</returns>
        public string GetLoadedSaveDisplayName()
        {
            return GetSaveEntryForCurrentlyLoadedRun().GetDisplayName;
        }

        /// <summary>
        /// Saves the currently loaded save or creates a new SaveEntry if there is no exisitng save yet
        /// </summary>
        /// <param name="_name">Name of new save; If adding to existing save, this will be ignored</param>
        public void SaveCurrentSave(string _name = null)
        {
            if (GetLoadedSaveID(out uint _currentLoadedID)) // if any run loaded
            {
                SaveEntry save = GetSaveEntryForCurrentlyLoadedRun();
                if (save != null)
                {
                    File.Copy(GameSaveFolderPath + "\\" + _currentLoadedID.ToString() + ".plateupsave", save.FolderPath + "\\" + _currentLoadedID.ToString() + ".plateupsave");
                    save.RefreshPreviousIDs();
                    SaveCurrentSetup();
                    return;
                }
                else if (_name != null)
                {
                    Directory.CreateDirectory(SaveFolderPath + "\\" + _currentLoadedID.ToString());
                    SaveEntry newSaveEntry = new SaveEntry(_currentLoadedID.ToString(), string.IsNullOrWhiteSpace(_name) ? _currentLoadedID.ToString() : _name);
                    File.Copy(GameSaveFolderPath + "\\" + _currentLoadedID.ToString() + ".plateupsave", save.FolderPath + "\\" + _currentLoadedID.ToString() + ".plateupsave");
                    save.RefreshPreviousIDs();
                    Saves.Add(newSaveEntry);
                    SaveCurrentSetup();
                }
            }
        }

        /// <summary>
        /// Finds the SaveEntry that is currently associated with the current run that is in the game, if available
        /// </summary>
        /// <returns>SaveEntry associated with the current run, otherwise null</returns>
        public SaveEntry GetSaveEntryForCurrentlyLoadedRun()
        {
            string[] currentFiles = Directory.GetFiles(GameSaveFolderPath, "*.plateupsave");
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
            string currentJson = JsonConvert.SerializeObject(Saves);
            File.WriteAllText(SaveFolderPath + "\\SaveInfo.json", currentJson);
        }

        /// <summary>
        /// Loads the currently knowm SaveEntry's from a JSON
        /// </summary>
        private void LoadCurrentSetup()
        {
            try
            {
                string text = System.IO.File.ReadAllText(SaveFolderPath + "\\SaveInfo.json");
                Saves = JsonConvert.DeserializeObject<List<SaveEntry>>(text);
            }
            catch (FileNotFoundException _fileEx)
            {
                SaveSystem_MultiMod.SaveSystem_ModLoaderSystem.LogWarning("No info file to load, probably started for the first time.");
                Saves = new List<SaveEntry>();
            }
        }

        /// <summary>
        /// Loads a specific save and removes all already existing previously loaded run files
        /// </summary>
        /// <param name="_name">Save to load</param>
        public void LoadSave(string _name)
        {
            foreach (SaveEntry saveEntry in Saves)
            {
                if (!saveEntry.HasSaves) continue;
                if (saveEntry.Name == _name)
                {
                    // TODO: maybe put all removed files inside a "deleted" folder which get cleaned up on game start
                    RemoveAllFiles(GameSaveFolderPath);
                    File.Copy(saveEntry.NewsetFilePath, Path.Combine(GameSaveFolderPath, Path.GetFileName(saveEntry.NewsetFilePath)));
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
        private void RemoveAllFiles(string _path)
        {
            Directory.Delete(_path, true);
        }

        /// <summary>
        /// Renames a save
        /// </summary>
        /// <param name="_oldName">Save to rename</param>
        /// <param name="_newName">New name for save</param>
        public void RenameSave(string _oldName, string _newName)
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
            List<string> allSaveFolderPaths = new List<string>();
            allSaveFolderPaths = Directory.GetDirectories(SaveFolderPath).ToList();
            foreach (SaveEntry saveEntry in Saves)
            {
                if (allSaveFolderPaths.Contains(saveEntry.FolderPath))
                {
                    allSaveFolderPaths.Remove(saveEntry.FolderPath);
                }
            }
            foreach (string untrackedSaves in allSaveFolderPaths)
            {
                Saves.Add(new SaveEntry(untrackedSaves, untrackedSaves));
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
                if (!saveEntry.HasSaves)
                {
                    emptyEntrys.Add(saveEntry);
                }
            }
            foreach (SaveEntry emptyEntry in emptyEntrys)
            {
                Directory.Delete(emptyEntry.FolderPath, true);
                Saves.Remove(emptyEntry);
            }
        }
    }
}
