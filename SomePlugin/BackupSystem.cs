using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BackupSystem
{
    public List<SaveFolder> m_saves;
    private string m_gameDataPath;

    public void Init()
    {
        m_gameDataPath = Application.persistentDataPath /*+ "\\It's Happening\\PlateUp"*/;
        foreach (string folder in LoadSaveNames())
        {
            Debug.Log(folder);
        }
        //LoadSaveFolders();
    }

    public void SelectSaveFolder(SaveFolder _selected)
    {
        // TODO: Load save list into game
    }

    //public void LoadSaveFolders() {
    //    if (Directory.Exists(m_gameDataPath + "\\Full") && Directory.GetFiles(m_gameDataPath + "\\Full").Length >= 0)
    //    foreach(string filePath in Directory.GetFiles("{Application.persistentDataPath.SaveFiles}\\It's Happening\\PlateUp")) // TODO: Find correct path of backups
    //        AddSaveFolder(filePath);
    //    m_saves.Sort();
    //}

    private string[] LoadSaveNames()
    {
        if (Directory.Exists(m_gameDataPath + "/SaveSystem"))
        {
            return Directory.GetDirectories(m_gameDataPath + "/SaveSystem");
        }
        return new string[0];
    }

    private void AddSaveFolder(string _filePath)
    {
        //if (File.Exists(path))
        //{
        //    m_saves.Add(new SaveFolder(_filePath));
        //}
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