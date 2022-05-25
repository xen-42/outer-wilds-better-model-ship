using OWML.ModHelper;
using OWML.Common;
using OWML.Utils;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using HarmonyLib;

namespace BetterModelShip
{
    public class BetterModelShip : ModBehaviour
    {
        public static BetterModelShip Instance;

        public static bool IsPilotingModelShip { get; private set; }
        public static bool IsRollMode { get; private set; }
        
        public static GameObject ModelShipCamera { get; private set; }

        private Camera _camera;
        private OWCamera _OWCamera;
        private OWCamera _previousCamera;
        private GameObject _modelShip;

        private bool _initialized;

        public static ICommonCameraAPI CommonCameraAPI;

        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Instance = this;

            CommonCameraAPI = ModHelper.Interaction.GetModApi<ICommonCameraAPI>("xen.CommonCameraUtility");

            GlobalMessenger<OWRigidbody>.AddListener("EnterRemoteFlightConsole", OnEnterRemoteFlightConsole);
            GlobalMessenger.AddListener("ExitRemoteFlightConsole", OnExitRemoteFlightConsole);
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem") return;

            (_OWCamera, _camera) = CommonCameraAPI.CreateCustomCamera("ModelShipCamera");

            _modelShip = GameObject.FindObjectOfType<ModelShipController>().gameObject;

            PreInit();
        }

        private void PreInit()
        {
            var modelShipController = _modelShip.GetComponent<ModelShipController>();
            modelShipController._thrusterModel._maxTranslationalThrust *= 2f;

            var detector = _modelShip.transform.Find("Detector").gameObject;
            GameObject.Destroy(detector.GetComponent<ConstantForceDetector>());
            detector.AddComponent<AlignmentForceDetector>();

            SectorManager.RegisterSectorDetector(detector.GetComponent<SectorDetector>());
        }

        private void Init()
        {
            var probeCamera = Locator.GetProbe().transform.Find("CameraPivot/ForwardCamera");
            
            var noise = _OWCamera.gameObject.AddComponent<NoiseImageEffect>();
            noise._noiseShader = probeCamera.GetComponent<NoiseImageEffect>()._noiseShader;
            noise.strength = 0.005f;

            var postProcessing = _OWCamera.gameObject.GetComponent<PostProcessingBehaviour>();
            postProcessing.profile = probeCamera.GetComponent<PostProcessingBehaviour>().profile;
        }

        private void OnEnterRemoteFlightConsole(OWRigidbody _)
        {
            ModHelper.Console.WriteLine($"OnEnterRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = true;
            _previousCamera = Locator.GetActiveCamera();
            _camera.enabled = true;
            GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _OWCamera);

            // For some stupid reason I can't set this where I wanted to idgi
            _OWCamera.gameObject.transform.parent = _modelShip.transform;
            _OWCamera.gameObject.transform.localPosition = new Vector3(0, 1, -1);
            _OWCamera.gameObject.transform.localRotation = Quaternion.identity;

            // Have to init here to be after Common Camera
            if(!_initialized)
            {
                _initialized = true;
                ModHelper.Events.Unity.FireOnNextUpdate(Init);
            }
        }

        private void OnExitRemoteFlightConsole()
        {
            ModHelper.Console.WriteLine($"OnExitRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = false;
            _camera.enabled = false;
            _previousCamera.enabled = true;
            GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _previousCamera);
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if(camera.gameObject.name.Equals("PlayerCamera") && IsPilotingModelShip)
            {
                // Send up back to the piloting camera
                // Has to be next tick else the game gets mad about recursive FireEvent calls
                ModHelper.Events.Unity.FireOnNextUpdate(() => OnEnterRemoteFlightConsole(null));
            }
        }

        private void Update()
        {
            if (!IsPilotingModelShip) return;

            if (OWInput.IsNewlyPressed(InputLibrary.rollMode)) IsRollMode = true;
            if (OWInput.IsNewlyReleased(InputLibrary.rollMode)) IsRollMode = false;
        }
    }
}
