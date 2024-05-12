using BepInEx;

namespace EnableSaveCaseForScav
{
    [BepInPlugin("com.MarsyApp.EnableSaveCaseForScav", "MarsyApp-EnableSaveCaseForScav", "1.1.0")]
    public class EnableSaveCaseForScav : BaseUnityPlugin
    {
        private void Awake()
        {
            Patcher.PatchAll();
            Logger.LogInfo($"Plugin EnableSaveCaseForScavMod is loaded!");
        }

        private void OnDestroy()
        {
            Patcher.UnpatchAll();
            Logger.LogInfo($"Plugin EnableSaveCaseForScavMod is unloaded!");
        }
    }
}
