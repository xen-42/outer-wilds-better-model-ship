using HarmonyLib;
using UnityEngine;

namespace BetterModelShip
{
    [HarmonyPatch]
    public static class RemoteFlightConsolePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RemoteFlightConsole), nameof(RemoteFlightConsole.Update))]
        public static void RemoteFlightConsole_Update(RemoteFlightConsole __instance)
        {
            __instance._verticalThrustPrompt.SetVisibility(false);
            __instance._horizontalThrustPrompt.SetVisibility(false);
        }
    }
}
