using BepInEx;
using UnityEngine;
using System.IO;
using KitchenLib;

namespace FBX_Test
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FBX_Test_BepInEx : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                GameObject prefab = LoadFromFileExample.GetStreamingAssetGameObject();
                Instantiate(prefab);
            }
        }
    }

    public static class LoadFromFileExample
    {
        private static AssetBundle cucumberAssetBundle;
        public static GameObject GetStreamingAssetGameObject()
        {
            if (cucumberAssetBundle == null)
                cucumberAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "provider_-_cucumber"));
            if (cucumberAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return null;
            }
            return cucumberAssetBundle.LoadAsset<GameObject>("provider_-_cucumber");
        }
    }
}
