using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BackupSystem : Monobehaviour{
    public List<SaveFile> m_saves;

    private void Start() {
        foreach(string filePath in Directory.GetFiles(Application.persistentDataPath.SaveFiles)) // TODO: Find correct path
        {
            AddSaveFile(filePath);
        }
    }

    public void AddSaveFile(string _filePath) {
        if (File.Exists(path))
        {
            m_save.Add(new SaveFile(_filePath));
        }
    }
}

public class SaveFile : IComparable<SaveFile> {
    public string name;
    public DateTime modifiedTime;
    
    public int CompareTo(SaveFile _other){
        return modifiedTime.CompareTo(_other.modifiedTime);
    }

    public SaveFile(string _saveFile) {
        if (!File.Exists(path))
            throw new ArgumentNullException();
        name = Path.GetFileName(_saveFile)
        DateTime = File.GetLastWriteTime(_saveFile);
    }
}