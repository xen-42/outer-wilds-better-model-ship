using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BetterModelShip
{
    [HarmonyPatch]
    public class ModelShipStreamingSupport : MonoBehaviour
    {
        private static readonly HashSet<OWCollider> ProtectedColliders = new HashSet<OWCollider>();
        private OWCollider[] shipColliders = new OWCollider[0];
        private StreamingGroup THstreamingGroup;

        public void Initialize()
        {
            //Get the model ships OW colliders and protect them from disabling.
            shipColliders = GetComponentsInChildren<OWCollider>(true);
            foreach (OWCollider collider in shipColliders)
            {
                ProtectedColliders.Add(collider);
                collider.SetLODLevel(0, 0f);
                collider.RestorePhysics(0f);
            }

            //Make a seperate request for Timber Hearth so it doesn't go bye bye.
            //This means Timber Hearth is always loaded... probably gonna fuck up low end PCs.
            THstreamingGroup = GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Streaming").GetComponent<SectorStreaming>()._streamingGroup;
            THstreamingGroup.RequestGeneralAssets(0);

            //
            RemoveModelShipVisFromUnload();
        }

        private void RemoveModelShipVisFromUnload()
        {
            if (GetComponent<CullGroupExcluder>() == null)
            {
                gameObject.AddComponent<CullGroupExcluder>();
            }

            //finds all the renderers.
            HashSet<Renderer> meshes = new HashSet<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    meshes.Add(renderer);
                }
            }

            //remove the model ships renderers and LOD groups from any existing lists.
            foreach (var group in Resources.FindObjectsOfTypeAll<CullGroup>())
            {
                group._staticRenderers?.RemoveAll(renderer => meshes.Contains(renderer));
                group._dynamicRenderers?.RemoveAll(renderer => renderer != null && meshes.Contains(renderer.GetRenderer()));
                group._lodGroups?.RemoveAll(lod => lod != null && lod.transform.IsChildOf(transform));
            }

            //Turn them back the fuck on.
            foreach (var renderer in meshes)
            {
                OWRenderer owRenderer = renderer.GetComponent<OWRenderer>();
                
                if (owRenderer != null) owRenderer.SetLODActivation(true);
                
                //Exclude the thruster flames... they arent needed.
                if(renderer.GetComponent<ThrusterFlameController>() == null)
                    renderer.enabled = true;
                else 
                    renderer.enabled = false;


            }

            foreach (var lod in GetComponentsInChildren<LODGroup>(true))
            {
                lod.enabled = true;
            }
        }

        //Cleanup.
        private void OnDestroy()
        {
            foreach (OWCollider collider in shipColliders)
            {
                ProtectedColliders.Remove(collider);
            }

            if (THstreamingGroup != null)
            {
                THstreamingGroup.ReleaseGeneralAssets();
            }
        }


        //If a collider is protected, we don't fuck with it.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWCollider), nameof(OWCollider.SetLODLevel))]
        public static void KeepModelColliderLOD(OWCollider __instance, ref int __0, ref float __1)
        {
            if (!ProtectedColliders.Contains(__instance)) return;

            __0 = 0;
            __1 = 0f;
        }
    }
}