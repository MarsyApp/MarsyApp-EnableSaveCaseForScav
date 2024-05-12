using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                // new ItemViewPatches.InteractionsHandlerClassPath(),
                new ItemViewPatches.HasItemsPath(),
                // new ItemViewPatches.ScavengerInventoryScreen_method_3_Path(),
                new ItemViewPatches.SlotView_CanDrag_Path(),
                new ItemViewPatches.ItemUiContext_QuickFindAppropriatePlace_Path(),
                // new ItemViewPatches.SlotViewHeader_Show_Path(),
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

        private static bool isScav(IProfile profile)
        {
            return profile.Side == EPlayerSide.Savage;
        }
        public class PostRaidHealthScreenClassPath : ModulePatch
        {
            // показ окна передачи лута даже при смерти, если есть подсумок
            
            protected static FieldInfo profileFieldInfo;
            protected override MethodBase GetTargetMethod()
            {
                profileFieldInfo = AccessTools.GetDeclaredFields(typeof(PostRaidHealthScreenClass)).Single(x => x.FieldType == typeof(Profile));
                return AccessTools.PropertyGetter(typeof(PostRaidHealthScreenClass), nameof(PostRaidHealthScreenClass.Boolean_3));
            }

            [PatchPostfix]
            private static void PatchPostfix(PostRaidHealthScreenClass __instance, ref bool __result)
            {
                try
                {
                    Profile profile = (Profile)profileFieldInfo.GetValue(__instance);
                    IEnumerable<Item> allRealPlayerItems = profile.Inventory.AllRealPlayerItems; 
                    bool hasSecureContainerTemplateClass = allRealPlayerItems.Any(x => x.Template.Parent is SecureContainerTemplateClass); 

                   if (isScav(profile) && hasSecureContainerTemplateClass)
                   {
                       __result = true;
                   }
                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        
        /*public class InteractionsHandlerClassPath : ModulePatch
        {
            
            // блокировка перемещения подсумка в инвентарь
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Move));
            }

            [PatchPrefix]
            private static bool PatchPrefix(InteractionsHandlerClass __instance, Item item, TraderControllerClass itemController, ref GStruct414<GClass2786> __result)
            {
                try
                {
                    Debugger.Break();
                    bool isScav = (itemController as GClass2759).Side == EPlayerSide.Savage || true;
                    bool isSecureContainer = item.Template.Parent is SecureContainerTemplateClass;
                    if (isScav && isSecureContainer)
                    {
                        UnlootableComponent itemComponent = item.GetItemComponent<UnlootableComponent>();
                        // __result = new InteractionsHandlerClass.GClass3334(itemComponent);
                        __result = new InteractionsHandlerClass.GClass3334(null);
                        return false;
                    }

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }

                return true;
            }
        }*/
        
        /*public class InteractionsHandlerClassPath : ModulePatch
        {
            
            // блокировка перемещения подсумка в инвентарь
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.Remove));
            }

            [PatchPrefix]
            private static bool PatchPrefix(InteractionsHandlerClass __instance, Item item, TraderControllerClass itemController, bool simulate, bool ignoreRestrictions, ref GStruct414<GClass2785> __result)
            {
                try
                {
                    Logger.LogInfo($"InteractionsHandlerClass.Remove simulate: ${simulate}");
                    if (simulate) return true;
                    bool isScav = (itemController as GClass2759).Side == EPlayerSide.Savage || true;
                    bool isSecureContainer = item.Template.Parent is SecureContainerTemplateClass;
                    if (isScav && isSecureContainer)
                    {
                        UnlootableComponent itemComponent = item.GetItemComponent<UnlootableComponent>();
                        // __result = new InteractionsHandlerClass.GClass3334(itemComponent);
                        __result = (GStruct414<GClass2785>) (Error) new InteractionsHandlerClass.GClass3334(itemComponent);
                        return false;
                    }

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }

                return true;
            }
        }*/
        
        public class HasItemsPath : ModulePatch
        {
            // не показывать предупреждение о продаже если остался только подсумок

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
        
        /*public class ScavengerInventoryScreen_method_3_Path : ModulePatch
        {
            
            // ? блокировка кнопки продать все, если нет ничего кроме подсумка
            protected static Type nestedType;
            protected override MethodBase GetTargetMethod()
            {
                nestedType = AccessTools.Inner(typeof(ScavengerInventoryScreen), nameof(ScavengerInventoryScreen.GClass3131));
                return AccessTools.GetDeclaredMethods(typeof(ScavengerInventoryScreen)).Single(x => x.ReturnType == typeof(bool) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(IEnumerable<Item>).MakeByRefType());
            }

            [PatchPostfix]
            private static void PatchPostfix(ScavengerInventoryScreen __instance, ref bool __result)
            {

                try
                {
                    
                    __result = false;
                    FieldInfo scavControllerFiledInfo = AccessTools.Field(nestedType, "ScavController");
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
        }*/
        
        public class SlotView_CanDrag_Path : ModulePatch
        {
            // запрет на перетаскивание подсумка в склад
            protected static MethodInfo slotFiledInfo;
            protected override MethodBase GetTargetMethod()
            {
                slotFiledInfo = AccessTools.PropertyGetter(typeof(SlotView), nameof(SlotView.Slot));
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.CanDrag));
            }

            [PatchPrefix]
            private static bool PatchPrefix(SlotView __instance, ItemContextAbstractClass itemContext, ref bool __result)
            {
                try
                {
                    Slot slot = (Slot)slotFiledInfo.Invoke(__instance, null);
                    var ParentItem = slot.ParentItem;
                    InventoryControllerClass Owner = ParentItem.Owner as InventoryControllerClass;
                    IProfile Profile = Owner.Profile;
                    bool isSecureContainer = itemContext.Item.Template.Parent is SecureContainerTemplateClass;
                    if (isScav(Profile) && isSecureContainer)
                    {
                        __result = false;
                        return false;
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
            protected static FieldInfo profileFieldInfo;
            protected override MethodBase GetTargetMethod()
            {
                profileFieldInfo = AccessTools.GetDeclaredFields(typeof(ItemUiContext)).Single(x => x.FieldType == typeof(Profile));
                return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.QuickFindAppropriatePlace));
            }

            [PatchPrefix]
            private static bool PatchPrefix(ItemUiContext __instance, ItemContextAbstractClass itemContext, ref GStruct413 __result)
            {
                try
                {
                    Debugger.Break();
                    InventoryControllerClass Owner = itemContext.Item.Owner as InventoryControllerClass;
                    IProfile profile = Owner.Profile;
                    bool isSecureContainer = itemContext.Item.Template.Parent is SecureContainerTemplateClass;
                    Debugger.Break();
                    if (isScav(profile) && isSecureContainer)
                    {
                        Debugger.Break();
                        __result = new GClass3346("Can't find appropriate container");
                        return false;
                    }
                    Debugger.Break();

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
                
                return true;
            }
        }
        
        /*public class SlotViewHeader_Show_Path : ModulePatch
        {
            // выключение кнопки смены предмета в слоте
            // похоже не нужен
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotViewHeader), nameof(SlotViewHeader.Show));
            }

            [PatchPrefix]
            private static bool PatchPrefix(InventoryControllerClass inventoryController, Slot slot, ref bool canBeSelected)
            {
                try
                {
                    // Debugger.Break();
                    bool isSecureContainer = slot.ID.Equals("SecuredContainer");
                    if (isScav(inventoryController.Profile) && isSecureContainer)
                    {
                        canBeSelected = false;
                    }

                } catch (Exception e)
                {
                    Logger.LogError(e);
                }
                
                return true;
            }
        }*/
    }
}
