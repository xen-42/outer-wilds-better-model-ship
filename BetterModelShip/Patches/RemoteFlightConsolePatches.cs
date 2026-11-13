using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterModelShip
{
    [HarmonyPatch]
    public static class RemoteFlightConsolePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RemoteFlightConsole), nameof(RemoteFlightConsole.Update))]
        public static bool RemoteFlightConsole_Update_Prefix(RemoteFlightConsole __instance)
        {
            if(!OWInput.IsNewlyPressed(InputLibrary.GetInputCommand(BetterModelShip.ExitViewBinding)) ||
                OWTime.IsPaused() || 
                OWInput.GetInputMode() != InputMode.ModelShip ||
                __instance._isRespawning ||
                __instance._modelShipBody == null)
            {
                return true;
            }

            //If both keys pressed, original wins.
            if (OWInput.IsNewlyPressed(InputLibrary.cancel)) return true;

            //If "Exit View" is pressed then, we do what we need to without respawning or suspending ship.
            __instance._lockOnTargeting.BreakLock();
            __instance._attachPoint.DetachPlayer();
            __instance._interactVolume.ResetInteraction();
            __instance._playerProxyCollider.SetActivation(false);
            __instance.enabled = false;

            GlobalMessenger.FireEvent("ExitRemoteFlightConsole");

            //Detector turned active to allow it to feel things (not emotions though...).
            __instance._modelShipBody.GetComponent<ModelShipController>()._detector.SetActive(true);

            __instance.HideAllPrompts();
            return false;
        }
        
        


        //Issues were caused with the player gravity as the player was being temporarily disabled with the Model Ship during this flow.
        //Essentially brings the destruction handling for the model ship forward a little, meaning player is still active when it occurs.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DestructionVolume), nameof(DestructionVolume.VanishModelRocketShip))]
        public static void ExitBeforeModelShipDestruction(OWRigidbody __0)
        {
            if (!BetterModelShip.IsPilotingModelShip || __0 != BetterModelShip.ModelShipBody)
            {
                return;
            }

            RemoteFlightConsole console = Object.FindObjectOfType<RemoteFlightConsole>();

            if (console != null && console._modelShipBody == __0)
            {
                console.OnModelRocketShipDestroyed();
            }
        }

        //Prevents issues for repeated calls to that OnDestroyed function.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RemoteFlightConsole), nameof(RemoteFlightConsole.OnModelRocketShipDestroyed))]
        public static bool PreventRepeatedDestructionExit(RemoteFlightConsole __instance)
        {
            return __instance._modelShipBody != null;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(RemoteFlightConsole), nameof(RemoteFlightConsole.Update))]
        public static void RemoteFlightConsole_Update(RemoteFlightConsole __instance)
        {
            __instance._verticalThrustPrompt.SetVisibility(false);
            __instance._horizontalThrustPrompt.SetVisibility(false);
        }
    }
}
