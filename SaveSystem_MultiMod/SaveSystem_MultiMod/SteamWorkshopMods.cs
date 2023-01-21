using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Ugc;
using Steamworks;

namespace SaveSystem_SteamWorkshop
{
    public static class SteamWorkshopModManager
    {
        private static List<Item> m_workshopMods = new List<Item>();

        public static List<string> GetCurrentWorkshopNames
        {
            get
            {
                return GetCurrentWorkshopMods().Select(o => o.Title).ToList();
            }
        }

        #region GetWorkshopNames
        private static async Task<List<string>> GetWorkshopNamesTask(params long[] _iDs)
        {
            List<ulong> paramValues = _iDs.Select(o => (ulong)o).ToList();
            List<Item> retVal = new List<Item>();
            List<Steamworks.Data.PublishedFileId> workshopIds = new List<Steamworks.Data.PublishedFileId>();
            foreach (ulong i in paramValues)
            {
                Steamworks.Data.PublishedFileId publishedId = new Steamworks.Data.PublishedFileId();
                publishedId.Value = i;
                workshopIds.Add(publishedId);
            }
            Query subedItemsQuery = Query.Items.WithFileId(workshopIds.ToArray());
            ResultPage? ugcResult = await subedItemsQuery.GetPageAsync(1); // TODO: multiple pages? - how many?
            if (ugcResult != null)
            {
                foreach (Item workshopItem in ugcResult.Value.Entries)
                {
                    retVal.Add(workshopItem);
                }
            }
            return retVal.Select(o => o.Title).ToList();
        }
        public static List<string> GetWorkshopNames(List<long> _iDs)
        {
            return Task.Run(async () => await GetWorkshopNamesTask(_iDs.ToArray())).Result;
        }
        #endregion

        public static List<long> GetCurrentWorkshopIDs
        {
            get
            {
                return GetCurrentWorkshopMods().Select(o => (long)o.Id.Value).ToList();
            }
        }

        private static List<Item> GetCurrentWorkshopMods()
        {
            UpdateModsList();
            return m_workshopMods;
        }

        private static async void UpdateModsList()
        {
            if (m_workshopMods.Count == 0) // Don't update after the first execution (in case the user subscribes to mods while the game is opened)
            {
                Query subedItemsQuery = Query.ItemsReadyToUse.WhereUserSubscribed(SteamClient.SteamId.AccountId);
                ResultPage? ugcResult = await subedItemsQuery.GetPageAsync(1); // TODO: multiple pages? - how many?
                if (ugcResult != null)
                {
                    foreach (Item workshopItem in ugcResult.Value.Entries)
                    {
                        m_workshopMods.Add(workshopItem);
                    }
                }
            }
        }
    }
}
