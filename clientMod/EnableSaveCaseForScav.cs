using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using EFT.InventoryLogic;
using EFT.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EnableSaveCaseForScav
{
    [BepInPlugin("com.MarsyApp.EnableSaveCaseForScav", "MarsyApp-EnableSaveCaseForScav", "1.0.0")]
    public class EnableSaveCaseForScav : BaseUnityPlugin
    {
        private void Awake()
        {
            Patcher.PatchAll();
            Logger.LogInfo($"Plugin EnableSaveCaseForScavMod is loaded!");
            ConsoleScreen.Processor.RegisterCommand("extract", _action);
        }
        
        Action _action = () =>
        {
            ConsoleScreen.LogError("This command may only be used 333333");
        };

        private void OnDestroy()
        {
            Patcher.UnpatchAll();
            Logger.LogInfo($"Plugin EnableSaveCaseForScavMod is unloaded!");
        }
    }
}
