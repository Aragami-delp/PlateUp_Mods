using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Kitchen;
using KitchenMods;
using SaveSystem;

namespace SaveSystem_MultiMod
{
    // Much easier as a patch - also avoids new components which make loading impossible if mod not installed
    //[UpdateInGroup(typeof(TimeManagementGroup))]
    //[UpdateAfter(typeof(AdvanceTime))]
    //[UpdateBefore(typeof(StartNewDay))]
    //public class SaveSystemFullSaveAtStartOfNewDay : NightSystem, IModSystem
    //{
    //    [ReadOnly]
    //    private EntityQuery SSaveSystemHasSavedQuery;
    //    [ReadOnly]
    //    private EntityQuery CRenameRestaurantQuery;

    //    protected override void Initialise()
    //    {
    //        base.Initialise();
    //        this.SSaveSystemHasSavedQuery = this.GetEntityQuery(ComponentType.ReadOnly<SSaveSystemStartOfNewDayHasSaved>());
    //        this.CRenameRestaurantQuery = this.GetEntityQuery(ComponentType.ReadOnly<CRenameRestaurant>());
    //    }

    //    protected override void OnUpdate()
    //    {
    //        if (this.HasSingleton<SIsNightFirstUpdate>() && this.HasSingleton<SSaveSystemStartOfNewDayHasSaved>())
    //        {
    //            this.EntityManager.DestroyEntity(this.SSaveSystemHasSavedQuery.GetSingletonEntity());
    //        }
    //        else
    //        {
    //            if (this.HasSingleton<SSaveSystemStartOfNewDayHasSaved>() || this.Time.IsPaused)
    //                return;
    //            SaveSystem_ModLoaderSystem.LogInfo("Performing a full end of night save");
    //            if (this.HasSingleton<CRenameRestaurant>())
    //            {
    //                SaveSystemManager.Instance.CurrentNamePlate = CRenameRestaurantQuery.GetSingleton<CRenameRestaurant>().Name.Value;
    //            }
    //            this.World.Add<SSaveSystemStartOfNewDayHasSaved>();
    //            this.World.Add<CRequestSave>(new CRequestSave()
    //            {
    //                SaveType = SaveType.AutoFull
    //            });
    //        }
    //    }

    //    //protected override void OnCreateForCompiler()
    //    //{
    //    //    base.OnCreateForCompiler();
    //    //    this.SSaveSystemHasSavedQuery = this.GetEntityQuery(ComponentType.ReadOnly<SaveSystemFullSaveAtDay.SSaveSystemDayHasSaved>());
    //    //}

    //    [StructLayout(LayoutKind.Sequential, Size = 1)]
    //    private struct SSaveSystemStartOfNewDayHasSaved : IModComponent
    //    {
    //    }
    //}
}
