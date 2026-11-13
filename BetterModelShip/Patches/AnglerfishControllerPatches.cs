using HarmonyLib;
using UnityEngine;

namespace BetterModelShip
{
    [HarmonyPatch(typeof(AnglerfishController),nameof(AnglerfishController.OnCaughtObject))]
    public static class AnglerfishControllerPatches
    {
        [HarmonyPrefix]
        public static bool BeforeCaughtObject(OWRigidbody caughtBody)
        {
            if (caughtBody == null || BetterModelShip.ModelShipBody == null)
            {
                return true;
            }

            //If its NOT caught either the model ship or the remote player attached to that ship, ignore it.
            if (caughtBody != BetterModelShip.ModelShipBody && !(BetterModelShip.IsPilotingModelShip && caughtBody == BetterModelShip.PlayerBody))
            {
                return true;
            }


            //If we cant find the RemoteFlightConsole, sod it.
            RemoteFlightConsole console = Object.FindObjectOfType<RemoteFlightConsole>();
            if (console == null || console._modelShipBody != BetterModelShip.ModelShipBody)
            {
                return true;
            }

            //If we've got this far, we're either already respawning to safety, or not.
            if (!console._isRespawning)
            {
                console.RespawnModelShip(true);
            }

            return false;
        }
    }
}