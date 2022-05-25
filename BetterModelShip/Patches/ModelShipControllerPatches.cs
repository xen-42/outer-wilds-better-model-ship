using HarmonyLib;
using UnityEngine;

namespace BetterModelShip
{
    [HarmonyPatch]
    public static class ModelShipControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ModelShipController), nameof(ModelShipController.ReadRotationalInput))]
        public static bool ModelShipController_ReadRotationalInput(ModelShipController __instance, ref Vector3 __result)
        {
            if (BetterModelShip.IsRollMode)
            {
                return true;
            }
            else
            {
                Vector3 zero = Vector3.zero;
                zero.y += OWInput.GetValue(InputLibrary.yaw, InputMode.All);
                zero.x -= OWInput.GetValue(InputLibrary.pitch, InputMode.All);
                __result = zero;
                return false;
            }
        }
    }
}
