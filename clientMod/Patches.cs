using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;

namespace EnableSaveCaseForScav
{
    class Patcher
    {
        public static void PatchAll()
        {
            new PatchManager().RunPatches();
        }
        
        public static void UnpatchAll()
        {
            new PatchManager().RunUnpatches();
        }
    }

    public class PatchManager
    {
        public PatchManager()
        {
            this._patches = new List<ModulePatch>
            {
                new ItemViewPatches.ApplyDamageInfoPath(),
                new ItemViewPatches.InteractionsHandlerClassPath(),
                new ItemViewPatches.HasItemsPath(),
            };
        }

        public void RunPatches()
        {
            foreach (ModulePatch patch in this._patches)
            {
                patch.Enable();
            }
        }
        
        public void RunUnpatches()
        {
            foreach (ModulePatch patch in this._patches)
            {
                patch.Disable();
            }
        }

        private readonly List<ModulePatch> _patches;
    }

    public static class ItemViewPatches
    {
        public class ApplyDamageInfoPath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(PostRaidHealthScreenClass).GetMethod("get_Boolean_3");
            }

            [PatchPostfix]
            private static void PatchPostfix(PostRaidHealthScreenClass __instance, ref bool __result)
            {
                try
                {
                    __result = true;
                } catch (System.Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        public class InteractionsHandlerClassPath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(InteractionsHandlerClass).GetMethod("Remove", BindingFlags.Static | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(InteractionsHandlerClass __instance, Item item, ref GStruct414<GClass2785> __result)
            {
                try
                {
                    if (item.TemplateId.Equals("5732ee6a24597719ae0c0281"))
                    {
                        UnlootableComponent itemComponent = item.GetItemComponent<UnlootableComponent>();
                        __result = new InteractionsHandlerClass.GClass3334(itemComponent);
                    }

                } catch (System.Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        public class HasItemsPath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type nestedType = typeof(ScavengerInventoryScreen).GetNestedType("GClass3131", BindingFlags.Public);
                MethodInfo methodInfo = nestedType.GetMethod("get_HasItems", BindingFlags.Public | BindingFlags.Instance);
                return methodInfo;
            }

            [PatchPostfix]
            private static void PatchPostfix(InteractionsHandlerClass __instance, ref bool __result)
            {

                try
                {
                    FieldInfo _ScavController = AccessTools.Field(typeof(ScavengerInventoryScreen.GClass3131), "ScavController");
                    GClass2764 scavController = (GClass2764)_ScavController.GetValue(__instance);
                    IEnumerable<Item> Items = scavController.Inventory.AllRealPlayerItems;
                    
                    IEnumerable<Item> filteredItems = Items.Where(x => !x.TemplateId.Equals("5732ee6a24597719ae0c0281"));
                    int filteredCount = filteredItems.Count();
                    if (filteredCount == 0)
                    {
                        __result = false;
                    }

                } catch (System.Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }
}
