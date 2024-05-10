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
                new ItemViewPatches.PostRaidHealthScreenClassPath(),
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

        private static string caseId = "5732ee6a24597719ae0c0281";
        
        public class PostRaidHealthScreenClassPath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.PropertyGetter(typeof(PostRaidHealthScreenClass), nameof(PostRaidHealthScreenClass.Boolean_3));
            }

            [PatchPostfix]
            private static void PatchPostfix(PostRaidHealthScreenClass __instance, ref bool __result)
            {
                try
                {
                    __result = true;
                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        public class InteractionsHandlerClassPath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Remove));
            }

            [PatchPostfix]
            private static void PatchPostfix(InteractionsHandlerClass __instance, Item item, ref GStruct414<GClass2785> __result)
            {
                try
                {
                    if (item.TemplateId.Equals(caseId))
                    {
                        UnlootableComponent itemComponent = item.GetItemComponent<UnlootableComponent>();
                        __result = new InteractionsHandlerClass.GClass3334(itemComponent);
                    }

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        public class HasItemsPath : ModulePatch
        {

            protected static Type nestedType;
            protected override MethodBase GetTargetMethod()
            {
                nestedType = AccessTools.Inner(typeof(ScavengerInventoryScreen), nameof(ScavengerInventoryScreen.GClass3131));
                return AccessTools.PropertyGetter(nestedType, "HasItems");
            }

            [PatchPostfix]
            private static void PatchPostfix(InteractionsHandlerClass __instance, ref bool __result)
            {

                try
                {
                    FieldInfo scavControllerFiledInfo = AccessTools.Field(nestedType, "ScavController");
                    GClass2764 scavController = (GClass2764)scavControllerFiledInfo.GetValue(__instance);
                    IEnumerable<Item> items = scavController.Inventory.AllRealPlayerItems;
                    
                    IEnumerable<Item> filteredItems = items.Where(x => !x.TemplateId.Equals(caseId));
                    int filteredCount = filteredItems.Count();
                    if (filteredCount == 0)
                    {
                        __result = false;
                    }

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }
}
