//using System.Runtime.InteropServices;
//using Unity.Collections;
//using Unity.Entities;
//using UnityEngine;
//using Kitchen;
//using KitchenMods;

//namespace SaveSystem_MultiMod
//{
//    public class SaveSystemFullSaveAtDay : DaySystem, IModSystem
//    {
//        [ReadOnly]
//        private EntityQuery _SingletonEntityQuery_SSaveSystemDayHasSaved_20;

//        protected override void OnUpdate()
//        {
//            if (this.HasSingleton<SIsDayFirstUpdate>() && this.HasSingleton<SaveSystemFullSaveAtDay.SSaveSystemDayHasSaved>())
//            {
//                this.EntityManager.DestroyEntity(this._SingletonEntityQuery_SSaveSystemDayHasSaved_20.GetSingletonEntity());
//            }
//            else
//            {
//                if (this.HasSingleton<SaveSystemFullSaveAtDay.SSaveSystemDayHasSaved>() || this.Time.IsPaused)
//                    return;
//                SaveSystem_ModLoaderSystem.LogError("Performing a full save");
//                this.World.Add<SaveSystemFullSaveAtDay.SSaveSystemDayHasSaved>();
//                this.World.Add<CRequestSave>(new CRequestSave()
//                {
//                    SaveType = SaveType.AutoFull
//                });
//            }
//        }

//        protected override void OnCreateForCompiler()
//        {
//            base.OnCreateForCompiler();
//            this._SingletonEntityQuery_SSaveSystemDayHasSaved_20 = this.GetEntityQuery(ComponentType.ReadOnly<SaveSystemFullSaveAtDay.SSaveSystemDayHasSaved>());
//        }

//        [StructLayout(LayoutKind.Sequential, Size = 1)]
//        private struct SSaveSystemDayHasSaved : IModComponent
//        {
//        }
//    }
//}
