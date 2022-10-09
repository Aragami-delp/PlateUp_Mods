using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using SaveSystem;

public static class BackupSystem
{
    /// <summary>
    /// List of the names of all save folders
    /// </summary>
    public static List<string> SaveFileNames = new List<string>();
    /// <summary>
    /// List of representation of the names of all save folders - localized to the local time format
    /// </summary>
    public static List<string> SaveFileDisplayNames = new List<string>();
    /// <summary>
    /// Currently selected save slot
    /// </summary>
    public static string SelectedSaveSlotName = null;

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
        SaveFileDisplayNames.Clear();
        List<string> foundFullPathes = new List<string>();
        if (Directory.Exists(Application.persistentDataPath + "\\SaveSystem"))
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
            if (IsUnixTimestamp(name))
            {
                SaveFileNames.Add(name);
                SaveFileDisplayNames.Add(UnixTimeToLocalDateTimeFormat(name));
            }
        }
    }

    /// <summary>
    /// Checks whether the given timestamp is a vaild unix timestamp
    /// </summary>
    /// <param name="_timestamp">Timestamp to check</param>
    /// <returns>True if valid, otherwise false</returns>
    private static bool IsUnixTimestamp(string _timestamp)
    {
        return Regex.IsMatch(_timestamp, "^[0-9]+$"); // Is unix timestamp
    }

    /// <summary>
    /// Converts a (string) unix timestamp into a localized string representation
    /// </summary>
    /// <param name="text">Timestamp to convert</param>
    /// <returns>Localized timestamp</returns>
    private static string UnixTimeToLocalDateTimeFormat(string text)
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
        if (!String.IsNullOrEmpty(SelectedSaveSlotName) && !CurrentSelectionLoaded)
        {
            // TODO: Backup current run

            string[] removeFiles = Directory.GetFiles(Application.persistentDataPath + "\\Full", "*.plateupsave");
            foreach (string removeFile in removeFiles)
            {
                File.Delete(removeFile);
            }

            string[] currentFiles = Directory.GetFiles(Application.persistentDataPath + "\\SaveSystem\\" + SelectedSaveSlotName, "*.plateupsave");
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
    /// <exception cref="NotImplementedException">TODO</exception>
    public static void BackupCurrentRun()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Makes a backup into the SaveSystem of the currently loaded run
    /// </summary>
    public static void SaveCurrentRun()
    {
        if (!CurrentSaveExists)
        {
            Directory.CreateDirectory(Application.persistentDataPath + "\\SaveSystem\\" + GetCurrentRunName());
            string[] currentFiles = Directory.GetFiles(Application.persistentDataPath + "\\Full", "*.plateupsave");

            foreach (string saveFile in currentFiles)
            {
                File.Copy(saveFile, Path.Combine(Application.persistentDataPath + "\\SaveSystem\\" + GetCurrentRunName(), Path.GetFileName(saveFile)));
            }
        }
    }

    /// <summary>
    /// Whether the current selection is already loading into the game
    /// </summary>
    public static bool CurrentSelectionLoaded
    {
        get
        {
            return (!String.IsNullOrEmpty(GetCurrentRunName())) && (SelectedSaveSlotName == GetCurrentRunName());
        }
    }

    /// <summary>
    /// Wheather the current selection already got a backup in the SaveSystem
    /// </summary>
    public static bool CurrentSaveExists
    {
        get
        {
            if (String.IsNullOrEmpty(GetCurrentRunName()))
                return false;
            return Directory.Exists(Application.persistentDataPath + "\\SaveSystem\\" + GetCurrentRunName());
        }
    }

    /// <summary>
    /// Wheather there is already a run loaded in/by the game
    /// </summary>
    public static bool CurrentlyAnyRunLoaded
    {
        get
        {
            return !String.IsNullOrEmpty(GetCurrentRunName());
        }
    }

    /// <summary>
    /// Gets the name of the currently loaded run
    /// </summary>
    /// <returns>Name of the newset savefile of the currently loaded run</returns>
    public static string GetCurrentRunName()
    {
        return GetRunNameAtPath(Application.persistentDataPath + "\\Full");
    }

    /// <summary>
    /// Gets the newest save file at the given path
    /// </summary>
    /// <param name="_path">Path to search for save files</param>
    /// <returns>Newest save file if any, otherwise String.Empty</returns>
    public static string GetRunNameAtPath(string _path)
    {
        List<string> foundFullPathNames = new List<string>();
        if (Directory.Exists(_path))
        {
            foundFullPathNames = Directory.GetFiles(_path, "*.plateupsave").ToList();
        }
        List<string> foundNames = new List<string>();
        foreach (string fullpath in foundFullPathNames)
        {
            foundNames.Add(Path.GetFileNameWithoutExtension(fullpath));
        }
        List<long> convertedNames = new List<long>(); // Int is prob enough, but to be sure
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