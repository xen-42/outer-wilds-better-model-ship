using HarmonyLib;
using UnityEngine;

namespace BetterModelShip
{
    [HarmonyPatch]
    public static class OWRigidBodyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OWRigidbody), nameof(OWRigidbody.SetPosition))]
        public static void OWRigidbody_SetPosition(OWRigidbody __instance, Vector3 worldPosition)
        {
            if (BetterModelShip.IsPilotingModelShip && __instance.name.Equals("ModelRocket_Body"))
            {
                GlobalMessenger.FireEvent("PlayerRepositioned");
            }
        }
    }
}
