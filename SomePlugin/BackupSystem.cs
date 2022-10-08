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
    public static List<SaveFolder> m_saves;

    //public static void Init()
    //{
    //    m_gameDataPath = Application.persistentDataPath /*+ "\\It's Happening\\PlateUp"*/;
    //    foreach (string folder in LoadSaveNames())
    //    {
    //        Debug.Log(folder);
    //    }
    //    //LoadSaveFolders();
    //}

    //public static void SelectSaveFolder(SaveFolder _selected)
    //{
    //    // TODO: Load save list into game
    //}

    //public void LoadSaveFolders() {
    //    if (Directory.Exists(m_gameDataPath + "\\Full") && Directory.GetFiles(m_gameDataPath + "\\Full").Length >= 0)
    //    foreach(string filePath in Directory.GetFiles("{Application.persistentDataPath.SaveFiles}\\It's Happening\\PlateUp")) // TODO: Find correct path of backups
    //        AddSaveFolder(filePath);
    //    m_saves.Sort();
    //}

    //private static string[] LoadSaveNames()
    //{
    //    if (Directory.Exists(m_gameDataPath + "/SaveSystem"))
    //    {
    //        return Directory.GetDirectories(m_gameDataPath + "/SaveSystem");
    //    }
    //    return new string[0];
    //}

    //private static void AddSaveFolder(string _filePath)
    //{
    //    //if (File.Exists(path))
    //    //{
    //    //    m_saves.Add(new SaveFolder(_filePath));
    //    //}
    //}

    //public static List<string> GetSaveFileNames()
    //{
    //    if (Directory.Exists(m_gameDataPath + "/SaveSystem"))
    //    {
    //        return Directory.GetDirectories(m_gameDataPath + "/SaveSystem").ToList();
    //    }
    //    return new List<string>();
    //}

    public static List<string> SaveFileNames = new List<string>();
    public static List<string> SaveFileDisplayNames = new List<string>();

    public static void ReloadSaveFileNames()
    {
        SaveFileNames.Clear();
        SaveFileDisplayNames.Clear();
        List<string> foundFullPathes = new List<string>();
        if (Directory.Exists(Application.persistentDataPath + "\\SaveSystem"))
        {
            foundFullPathes= Directory.GetDirectories(Application.persistentDataPath + "\\SaveSystem").ToList();
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
}

public class SaveFolder : IComparable<SaveFolder>
{
    public readonly string name;
    public readonly DateTime modifiedTime;

    public int CompareTo(SaveFolder _other)
    {
        return modifiedTime.CompareTo(_other.modifiedTime);
    }

    public SaveFolder(string _saveFolder)
    {
        //if (!File.Exists(path))
        //    throw new ArgumentNullException();
        name = Path.GetFileName(_saveFolder);
        modifiedTime = File.GetLastWriteTime(_saveFolder);
    }
}