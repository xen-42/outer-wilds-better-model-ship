using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterModelShip
{
    public static class Patches
    {
        public static void Apply()
        {
            var harmony = BetterModelShip.Instance.ModHelper.HarmonyHelper;

            harmony.AddPrefix<ModelShipController>(nameof(ModelShipController.ReadRotationalInput), typeof(Patches), nameof(Patches.ReadRotationalInput));
        }

        private static bool ReadRotationalInput(ModelShipController __instance, ref Vector3 __result)
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
