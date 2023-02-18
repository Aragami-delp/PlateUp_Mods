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
            ResultPage? ugcResult = await harmonyItemQuery.GetPageAsync(1); // TODO: multiple pages? - how many?
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
        public const string Version = "1.3.14";
        private readonly HarmonyLib.Harmony m_harmony = new HarmonyLib.Harmony("com.aragami.plateup.mods.harmony");
        public static DisplayVersion m_DisplayVersion;
        public static string m_DisplayVersionDefaultText;

        private void Awake()
        {
            m_harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UpdateDisplayVersion()
        {
            string extraText = String.Empty;
            if (!(bool)SaveSystemManager.Instance.Settings["hidesaveinfo"].GetValue(SaveSetting.SettingType.boolValue))
            {
                string currentName = SaveSystemManager.Instance.CurrentRunName;
                extraText = "\n" + (SaveSystemManager.Instance.CurrentRunAlreadySaved ? "Last save at " + SaveSystemManager.Instance.GetSaveEntryForCurrentlyLoadedRun().GetDateTime + ": " : "Unsaved: ") + (String.IsNullOrWhiteSpace(currentName) ? "No save selected" : currentName);
            }
            m_DisplayVersion.Text.SetText(m_DisplayVersionDefaultText + extraText);
        }
    }
    #region Reflection/Helper
    public static class Helper
    {
        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that doesn't have parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
        public static MethodInfo GetMethod(Type _typeOfOriginal, string _name, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that has Parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_paramTypes">Types of parameters of the Method in right order</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
        public static MethodInfo GetMethod(Type _typeOfOriginal, string _name, Type[] _paramTypes, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Instance, null, _paramTypes, null);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        /// <summary>
        /// Gets a MethodInfo of a given class using Reflection, that has Parameters
        /// </summary>
        /// <param name="_typeOfOriginal">Type of class to find a Method on</param>
        /// <param name="_name">Name of the Method to find</param>
        /// <param name="_paramTypes">Types of parameters of the Method in right order</param>
        /// <param name="_genericT">Type of Method</param>
        /// <returns>MethodInfo if found</returns>
        public static MethodInfo GetStaticMethod(Type _typeOfOriginal, string _name, Type[] _paramTypes, Type _genericT = null)
        {
            MethodInfo retVal = _typeOfOriginal.GetMethod(_name, BindingFlags.Static, null, _paramTypes, null);
            if (_genericT != null)
            {
                retVal = retVal.MakeGenericMethod(_genericT);
            }
            return retVal;
        }

        public static string SanitizeUserInput(string _input)
        {
            if (!string.IsNullOrWhiteSpace(_input))
            {
                string s = string.Join("_", _input.Split(Path.GetInvalidFileNameChars()));
                return string.Join("_", s.Split(Path.GetInvalidPathChars()));
            }
            return _input;
        }

        public static void ChangeScene(SceneType _next)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity entity = entityManager.CreateEntity((ComponentType)typeof(SPerformSceneTransition), (ComponentType)typeof(CDoNotPersist));
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.AddComponentData<SPerformSceneTransition>(entity, new SPerformSceneTransition()
            {
                NextScene = _next
            });
        }

        //  http://www.java2s.com/Code/CSharp/Reflection/Getsanassemblybyitsnameifitiscurrentlyloaded.htm
        /// <summary>
        /// Gets an assembly by its name if it is currently loaded
        /// </summary>
        /// <param name="Name">Name of the assembly to return</param>
        /// <returns>The assembly specified if it exists, otherwise it returns null</returns>
        public static System.Reflection.Assembly GetLoadedAssembly(string Name)
        {
            try
            {
                foreach (Assembly TempAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (TempAssembly.GetName().Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return TempAssembly;
                    }
                }
                return null;
            }
            catch { throw; }
        }
    }
    #endregion

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

            #region BackToLobbyPatch
            MethodInfo m_addBackToLobbyButton = Helper.GetMethod(__instance.GetType(), "AddSubmenuButton");
            if (GameInfo.CurrentScene == SceneType.Kitchen)
                m_addBackToLobbyButton.Invoke(__instance, new object[3] { "Back to lobby", typeof(SaveSystemBackToLobby), false }); // TODO: Add voting
            #endregion
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
            mInfo.Invoke(__instance, new object[2] { typeof(SaveSystemBackToLobby), new SaveSystemBackToLobby(__instance.ButtonContainer, moduleList) });
        }
    }
    #endregion

    #region Delete Remaining SaveFiles On New Kitchen
    [HarmonyPatch(typeof(CreateNewKitchen), "OnUpdate")]
    class CreateNewKitchen_Patch
    {
        [HarmonyPostfix]
        static void Postfix(CreateNewKitchen __instance, EntityQuery ____SingletonEntityQuery_SCreateScene_29)
        {
            if (____SingletonEntityQuery_SCreateScene_29.GetSingleton<SCreateScene>().Type != SceneType.Kitchen)
                return;
            Persistence.ClearSaves<FullWorldSaveSystem>();
        }
    }
    #endregion

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
    public class SaveSystemBackToLobby : Menu<PauseMenuAction>
    {
        public SaveSystemBackToLobby(Transform container, ModuleList module_list) : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
            AddLabel("Back to lobby?");
            this.AddButton("Confirm", (Action<int>)(i => this.ReturnToLobby()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void ReturnToLobby()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Saving current run with previouse one.");
            SaveSystemManager.Instance.SaveCurrentSave();
            SaveSystemMod.UpdateDisplayVersion();
            Helper.ChangeScene(SceneType.Franchise);
            RequestAction(PauseMenuAction.CloseMenu);
        }
    }

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
            this.AddButton(this.Localisation["PROFILE_CONFIRM_DELETE"], (Action<int>)(i => this.ConfirmDelete()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void ConfirmDelete()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Deleting run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.DeleteSave(SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            this.RequestPreviousMenu();
            SaveSystemMod.UpdateDisplayVersion();
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
            this.Localisation["SETTING_DISABLED"],
            this.Localisation["SETTING_ENABLED"]
        });
        private Option<bool> GetShowSaveModsOption() => new Option<bool>(new List<bool>()
        {
            false,
            true
            }, (bool)SaveSystemManager.Instance.Settings["showsavemods"].GetValue(SaveSetting.SettingType.boolValue), new List<string>()
            {
            this.Localisation["SETTING_DISABLED"],
            this.Localisation["SETTING_ENABLED"]
        });

        public override void Setup(int player_id)
        {
            HideSaveInfo = GetHideSaveInfoOption();
            ShowSaveMods = GetShowSaveModsOption();

            AddLabel("Hide save info");
            Add<bool>(this.HideSaveInfo).OnChanged += (EventHandler<bool>)((_, value) =>
            {
                SaveSystemManager.Instance.Settings["hidesaveinfo"].SetValue(value);
                SaveSystemManager.Instance.Settings.SaveCurrentSettings();
                SaveSystemMod.UpdateDisplayVersion();
            });
            AddInfo("Hides infos about the save state in the bottom right corner of the screen.");
            AddLabel("Show mods of save");
            Add<bool>(this.ShowSaveMods).OnChanged += (EventHandler<bool>)((_, value) =>
            {
                SaveSystemManager.Instance.Settings["showsavemods"].SetValue(value); // Gets initiallised when first changing this option, but fine for now
                SaveSystemManager.Instance.Settings.SaveCurrentSettings();
                SaveSystemMod.UpdateDisplayVersion();
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
            if (!SaveSystemManager.Instance.CurrentRunAlreadySaved)
            {
                AddLabel("Overwrite currently loaded save?");
                AddInfo(SaveSystemManager.Instance.CurrentRunName);
                AddInfo(SaveSystemManager.Instance.CurrentRunDateTime);
                New<SpacerElement>();
            }
            this.AddButton("Confirm loading", (Action<int>)(i => this.LoadAndGoBack()));
            this.AddButton(this.Localisation["CANCEL_PROFILE"], (Action<int>)(i => this.RequestPreviousMenu()));
        }

        public void LoadAndGoBack()
        {
            SaveSystem_ModLoaderSystem.LogInfo("Loading run: " + SaveSystemMenu.currentlySelectedName);
            SaveSystemManager.Instance.LoadSave(SaveSystemMenu.currentlySelectedName);
            SaveSystemMenu.currentlySelectedName = null;
            this.RequestAction(PauseMenuAction.CloseMenu);
            SaveSystemMod.UpdateDisplayVersion();
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
            ShowSelection = 2,
            ShowLoadButton = 4,
            ShowRenameButton = 8,
            ShowDeleteButton = 16,
            ShowOptionsButton = 32,
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
            #endregion

            //AddLabel("Save System");
            if (GameInfo.CurrentScene == SceneType.Kitchen)
            {
                showFlags |= ShowUIFlags.ShowSaveButton;
            }
            else
            {
                if (/*GameInfo.CurrentScene != SceneType.Kitchen && */SaveSystemManager.Instance.HasSavedRuns)
                {
                    showFlags |= ShowUIFlags.ShowSelection;
                    showFlags |= ShowUIFlags.ShowLoadButton;
                    showFlags |= ShowUIFlags.ShowRenameButton;
                    showFlags |= ShowUIFlags.ShowDeleteButton;
                    showFlags |= ShowUIFlags.ShowOptionsButton;
                }
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowSaveButton))
            {
                if (SaveSystemManager.Instance.CurrentRunAlreadySaved)
                {
                    SaveButton = AddButton("Already saved", (Action<int>)(_ =>
                    {

                    }));
                }
                else
                {
                    SaveButton = AddButton("Save now", (Action<int>)(_ =>
                    {
                        PlayerID = player_id;
                        if (!SaveSystemManager.Instance.CurrentRunHasPreviousSaves)
                        {
                            TextInputView.RequestTextInput("Enter save name:", /*TODO: Preset with franchise name*/"", 30, new Action<TextInputView.TextInputState, string>(SaveRun));
                            SaveSystemMod.UpdateDisplayVersion();
                        }
                        else
                        {
                            SaveRun();
                            SaveSystemMod.UpdateDisplayVersion();
                        }
                        this.RequestAction(PauseMenuAction.CloseMenu);
                        SaveSystemMod.UpdateDisplayVersion();
                    }));
                }
            }
            if (showFlags.HasFlag(ShowUIFlags.ShowSelection))
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
                RenameButton = AddButton("Rename", (Action<int>)(_ => // TODO: same name not allowed
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

        private void InitSaveInfo()
        {
            List<string> saveNames = SaveSystemManager.Instance.GetSaveNamesList();
            string preselectedName = SaveSystemManager.Instance.CurrentRunName != null ? SaveSystemManager.Instance.CurrentRunName : saveNames[0];
            currentlySelectedName = preselectedName;
            List<string> saveDisplayNames = SaveSystemManager.Instance.GetSaveDisplayNamesList();
            m_dicSavesDescription = saveNames.Zip(SaveSystemManager.Instance.GetDescriptionList(), (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            SaveSelectOption = new Option<string>(saveNames, preselectedName, saveDisplayNames);
        }

        private void SetLoadButtonText()
        {
            if (SaveSystemManager.Instance.CurrentRunName == currentlySelectedName)
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
            SaveSystemManager.Instance.SaveCurrentSave();
            SaveSystemMod.UpdateDisplayVersion();
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
            SaveSystemManager.Instance.SaveCurrentSave(_name);
            SaveSystemMod.UpdateDisplayVersion();
            ReloadMenu(SaveButton);
        }

        public void RenameRun(TextInputView.TextInputState _result, string _name)
        {
            if (_result != TextInputView.TextInputState.TextEntryComplete && _result != TextInputView.TextInputState.TextEntryCancelled)
                return;
            SaveSystem_ModLoaderSystem.LogInfo("Renaming current run: " + currentlySelectedName + " to: " + _name);
            SaveSystemManager.Instance.RenameSave(currentlySelectedName, _name);
            RequestSubMenu(this.GetType(), true); // Doesnt work for some reason
            SaveSystemMod.UpdateDisplayVersion();
        }
    }
    #endregion

    [HarmonyPatch(typeof(DisplayVersion), "Awake")]
    public static class DisplayVersionPatch
    {
        // ReSharper disable once UnusedMember.Local
        static void Postfix(ref DisplayVersion __instance)
        {
            if (Session.CurrentGameNetworkMode != GameNetworkMode.Host/* || GameInfo.CurrentScene != SceneType.Franchise*/)
                return;
            SaveSystemMod.m_DisplayVersion = __instance;
            SaveSystemMod.m_DisplayVersionDefaultText = __instance.Text.text;
            SaveSystemMod.UpdateDisplayVersion();
        }
    }
}
