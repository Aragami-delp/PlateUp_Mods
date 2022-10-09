using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using SomePlugin;

public static class BackupSystem
{
    public static List<string> SaveFileNames = new List<string>();
    public static List<string> SaveFileDisplayNames = new List<string>();
    public static string SelectedSaveSlotName = null;

    public static void ReloadSaveFileNames()
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
            if (Regex.IsMatch(name, "^[0-9]+$")) // Is unix timestamp
            {
                SaveFileNames.Add(name);
                DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
                string convertedName = UnixTimeToDateTime(name).ToLocalTime().ToString(formatInfo);
                SaveFileDisplayNames.Add(convertedName);
            }
        }
    }

    private static DateTime UnixTimeToDateTime(string text)
    {
        DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double seconds = double.Parse(text, CultureInfo.InvariantCulture);
        return Epoch.AddSeconds(seconds);
    }

    public static void LoadSaveSlot()
    {
        if (!String.IsNullOrEmpty(SelectedSaveSlotName) && SelectedSaveSlotName != GetCurrentRunName())
        {

        }
        else
            throw new DirectoryNotFoundException("Can't find backup save");
    }

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

    public static bool CurrentSaveExists
    {
        get
        {
            return Directory.Exists(Application.persistentDataPath + "\\SaveSystem\\" + GetCurrentRunName());
        }
    }

    public static string GetCurrentRunName()
    {
        List<string> foundFullPathNames = new List<string>();
        if (Directory.Exists(Application.persistentDataPath + "\\Full"))
        {
            foundFullPathNames = Directory.GetFiles(Application.persistentDataPath + "\\Full").ToList();
        }
        List<string> foundNames = new List<string>();
        foreach (string fullpath in foundFullPathNames)
        {
            foundNames.Add(Path.GetFileNameWithoutExtension(fullpath));
        }
        List<long> convertedNames = new List<long>(); // Int is prob enough, but to be sure
        foreach (string name in foundNames)
        {
            if (Regex.IsMatch(name, "^[0-9]+$")) // Is unix timestamp
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