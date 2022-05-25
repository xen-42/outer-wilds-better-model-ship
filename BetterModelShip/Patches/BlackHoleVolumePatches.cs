using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterModelShip.Patches
{
    [HarmonyPatch(typeof(BlackHoleVolume))]
    public static class BlackHoleVolumePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BlackHoleVolume.VanishModelRocketShip))]
        public static bool BlackHoleVolume_VanishModelRocketShip(BlackHoleVolume __instance)
        {
            GameObject.FindObjectOfType<RemoteFlightConsole>().RespawnModelShip(true);

            return false;
        }
    }
}
