using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SaveSystem
{
    public static class BackupSystem
    {
        /// <summary>
        /// Currently selected save slot
        /// </summary>
        public static string SelectedSaveSlotUnixName = null;
        /// <summary>
        /// Save file names, (UnixTimestamp, SaveName)
        /// </summary>
        public static Dictionary<string, string> SaveFileNames = new Dictionary<string, string>();
        public static string SelectedSaveSlotDisplayName
        {
            get
            {
                try
                {
                    string save;
                    if (SaveFileNames.ContainsKey(GetCurrentRunUnixName()))
                    {
                        save = SaveFileNames[GetCurrentRunUnixName()];
                        if (IsUnixTimestamp(save))
                            return UnixTimeToLocalDateTimeFormat(save);
                        else
                            return save;
                    }
                    return "No save selected";
                } catch { return "No save selected"; }
            }
        }

        /// <summary>
        /// Reloads the entire SaveSystem including the lists of save files currently backed up
        /// </summary>
        public static void ReloadSaveSystem()
        {
            // TODO: Create SaveSystem folder if not there yet
            if (!Directory.Exists(Application.persistentDataPath + "\\SaveSystem"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "\\SaveSystem\\");
            }
            SaveFileNames.Clear();
            List<string> foundFullPathes = new List<string>();
            if (Directory.Exists(Application.persistentDataPath + "\\SaveSystem")) // remove? - it should always exist
            {
                foundFullPathes = Directory.GetDirectories(Application.persistentDataPath + "\\SaveSystem").ToList();
            }
            List<string> foundNames = new List<string>();
            foreach (string fullpath in foundFullPathes)
            {
                foundNames.Add(Path.GetFileName(fullpath));
            }
            foreach (string name in foundNames)
            {
                try
                {
                    if (IsUnixTimestamp(name))
                    {
                        Debug.Log("0 " + name);
                        SaveFileNames.Add(name, name);
                    }
                    else
                    {
                        string namePath = Application.persistentDataPath + "\\SaveSystem\\" + name;
                        if (Directory.GetFiles(namePath).Length > 0)
                        {
                            string unixName = GetRunUnixNameAtPath(namePath);
                            if (!String.IsNullOrEmpty(unixName))
                            {
                                Debug.Log("1 " + unixName + " " + name);
                                SaveFileNames.Add(unixName, name);
                            }
                        }
                    }
                }
                catch (ArgumentException _e)
                {
                    // Save loaded twice
                    // TODO: Log this properly ^^
                    Debug.LogWarning("Save " + (IsUnixTimestamp(name) ? UnixTimeToLocalDateTimeFormat(name) : name) + " duplicated");
                }
            }
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
        /// Loads the currently selected save slot
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">Thrown if selected save slot doesn't exists</exception>
        public static void LoadSaveSlot()
        {
            if (!String.IsNullOrEmpty(SelectedSaveSlotUnixName) && !CurrentSelectionLoaded)
            {
                if (CurrentlyAnyRunLoaded && !CurrentSaveExists)
                {
                    BackupCurrentRun();
                }

                string[] removeFiles = Directory.GetFiles(Application.persistentDataPath + "\\Full", "*.plateupsave");
                foreach (string removeFile in removeFiles)
                {
                    File.Delete(removeFile);
                }

                string path = Application.persistentDataPath + "\\SaveSystem\\" + SaveFileNames[SelectedSaveSlotUnixName];
                string[] currentFiles = Directory.GetFiles(path, "*.plateupsave");
                foreach (string saveFile in currentFiles)
                {
                    File.Copy(saveFile, Path.Combine(Application.persistentDataPath + "\\Full", Path.GetFileName(saveFile)));
                }
            }
            else
                throw new DirectoryNotFoundException("Can't find backup save");
        }

        /// <summary>
        /// Backs up the current run into a backup folder
        /// </summary>
        public static void BackupCurrentRun()
        {
            if (!Directory.Exists(Application.persistentDataPath + "\\SaveSystemBackup"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "\\SaveSystemBackup\\");
            }

            string[] removeFiles = Directory.GetFiles(Application.persistentDataPath + "\\SaveSystemBackup", "*.plateupsave");
            foreach (string removeFile in removeFiles)
            {
                File.Delete(removeFile);
            }

            string newestSaveFileName = GetRunUnixNameAtPath(Application.persistentDataPath + "\\Full");
            File.Copy(Application.persistentDataPath + "\\Full\\" + newestSaveFileName + ".plateupsave", Application.persistentDataPath + "\\SaveSystemBackup\\" + newestSaveFileName + ".plateupsave");
        }

        /// <summary>
        /// Makes a backup into the SaveSystem of the currently loaded run
        /// </summary>
        /// <param name="_name">Name of the new save, empty will save with the unix timestamp as name</param>
        /// <param name="_saveAllFiles">If all run days should be saved or just the latest</param>
        public static void SaveCurrentRun(string _name = "", bool _saveAllFiles = false)
        {
            // TODO: handle override - currently it just overrides all
            if (!CurrentSaveExists)
            {
                string newDirectoryName = Application.persistentDataPath + "\\SaveSystem\\" + (String.IsNullOrWhiteSpace(_name) ? GetCurrentRunUnixName() : _name);
                Directory.CreateDirectory(newDirectoryName);

                if (_saveAllFiles)
                {
                    string[] currentFiles = Directory.GetFiles(Application.persistentDataPath + "\\Full", "*.plateupsave");
                    foreach (string saveFile in currentFiles)
                    {
                        File.Copy(saveFile, Path.Combine(newDirectoryName, Path.GetFileName(saveFile)), true);
                    }
                }
                else
                {
                    string newestSaveFileName = GetRunUnixNameAtPath(Application.persistentDataPath + "\\Full");
                    File.Copy(Application.persistentDataPath + "\\Full\\" + newestSaveFileName + ".plateupsave", newDirectoryName + "\\" + newestSaveFileName + ".plateupsave");
                }
            }
        }

        public static void DeleteSaveSlot()
        {
            if (!String.IsNullOrEmpty(SelectedSaveSlotUnixName))
            {
                string path = Application.persistentDataPath + "\\SaveSystem\\" + SaveFileNames[SelectedSaveSlotUnixName];
                Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Whether the current selection is already loaded into the game
        /// </summary>
        public static bool CurrentSelectionLoaded
        {
            get
            {
                return CurrentlyAnyRunLoaded && (SelectedSaveSlotUnixName == GetCurrentRunUnixName());
            }
        }

        /// <summary>
        /// Wheather the current run already got a backup in the SaveSystem
        /// </summary>
        public static bool CurrentSaveExists
        {
            get
            {
                if (!CurrentlyAnyRunLoaded)
                    return false;
                if (SaveFileNames.ContainsKey(GetCurrentRunUnixName()))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Wheather there is already a run loaded in/by the game
        /// </summary>
        public static bool CurrentlyAnyRunLoaded
        {
            get
            {
                return !String.IsNullOrEmpty(GetCurrentRunUnixName());
            }
        }

        /// <summary>
        /// Gets the name of the currently loaded run
        /// </summary>
        /// <returns>Name of the newset savefile of the currently loaded run</returns>
        public static string GetCurrentRunUnixName()
        {
            return GetRunUnixNameAtPath(Application.persistentDataPath + "\\Full");
        }

        /// <summary>
        /// Gets the newest save file at the given path
        /// </summary>
        /// <param name="_path">Path to search for save files</param>
        /// <returns>Newest save file if any, otherwise String.Empty</returns>
        public static string GetRunUnixNameAtPath(string _path)
        {
            List<string> foundFullPathNames = new List<string>();
            if (Directory.Exists(_path))
            {
                foundFullPathNames = Directory.GetFiles(_path, "*.plateupsave").ToList();
            }
            else
            {
                return String.Empty;
            }
            List<string> foundNames = new List<string>();
            foreach (string fullpath in foundFullPathNames)
            {
                foundNames.Add(Path.GetFileNameWithoutExtension(fullpath));
            }
            List<long> convertedNames = new List<long>(); // Int is prob enough, but to be sure since thats what the game uses
            foreach (string name in foundNames)
            {
                if (IsUnixTimestamp(name))
                {
                    convertedNames.Add(long.Parse(name));
                }
            }
            if (convertedNames.Count > 0)
            {
                return convertedNames.Max().ToString(); // Convert twice, but so be it
            }
            return String.Empty;
        }
    }
}