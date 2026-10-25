using HarmonyLib;
using LevelGeneration;

namespace LoadingBars
{
    [HarmonyPatch]
    internal static class LoadingBarPatches
    {
        [HarmonyPatch(typeof(LG_Factory), nameof(LG_Factory.GetNewJob))]
        [HarmonyPostfix]
        private static void Post_GetNewJob(LG_Factory __instance, bool __result)
        {
            if (__result)
                LoadingBarManager.OnBatchUpdate_Internal(__instance);
        }

        // Reset the nav marker layer when returning to lobby, to help ensure that retiring and returning after expedition function
        [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.ChangeState))]
        [HarmonyPostfix]
        private static void Post_ChangeGameState(eGameStateName nextState)
        {
            if (nextState == eGameStateName.Lobby)
            {
                LoadingBarManager.ResetPlayers();
                GuiManager.NavMarkerLayer.SetVisible(false);
            }
        }

        [HarmonyPatch(typeof(FocusStateManager), nameof(FocusStateManager.ChangeState))]
        [HarmonyPostfix]
        private static void Post_ChangeFocusState(eFocusState state)
        {
            if (state == eFocusState.InElevator)
            {
                GuiManager.NavMarkerLayer.SetVisible(true);
                LoadingBarUI.SetBarsActive(true);
            }
            else
            {
                LoadingBarUI.SetBarsActive(false);
            }
        }

        // Nav markers do not show if their position is not in the same dimension as the player
        // As a result, when redropping into levels with multiple dimensions, the position is shown as in another dimension than reality
        // So during drop, we halt the updating of their dimension to ensure they don't get hidden
        [HarmonyPatch(typeof(NavMarker), nameof(NavMarker.UpdateTrackingDimension))]
        [HarmonyPrefix]
        private static bool Pre_UpdateTrackingDimension(NavMarker __instance)
        {
            switch (GameStateManager.CurrentStateName)
            {
                case eGameStateName.Generating:
                case eGameStateName.ReadyToStartLevel:
                case eGameStateName.ReadyToStopElevatorRide:
                case eGameStateName.StopElevatorRide:
                    return false;
                default:
                    return true;
            }
        }
    }
}
