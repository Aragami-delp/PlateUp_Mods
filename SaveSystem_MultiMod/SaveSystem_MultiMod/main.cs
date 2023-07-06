using KitchenMods;

using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using Kitchen;
using Kitchen.Modules;
using SaveSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using TMPro;
using Unity.Entities;
using Steamworks;
using System.Windows.Forms;
using Steamworks.Ugc;
using System.Threading.Tasks;
using Kitchen.Components;

namespace SaveSystem_MultiMod
{
    public class SaveSystem_ModLoaderSystem : GenericSystemBase, IModSystem, IModInitializer
    {
        protected override void Initialise()
        {
            if (GameObject.FindObjectOfType<SaveSystemMod>() == null)
            {
                GameObject saveSystemMod = new GameObject("SaveSystem");
                saveSystemMod.AddComponent<SaveSystemMod>();
                GameObject.DontDestroyOnLoad(saveSystemMod);
            }

            LogInfo("Workshop mod: SaveSystem v" + SaveSystemMod.Version + " is loaded!"); // Might be unnecessary for Workshop mods
        }

        protected override void OnUpdate() { }

        public static void LogInfo(string _log) { Debug.Log("[SaveSystem] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning("[SaveSystem] " + _log); }
        public static void LogError(string _log) { Debug.LogError("[SaveSystem] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }

        public void PostActivate(Mod mod)
        {
            Debug.LogWarning("Mod: SaveSystemMod in use!"); // For log file output for official support staff

            CheckForLaunchAllowed();
        }

        public void PreInject()
        {
            // Nope
        }

        public void PostInject()
        {
            // Nope
        }

        private void CheckForLaunchAllowed()
        {
            if (Helper.GetLoadedAssembly("0Harmony") != null) // Should cover Harmony mod as well as HarmonyX (UnityExplorer)
            {
                return;
            }

            LogError("Mod: SaveSystem not loaded since Harmony was not found");

            bool startGame = true;
            if (!SteamUtils.IsSteamInBigPictureMode) // If not SteamDeck (or BigPicture) - might even work on SteamDeck
            {
                DialogResult result = MessageBox.Show("SaveSystem needs Harmony installed to work.\n\nPress \"Yes\" to close the game and install automatically.\nPress \"No\" to close the game and open the workshop page.\nPress \"Cancel\" to start anyways.", "SaveSystem Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    Task.Run(async () => await SubscribeToHarmony()).GetAwaiter().GetResult();
                    startGame = false;
                }
                else if (result == DialogResult.No)
                {
                    UnityEngine.Application.OpenURL(@"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id=2898033283");
                    startGame = false;
                }
                else if (result == DialogResult.Cancel)
                {
                    startGame = true;
                }
            }
            else
            {
                UnityEngine.Application.OpenURL(@"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id=2898033283");
                // startGame = false; // Not sure how it behaves on SteamDeck, so start game anyways
            }
            if (!startGame) { Session.SoftExit(); }
            return;
        }

        private static async Task<int> SubscribeToHarmony()
        {
            Query harmonyItemQuery = Query.ItemsReadyToUse.WithFileId(2898033283);
            ResultPage? ugcResult = await harmonyItemQuery.GetPageAsync(1); // TODO: multiple pages? - how many? - take a look at DependencyChecker - not needed since with this fileid there is always only 1 page
            if (ugcResult != null)
            {
                foreach (Item harmonyItem in ugcResult.Value.Entries)
                {
                    await harmonyItem.Subscribe();
                    harmonyItem.Download(true);
                    return 0;
                }
            }
            return -1;
        }
    }

    public class SaveSystemMod : MonoBehaviour
    {
        public const string Version = "1.4.2";
        private readonly HarmonyLib.Harmony m_harmony = new HarmonyLib.Harmony("com.aragami.plateup.mods.harmony");
        //public static DisplayVersion m_DisplayVersion;
        //public static string m_DisplayVersionDefaultText;
        public static int m_selectedSaveSlot = 1;

        private void Awake()
        {
            m_harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        //public static void UpdateDisplayVersion()
        //{
        //    string extraText = String.Empty;
        //    if (!(bool)SaveSystemManager.Instance.Settings["hidesaveinfo"].GetValue(SaveSetting.SettingType.boolValue))
        //    {
        //        string currentName = SaveSystemManager.Instance.GetCurrentRunName();
        //        extraText = "\n" + (SaveSystemManager.Instance.GetCurrentRunAlreadySaved() ? "Last save at " + SaveSystemManager.Instance.GetSaveEntryForCurrentlyLoadedRun().GetDateTime + ": " : "Unsaved: ") + (String.IsNullOrWhiteSpace(currentName) ? "No save selected" : currentName);
        //    }
        //    m_DisplayVersion.Text.SetText(m_DisplayVersionDefaultText + extraText);
        //}
    }

    #region Add SaveSystem to pause menu
    [HarmonyPatch(typeof(Kitchen.MainMenu), nameof(Kitchen.MainMenu.Setup))]
    public static class MainMenuSetupPatch
    {
        static void Prefix(Kitchen.MainMenu __instance, int player_id)
        {
            if (Session.CurrentGameNetworkMode != GameNetworkMode.Host || (GameInfo.CurrentScene != SceneType.Franchise && GameInfo.CurrentScene != SceneType.Kitchen))
                return;
            MethodInfo m_addButtonMenu = Helper.GetMethod(__instance.GetType(), "AddSubmenuButton");
            m_addButtonMenu.Invoke(__instance, new object[3] { "Save System", typeof(SaveSystemMenu), false });

            //#region BackToLobbyPatch
            //MethodInfo m_addBackToLobbyButton = Helper.GetMethod(__instance.GetType(), "AddSubmenuButton");
            //if (GameInfo.CurrentScene == SceneType.Kitchen)
            //    m_addBackToLobbyButton.Invoke(__instance, new object[3] { "Back to lobby", typeof(SaveSystemBackToLobby), false }); // TODO: Add voting
            //#endregion
        }

        static void BackToLobby(Kitchen.MainMenu __instance)
        {
            Helper.ChangeScene(SceneType.Franchise);
            MethodInfo closeMenuEvent = Helper.GetMethod(typeof(Kitchen.MainMenu), "RequestAction");
            closeMenuEvent.Invoke(__instance, new object[1] { PauseMenuAction.CloseMenu });
        }
    }

    [HarmonyPatch(typeof(PlayerPauseView), "SetupMenus")]
    class PlayerPauseView_Patch
    {
        [HarmonyPrefix]
        static void Prefix(PlayerPauseView __instance)
        {
            ModuleList moduleList = (ModuleList)__instance.GetType().GetField("ModuleList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            MethodInfo mInfo = Helper.GetMethod(__instance.GetType(), "AddMenu");

            mInfo.Invoke(__instance, new object[2] { typeof(SaveSystemMenu), new SaveSystemMenu(__instance.ButtonContainer, moduleList) });
            //mInfo.Invoke(__instance, new object[2] { typeof(SaveSystemBackToLobby), new SaveSystemBackToLobby(__instance.ButtonContainer, moduleList) });
        }
    }
    #endregion

    //#region Delete Remaining SaveFiles On New Kitchen
    //[HarmonyPatch(typeof(CreateNewKitchen), "OnUpdate")]
    //class CreateNewKitchen_Patch
    //{
    //    [HarmonyPostfix]
    //    static void Postfix(CreateNewKitchen __instance, EntityQuery ____SingletonEntityQuery_SCreateScene_29, WorldBackupSystem ___WorldBackup)
    //    {
    //        if (____SingletonEntityQuery_SCreateScene_29.GetSingleton<SCreateScene>().Type != SceneType.Kitchen)
    //            return;
    //        ___WorldBackup.ClearBackup(); //TODO: ClearFullWorldSaves(int slot) delete slot as well
    //    }
    //}
    //#endregion

    #region Get Day Change Info
    //[HarmonyPatch(typeof(FullSaveAtNight), "OnUpdate")]
    //class FullSaveAtNight_Patch // Autosave
    //{
    //    [HarmonyPostfix]
    //    static void Postfix(FullSaveAtNight __instance, EntityQuery ____SingletonEntityQuery_SGameTime_10) // TODO: Save more info for each save
    //    {
    //        Type typeSHasSaved = typeof(FullSaveAtNight).GetNestedTypes(BindingFlags.NonPublic).First();
    //        var mi = typeof(FullSaveAtNight).GetMethod(nameof(FullSaveAtNight.HasSingleton));
    //        var hasSingletonRef = mi.MakeGenericMethod(typeSHasSaved);
    //        bool retVal = (bool)hasSingletonRef.Invoke(__instance, null);
    //        if (__instance.HasSingleton<SIsNightFirstUpdate>() && retVal)
    //        { }
    //        if (retVal || ____SingletonEntityQuery_SGameTime_10.GetSingleton<SGameTime>().IsPaused)
    //            return;
    //        //SaveSystem_ModLoaderSystem.LogError("Performing a full save");
    //        //SaveSystemManager.Instance.SaveCurrentSave();
    //        //SaveSystemMod.UpdateDisplayVersion();

    //    }
    //}
    #endregion

    #region ReworkUI
    //public class SaveSystemBackToLobby : Menu<PauseMenuAction>
    //{
    //    public SaveSystemBackToLobby(Transform container, ModuleList module_list) : base(container, module_list)
    //    {
    //    }

    //    public override void Setup(int player_id)
    //    {
    //        AddLabel(Helper.GetNameplateName);
    //        if (Helper.IsInCardSelection)
    //        {
    //            AddLabel("Please select a card first");
    //        }
    //        else
    //        {
    //            AddLabel("Back to lobby?");
    //            AddButton("Confirm", (Action<int>)(i => ReturnToLobby()));
    //        }
    //        AddButton(Localisation["CANCEL_PROFILE"], (Action<int>)(i => RequestPreviousMenu()));
    //    }

    //    public void ReturnToLobby()
    //    {
    //        SaveSystem_ModLoaderSystem.LogInfo("Saving current run with previouse one.");
    //        SaveSystemManager.Instance.SaveCurrentSave();
    //        SaveSystemMod.UpdateDisplayVersion();
    //        Helper.ChangeScene(SceneType.Franchise);
    //        RequestAction(PauseMenuAction.CloseMenu);
    //    }
    //}

    public class SaveSystemDeleteMenu : Menu<PauseMenuAction>
    {
        public SaveSystemDeleteMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            AddLabel("Delete this save?");
            AddInfo(SaveSystemMenu.currentlySelectedName);
            AddInfo(SaveSystemMenu.m_dicSavesDescription[SaveSystemMenu.currentlySelectedName].DateTime);
            AddInfo(SaveSystemMenu.m_dicSavesDescription[SaveSystemMenu.currentlySelectedName].NameplateName);
            AddButton(Localisation["PROFILE_CONFIRM_DELETE"], (Action<int>)(i => ConfirmDelete()));
            AddButton(Localisation["CANCEL_PROFILE"], (Action<int>)(i => RequestPreviousMenu()));
        }

        public void ConfirmDelete()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Deleting run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.DeleteSave(SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            RequestPreviousMenu();
            //SaveSystemMod.UpdateDisplayVersion();
        }
    }

    public class SaveSystemOptionsMenu : Menu<PauseMenuAction>
    {
        public SaveSystemOptionsMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public Option<bool> HideSaveInfo;
        public Option<bool> ShowSaveMods;

        private Option<bool> GetHideSaveInfoOption() => new Option<bool>(new List<bool>()
        {
            false,
            true
            }, (bool)SaveSystemManager.Instance.Settings["hidesaveinfo"].GetValue(SaveSetting.SettingType.boolValue), new List<string>()
            {
            Localisation["SETTING_DISABLED"],
            Localisation["SETTING_ENABLED"]
        });
        private Option<bool> GetShowSaveModsOption() => new Option<bool>(new List<bool>()
        {
            false,
            true
            }, (bool)SaveSystemManager.Instance.Settings["showsavemods"].GetValue(SaveSetting.SettingType.boolValue), new List<string>()
            {
            Localisation["SETTING_DISABLED"],
            Localisation["SETTING_ENABLED"]
        });

        public override void Setup(int player_id)
        {
            //HideSaveInfo = GetHideSaveInfoOption();
            ShowSaveMods = GetShowSaveModsOption();

            //AddLabel("Hide save info");
            //Add<bool>(this.HideSaveInfo).OnChanged += (EventHandler<bool>)((_, value) =>
            //{
            //    SaveSystemManager.Instance.Settings["hidesaveinfo"].SetValue(value);
            //    SaveSystemManager.Instance.Settings.SaveCurrentSettings();
            //    //SaveSystemMod.UpdateDisplayVersion();
            //});
            //AddInfo("Hides infos about the save state in the bottom right corner of the screen.");
            AddLabel("Show mods of save (out of order)");
            Add<bool>(this.ShowSaveMods).OnChanged += (EventHandler<bool>)((_, value) =>
            {
                SaveSystemManager.Instance.Settings["showsavemods"].SetValue(value); // Gets initiallised when first changing this option, but fine for now
                SaveSystemManager.Instance.Settings.SaveCurrentSettings();
                //SaveSystemMod.UpdateDisplayVersion();
            });
            AddInfo("Turning this on might increase the Menu load times.");
            New<SpacerElement>();
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }
    }

    public class SaveSystemLoadConfirmMenu : Menu<PauseMenuAction>
    {
        public SaveSystemLoadConfirmMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            if (SaveSystemManager.Instance.SlotHasRun(SaveSystemMod.m_selectedSaveSlot) && !SaveSystemManager.Instance.GetCurrentRunAlreadySaved(SaveSystemMod.m_selectedSaveSlot))
            {
                AddLabel("Overwrite currently loaded save in slot " + SaveSystemMod.m_selectedSaveSlot.ToString() + "?");
                AddInfo(SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot));
                AddInfo(SaveSystemManager.Instance.GetCurrentRunDateTime(SaveSystemMod.m_selectedSaveSlot));
                New<SpacerElement>();
            }
            this.AddButton("Confirm loading", (Action<int>)(i => this.LoadAndGoBack()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void LoadAndGoBack() // TODO: NTH - Not reload lobby when loading
        {
            SaveSystem_ModLoaderSystem.LogInfo("Loading run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.LoadSave(SaveSystemMod.m_selectedSaveSlot, SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            this.RequestAction(PauseMenuAction.CloseMenu);
            //SaveSystemMod.UpdateDisplayVersion();
            Helper.ChangeScene(SceneType.Franchise); // Reload scene
        }
    }

    public class SaveSystemMenu : Menu<PauseMenuAction>
    {
        public SaveSystemMenu(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        private static int PlayerID;
        private ButtonElement SaveButton;
        private ButtonElement LoadButton;
        private ButtonElement RenameButton;
        private ButtonElement DeleteButton;
        private Option<string> SaveSelectOption;
        private Option<int> SaveSlotSelection;
        private IModule SaveSelectModule;
        private LabelElement SaveSelectDateTime;
        private LabelElement SaveSelectNameplateName;
        private LabelElement SaveSelectPlayerNames;
        private LabelElement SaveSelectMods;
        public static string currentlySelectedName;
        public static Dictionary<string, SaveSelectDescription> m_dicSavesDescription;

        public override void CreateSubmenus(ref Dictionary<Type, Menu<PauseMenuAction>> menus)
        {
            menus.Add(typeof(SaveSystemDeleteMenu), new SaveSystemDeleteMenu(Container, ModuleList));
            menus.Add(typeof(SaveSystemLoadConfirmMenu), new SaveSystemLoadConfirmMenu(Container, ModuleList));
            menus.Add(typeof(SaveSystemOptionsMenu), new SaveSystemOptionsMenu(Container, ModuleList));
        }

        [Flags]
        public enum ShowUIFlags
        {
            None = 0,
            ShowSaveButton = 1,
            ShowSaveSelection = 2,
            ShowLoadButton = 4,
            ShowRenameButton = 8,
            ShowDeleteButton = 16,
            ShowOptionsButton = 32,
            ShowSlotSelection = 64,
        }

        public override void Setup(int player_id)
        {
            ShowUIFlags showFlags = ShowUIFlags.None;
            #region SaveSelectInit
            if (SaveSystemManager.Instance.HasSavedRuns)
            {
                InitSaveInfo();
                SaveSelectOption.OnChanged += (EventHandler<string>)((_, f) =>
                {
                    currentlySelectedName = f;
                    SaveSelectDateTime.SetLabel(m_dicSavesDescription[currentlySelectedName].DateTime);
                    SaveSelectNameplateName.SetLabel(m_dicSavesDescription[currentlySelectedName].NameplateName);
                    SaveSelectPlayerNames.SetLabel(m_dicSavesDescription[currentlySelectedName].PlayerNamesFormat);
                    if ((bool)SaveSystemManager.Instance.Settings["showsavemods"].GetValue(SaveSetting.SettingType.boolValue))
                        SaveSelectMods.SetLabel(m_dicSavesDescription[currentlySelectedName].ModsFormat);
                    SetLoadButtonText();
                });
            }
            // Not in InitSaveInfo, since it needs to always happen
            SaveSlotSelection = new Option<int>(new List<int>() { 1, 2, 3, 4, 5 }, SaveSystemMod.m_selectedSaveSlot, SlotSelection); // NTH: Same selection for saves if within a limit of save amounts

            SaveSlotSelection.OnChanged += (_, newSlot) =>
            {
                ChangeSlot(newSlot, player_id);
                //SetSaveInfoAccordingToSlot();
            };
            #endregion

            //AddLabel("Save System");
            if (GameInfo.CurrentScene == SceneType.Kitchen)
            {
                showFlags |= ShowUIFlags.ShowSaveButton;
            }
            else
            {
                showFlags |= ShowUIFlags.ShowSlotSelection;
                showFlags |= ShowUIFlags.ShowSaveButton;
                if (/*GameInfo.CurrentScene != SceneType.Kitchen && */SaveSystemManager.Instance.HasSavedRuns)
                {
                    showFlags |= ShowUIFlags.ShowSaveSelection;
                    showFlags |= ShowUIFlags.ShowLoadButton;
                    showFlags |= ShowUIFlags.ShowRenameButton;
                    showFlags |= ShowUIFlags.ShowDeleteButton;
                }
                showFlags |= ShowUIFlags.ShowOptionsButton;
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowSlotSelection))
            {
                AddLabel("Select Slot to modify");
                AddSelect(SaveSlotSelection);
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowSaveButton))
            {
                AddSaveButton(player_id);
                New<SpacerElement>();
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowSaveSelection))
            {
                SaveSelectModule = AddSelect<string>(SaveSelectOption);
                SaveSelectDateTime = AddLabel(m_dicSavesDescription[currentlySelectedName].DateTime);
                SaveSelectNameplateName = AddLabel(m_dicSavesDescription[currentlySelectedName].NameplateName);
                SaveSelectPlayerNames = AddInfo(m_dicSavesDescription[currentlySelectedName].PlayerNamesFormat);
                if ((bool)SaveSystemManager.Instance.Settings["showsavemods"].GetValue(SaveSetting.SettingType.boolValue))
                    SaveSelectMods = AddInfo(m_dicSavesDescription[currentlySelectedName].ModsFormat);
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowLoadButton))
            {
                LoadButton = AddButton("", (Action<int>)(_ =>
                {
                    RequestSubMenu(typeof(SaveSystemLoadConfirmMenu));
                }));
                SetLoadButtonText();
                New<SpacerElement>();
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowRenameButton))
            {
                RenameButton = AddButton("Rename", (Action<int>)(_ => // TODO: same name not allowed - fixed? by saving using timestamp (name may be same still)
                {
                    PlayerID = player_id;
                    TextInputView.RequestTextInput("Enter new name:", currentlySelectedName, 30, new Action<TextInputView.TextInputState, string>(RenameRun));
                    this.RequestAction(PauseMenuAction.CloseMenu);
                }));
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowDeleteButton))
            {
                DeleteButton = AddButton("Delete", (Action<int>)(_ =>
                {
                    RequestSubMenu(typeof(SaveSystemDeleteMenu));
                }));
                New<SpacerElement>();
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowOptionsButton))
            {
                AddButton("Options", (Action<int>)(_ =>
                {
                    RequestSubMenu(typeof(SaveSystemOptionsMenu));
                }));
                New<SpacerElement>();
            }

            AddButton(this.Localisation["MENU_BACK_SETTINGS"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        private static int GetContextSaveSlot
        {
            get
            {
                if (GameInfo.CurrentScene == SceneType.Kitchen)
                    return new EntityContext(World.DefaultGameObjectInjectionWorld.EntityManager).Get<SSelectedLocation>().Selected.Slot;
                else
                    return SaveSystemMod.m_selectedSaveSlot;
            }
        }

        private void AddSaveButton(int _playerID)
        {
            if (SaveButton == null)
            {
                SaveButton = AddButton("Save now", (Action<int>)(_ =>
                {
                    if (!SaveSystemManager.Instance.GetCurrentRunAlreadySaved(GetContextSaveSlot))
                    {
                        PlayerID = _playerID;
                        if (!SaveSystemManager.Instance.GetCurrentRunHasPreviousSaves(GetContextSaveSlot))
                        {
                            string NameplateName = Helper.GetNameplateName; // returns string.empty when not possible (for example in lobby)
                            TextInputView.RequestTextInput("Enter save name:", !string.IsNullOrWhiteSpace(NameplateName) ? NameplateName : string.Empty, 30, new Action<TextInputView.TextInputState, string>(SaveRun));
                            //SaveSystemMod.UpdateDisplayVersion();
                        }
                        else
                        {
                            SaveRun();
                            //SaveSystemMod.UpdateDisplayVersion();
                        }
                        this.RequestAction(PauseMenuAction.CloseMenu);
                        //SaveSystemMod.UpdateDisplayVersion();
                    }
                }));
            }
            if (!SaveSystemManager.Instance.GetCurrentRunAlreadySaved(GetContextSaveSlot))
            {
                SaveButton.SetLabel("Save now");
            }
            else
            {
                SaveButton.SetLabel("Already saved");
            }
        }

        //private void SetSaveInfoAccordingToSlot()
        //{
        //    List<string> saveNames = SaveSystemManager.Instance.GetSaveNamesList();
        //    if (SaveSelectOption.TryGetChosen(out string _saveName))
        //    {
        //        string preselectedName = SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot) != null ? SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot) : _saveName;
        //        SaveSelectOption.SetChosen(SaveSelectOption.GetBestIndex(preselectedName));
        //    }
        //}

        private void InitSaveInfo()
        {
            List<string> saveNames = SaveSystemManager.Instance.GetSaveNamesList();
            string preselectedName = SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot) != null ? SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot) : saveNames[0];
            currentlySelectedName = preselectedName;
            List<string> saveDisplayNames = SaveSystemManager.Instance.GetSaveDisplayNamesList();
            m_dicSavesDescription = saveNames.Zip(SaveSystemManager.Instance.GetDescriptionList(), (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            SaveSelectOption = new Option<string>(saveNames, preselectedName, saveDisplayNames);
        }

        private void ChangeSlot(int _slot, int _playerID)
        {
            SaveSystem_ModLoaderSystem.LogInfo("Changing selected save slot to: " + _slot.ToString());
            SaveSystemMod.m_selectedSaveSlot = _slot;
            AddSaveButton(_playerID);
        }

        private static List<string> SlotSelection
        {
            get
            {
                List<string> stringList = new List<string>();
                for (int i = 0; i < 5; ++i)
                {
                    string str1 = string.Concat(Enumerable.Repeat<string>("<sprite name=\"pip_filled\"> ", i + 1));
                    string str2 = string.Concat(Enumerable.Repeat<string>("<sprite name=\"pip_empty\"> ", 5 - i - 1));
                    stringList.Add("<size=1.7><voffset=-0.5><mspace=1em>" + str1 + str2);
                }
                return stringList;
            }
        }

        private void SetLoadButtonText()
        {
            if (SaveSystemManager.Instance.GetCurrentRunName(SaveSystemMod.m_selectedSaveSlot) == currentlySelectedName)
            {
                LoadButton?.SetLabel("Already loaded");
            }
            LoadButton?.SetLabel("Load");
        }

        private void ReloadMenu(IModule _selectThis)
        {
            ModuleList.Clear();
            Setup(PlayerID);
            if (_selectThis != null)
                ModuleList.Select(_selectThis);
        }

        public void SaveRun()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run with previouse one.");
            SaveSystemManager.Instance.SaveCurrentSave(GetContextSaveSlot);
            //SaveSystemMod.UpdateDisplayVersion();
            ReloadMenu(SaveButton);
        }

        public void SaveRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete && _result != TextInputView.TextInputState.TextEntryCancelled)
                return;
            if (SaveSystemManager.Instance.SaveAlreadyExists(_name))
            {
                SaveSystem_ModLoaderSystem.LogWarning("Save: " + _name + " already exists!\n skipping saving.");
                ReloadMenu(SaveButton);
                return;
            }
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run: " + _name);
            SaveSystemManager.Instance.SaveCurrentSave(GetContextSaveSlot, _name);
            //SaveSystemMod.UpdateDisplayVersion();
            ReloadMenu(SaveButton);
        }

        public void RenameRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete && _result != TextInputView.TextInputState.TextEntryCancelled)
                return;
            SaveSystem_ModLoaderSystem.LogInfo("Renaming current run: " + currentlySelectedName + " to: " + _name);
            SaveSystemManager.Instance.RenameSave(currentlySelectedName, _name);
            RequestSubMenu(this.GetType(), true); // Doesnt work for some reason
            //SaveSystemMod.UpdateDisplayVersion();
        }
    }
    #endregion

    //[HarmonyPatch(typeof(DisplayVersion), "Awake")]
    //public static class DisplayVersionPatch
    //{
    //    // ReSharper disable once UnusedMember.Local
    //    static void Postfix(ref DisplayVersion __instance)
    //    {
    //        if (Session.CurrentGameNetworkMode != GameNetworkMode.Host/* || GameInfo.CurrentScene != SceneType.Franchise*/)
    //            return;
    //        SaveSystemMod.m_DisplayVersion = __instance;
    //        SaveSystemMod.m_DisplayVersionDefaultText = __instance.Text.text;
    //        SaveSystemMod.UpdateDisplayVersion();
    //    }
    //}
}
