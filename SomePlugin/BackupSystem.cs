using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BackupSystem : Monobehaviour {
    public List<SaveFile> m_saves;

    private void Start() {
        LoadSaveFiles()
    }

    public void SelectSaveFile(SaveFile _selected) {
        // TODO: Load save into game
    }

    public void LoadSaveFiles() {
        foreach(string filePath in Directory.GetFiles(Application.persistentDataPath.SaveFiles)) // TODO: Find correct path
        {
            AddSaveFile(filePath);
        }
        m_save.Sort();
    }

    private void AddSaveFile(string _filePath) {
        if (File.Exists(path))
        {
            m_save.Add(new SaveFile(_filePath));
        }
    }
}

public class SaveFile : IComparable<SaveFile> {
    public readonly string name;
    public readonly DateTime modifiedTime;
    
    public override int CompareTo(SaveFile _other) {
        return modifiedTime.CompareTo(_other.modifiedTime);
    }

    public SaveFile(string _saveFile) {
        if (!File.Exists(path))
            throw new ArgumentNullException();
        name = Path.GetFileName(_saveFile);
        DateTime = File.GetLastWriteTime(_saveFile);
    }
}