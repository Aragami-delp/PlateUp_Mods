#define DONTUSEWORKSHOP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Ugc;
using Steamworks;
using Steamworks.Data;


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
        private static List<string> GetWorkshopNames(params long[] _iDs)
        {
#if DONTUSEWORKSHOP
            return new List<string>();
#endif
            List<ulong> paramValues = _iDs.Select(o => (ulong)o).ToList();
            HashSet<PublishedFileId> workshopIds = new HashSet<PublishedFileId>();
            List<Item> retVal = Task.Run(() => GetModItems(workshopIds)).GetAwaiter().GetResult();
            return retVal.Select(o => o.Title).ToList();
        }
        public static List<string> GetWorkshopNames(List<long> _iDs)
        {
            return GetWorkshopNames(_iDs.ToArray());
        }
        #endregion

        public static List<long> GetCurrentWorkshopIDs
        {
            get
            {
                return GetCurrentWorkshopMods().Select(o => (long)o.Id.Value).ToList(); // NOTTODO: retrive from local mods in case a mod subbed mod is not installed yet - not worth checking for - use dependency checker if needed
            }
        }

        private static List<Item> GetCurrentWorkshopMods()
        {
            UpdateModsList();
            return m_workshopMods;
        }

        private static void UpdateModsList()
        {
            if (m_workshopMods.Count == 0) // Don't update after the first execution (in case the user subscribes to mods while the game is opened)
            {
                m_workshopMods = Task.Run(() => GetSubscribedModItems()).GetAwaiter().GetResult();
            }
        }

        public static async Task<List<Item>> GetSubscribedModItems()
        {
#if DONTUSEWORKSHOP
            return new List<Item>();
#endif
            List<Item> items = new List<Item>();
            int page_number = 1;
            int result_count = 0;
            ResultPage value;
            do
            {
                ResultPage? page = await Query.Items.WhereUserSubscribed().GetPageAsync(page_number);
                if (!page.HasValue)
                {
                    break;
                }

                value = page.Value;
                items.AddRange(value.Entries);
                result_count += value.ResultCount;
                page_number++;
            }
            while (value.ResultCount != 0 && result_count < value.TotalCount);

            return items;
        }

        public static async Task<List<Item>> GetModItems(HashSet<PublishedFileId> _ids)
        {
#if DONTUSEWORKSHOP
            return new List<Item>();
#endif
            List<Item> items = new List<Item>();
            int page_number = 1;
            int result_count = 0;
            ResultPage value;
            do
            {
                ResultPage? page = await Query.Items.WithFileId(_ids.ToArray()).GetPageAsync(page_number);
                if (!page.HasValue)
                {
                    break;
                }

                value = page.Value;
                items.AddRange(value.Entries);
                result_count += value.ResultCount;
                page_number++;
            }
            while (value.ResultCount != 0 && result_count < value.TotalCount);

            return items;
        }
    }
}
