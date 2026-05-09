using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace LoadingBars
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.0")]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "LoadingBars";

        public override void Load()
        {
            LoadingBarNetwork.Init();
            LoadingBarManager.Init();
            LoadingBarUI.Init();
            new Harmony(MODNAME).PatchAll();
            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}