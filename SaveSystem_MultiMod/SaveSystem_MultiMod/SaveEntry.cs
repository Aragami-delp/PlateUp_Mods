﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SaveSystem
{
    public class SaveEntry
    {
        public string FolderName;
        public string Name;
        public string NameplateName;
        public List<long> Mods = new List<long>();
        public List<string> PlayerNames = new List<string>();
        [JsonIgnore] private readonly List<uint> Previous_Save_IDs = new List<uint>();
        public SaveEntry(string _folderName, string _name, string _nameplateName, List<string> _playerNames, List<long> _mods)
        {
            FolderName = _folderName;
            Name = _name;
            NameplateName = _nameplateName;
            PlayerNames = _playerNames;
            Mods = _mods;
            RefreshPreviousIDs();
        }

        [JsonIgnore] public string FolderPath => SaveSystemManager.Instance.SaveFolderPath + "/" + FolderName;
        [JsonIgnore] public uint NewestID => Previous_Save_IDs.Max();
        [JsonIgnore] public string NewsetFilePath => FolderPath + "/" + NewestID + ".plateupsave";
        public void ChangeName(string _newName) { Name = _newName; }
        public bool HasID(uint _id) { return Previous_Save_IDs.Contains(_id); }
        [JsonIgnore] public bool HasSaves => Previous_Save_IDs.Count > 0;
        [JsonIgnore] public string GetDisplayName => SaveSystemManager.IsUnixTimestamp(Name) ? SaveSystemManager.UnixTimeToLocalDateTimeFormat(Name) : Name;
        [JsonIgnore] public string GetDateTime => SaveSystemManager.UnixTimeToLocalDateTimeFormat(NewestID);
        [JsonIgnore] public string GetNameplateName => !string.IsNullOrWhiteSpace(NameplateName) ? NameplateName : string.Empty;

        public void RefreshPreviousIDs()
        {
            try
            {
                Previous_Save_IDs.Clear();
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
}