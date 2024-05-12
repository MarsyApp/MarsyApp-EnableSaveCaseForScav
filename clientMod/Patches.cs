using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
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
                new ItemViewPatches.HasItemsPath(),
                new ItemViewPatches.SlotView_CanDrag_Path(),
                new ItemViewPatches.ItemUiContext_QuickFindAppropriatePlace_Path(),
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

        private static bool IsScav(IProfile profile)
        {
            return profile.Side == EPlayerSide.Savage;
        }
        public class PostRaidHealthScreenClassPath : ModulePatch
        {
            // показ окна передачи лута даже при смерти, если есть подсумок
            
            protected static FieldInfo ProfileFieldInfo;
            protected override MethodBase GetTargetMethod()
            {
                ProfileFieldInfo = AccessTools.GetDeclaredFields(typeof(PostRaidHealthScreenClass)).Single(x => x.FieldType == typeof(Profile));
                return AccessTools.PropertyGetter(typeof(PostRaidHealthScreenClass), nameof(PostRaidHealthScreenClass.Boolean_3));
            }

            [PatchPostfix]
            private static void PatchPostfix(PostRaidHealthScreenClass __instance, ref bool __result)
            {
                try
                {
                    Profile profile = (Profile)ProfileFieldInfo.GetValue(__instance);
                    IEnumerable<Item> allRealPlayerItems = profile.Inventory.AllRealPlayerItems; 
                    bool hasSecureContainerTemplateClass = allRealPlayerItems.Any(x => x.Template.Parent is SecureContainerTemplateClass); 

                   if (IsScav(profile) && hasSecureContainerTemplateClass)
                   {
                       __result = true;
                   }
                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        public class HasItemsPath : ModulePatch
        {
            // не показывать предупреждение о продаже если остался только подсумок

            protected static Type NestedType;
            protected override MethodBase GetTargetMethod()
            {
                NestedType = AccessTools.Inner(typeof(ScavengerInventoryScreen), nameof(ScavengerInventoryScreen.GClass3131));
                return AccessTools.PropertyGetter(NestedType, "HasItems");
            }

            [PatchPostfix]
            private static void PatchPostfix(InteractionsHandlerClass __instance, ref bool __result)
            {

                try
                {
                    FieldInfo scavControllerFiledInfo = AccessTools.Field(NestedType, "ScavController");
                    GClass2764 scavController = (GClass2764)scavControllerFiledInfo.GetValue(__instance);
                    IEnumerable<Item> items = scavController.Inventory.AllRealPlayerItems;
                    
                    IEnumerable<Item> filteredItems = items.Where(x => !(x.Template.Parent is SecureContainerTemplateClass));
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
        
        public class SlotView_CanDrag_Path : ModulePatch
        {
            // запрет на перетаскивание подсумка в склад
            protected static MethodInfo SlotFiledInfo;
            protected override MethodBase GetTargetMethod()
            {
                SlotFiledInfo = AccessTools.PropertyGetter(typeof(SlotView), nameof(SlotView.Slot));
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.CanDrag));
            }

            [PatchPrefix]
            private static bool PatchPrefix(SlotView __instance, ItemContextAbstractClass itemContext, ref bool __result)
            {
                try
                {
                    Slot slot = (Slot)SlotFiledInfo.Invoke(__instance, null);
                    var parentItem = slot.ParentItem;
                    InventoryControllerClass owner = parentItem.Owner as InventoryControllerClass;
                    if (owner != null)
                    {
                        IProfile profile = owner.Profile;
                        bool isSecureContainer = itemContext.Item.Template.Parent is SecureContainerTemplateClass;
                        if (IsScav(profile) && isSecureContainer)
                        {
                            __result = false;
                            return false;
                        }
                    }
                } catch (Exception e)
                {
                    Logger.LogError(e);
                }

                return true;
            }
        }
        
        public class ItemUiContext_QuickFindAppropriatePlace_Path : ModulePatch
        {
            // запрет на перенос посука через ctrl+click
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.QuickFindAppropriatePlace));
            }

            [PatchPrefix]
            private static bool PatchPrefix(ItemUiContext __instance, ItemContextAbstractClass itemContext, ref GStruct413 __result)
            {
                try
                {
                    InventoryControllerClass owner = itemContext.Item.Owner as InventoryControllerClass;
                    if (owner != null)
                    {
                        IProfile profile = owner.Profile;
                        bool isSecureContainer = itemContext.Item.Template.Parent is SecureContainerTemplateClass;
                        if (IsScav(profile) && isSecureContainer)
                        {
                            __result = new GClass3346("Can't find appropriate container");
                            return false;
                        }
                    }
                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
                
                return true;
            }
        }
    }
}
