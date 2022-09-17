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
        private GameObject _modelShip;
        private GameObject _dummyPlayer;
        private PlayerResources _playerResources;
        private PlayerAttachPoint _playerAttachPoint;

        private bool _isSuited;

        private bool _initialized;

        public static ICommonCameraAPI CommonCameraAPI;

        public static OWRigidbody PlayerBody { get; private set; }
        public static OWRigidbody ModelShipBody { get; private set; }

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

            _initialized = false;

            PreInit();
            ModHelper.Events.Unity.FireOnNextUpdate(Init);
        }

        private void PreInit()
        {
            var modelShipController = _modelShip.GetComponent<ModelShipController>();
            modelShipController._thrusterModel._maxTranslationalThrust *= 2f;

            var detector = _modelShip.transform.Find("Detector").gameObject;
            GameObject.Destroy(detector.GetComponent<ConstantForceDetector>());
            detector.AddComponent<AlignmentForceDetector>();

            var attachGO = new GameObject("AttachPoint");
            attachGO.transform.parent = _modelShip.transform;
            attachGO.transform.localPosition = Vector3.zero;
            attachGO.transform.localRotation = Quaternion.Euler(270, 0, 0);
            _playerAttachPoint = attachGO.AddComponent<PlayerAttachPoint>();

            _modelShip.AddComponent<PromptHandler>();
            _modelShip.AddComponent<ModelShipFlashlight>();
        }

        private void Init()
        {
            PlayerBody = Locator.GetPlayerTransform().GetComponent<OWRigidbody>();
            ModelShipBody = _modelShip.GetComponent<OWRigidbody>();
            _playerResources = GameObject.FindObjectOfType<PlayerResources>();

            var cullGroup = GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Village/Interactables_Village").GetComponent<SectorCullGroup>();
            cullGroup.SetSector(null);
            cullGroup.SetVisible(true);
        }

        private void InitCamera()
        {
            var probeCamera = Locator.GetProbe().transform.Find("CameraPivot/ForwardCamera");

            var postProcessing = _OWCamera.gameObject.GetComponent<PostProcessingBehaviour>();
            postProcessing.profile = probeCamera.GetComponent<PostProcessingBehaviour>().profile;
        }

        private void EnterCameraView()
        {
            CommonCameraAPI.EnterCamera(_OWCamera);
        }

        private void OnEnterRemoteFlightConsole(OWRigidbody _)
        {
            ModHelper.Console.WriteLine($"OnEnterRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = true;
            EnterCameraView();

            // For some stupid reason I can't set this where I wanted to idgi
            _OWCamera.gameObject.transform.parent = _modelShip.transform;
            _OWCamera.gameObject.transform.localPosition = new Vector3(0, 0, 0.5f);
            _OWCamera.gameObject.transform.localRotation = Quaternion.identity;

            // Have to init here to be after Common Camera
            if (!_initialized)
            {
                _initialized = true;
                ModHelper.Events.Unity.FireOnNextUpdate(InitCamera);
            }

            // We need to replace the player with a dummy
            var player = Locator.GetPlayerBody();

            _dummyPlayer = new GameObject("DummyPlayer");

            var model = GameObject.Instantiate(player.transform.Find("Traveller_HEA_Player_v2")).gameObject;
            model.transform.parent = _dummyPlayer.transform;
            model.transform.localPosition = new Vector3(0, -1, 0); // To have the feet on the ground

            _dummyPlayer.transform.parent = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform;
            _dummyPlayer.transform.position = player.transform.position;
            _dummyPlayer.transform.rotation = player.transform.rotation;

            // The suit makes unwanted HUD stuff appear
            _isSuited = PlayerState.IsWearingSuit();
            if (_isSuited)
            {
                Locator.GetPlayerSuit().RemoveSuit();
            }

            // Keep TH loaded
            GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Streaming").GetComponent<SectorStreaming>()._softLoadRadius = 100000;
            GameObject.Find("TimberHearth_Body/Sector_TH").GetComponent<SphereShape>().radius = 100000;

            // Now disable player rendering and put it on the model ship
            foreach (var renderer in player.GetComponentsInChildren<Renderer>())
            {
                renderer.forceRenderingOff = true;
            }

            _playerAttachPoint.AttachPlayer();

            GlobalMessenger.FireEvent("PlayerRepositioned");

            // Player has to be immortal
            _playerResources._invincible = true;
            Locator.GetDeathManager()._invincible = true;
        }

        private void OnExitRemoteFlightConsole()
        {
            ModHelper.Console.WriteLine($"OnExitRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = false;

			CommonCameraAPI.ExitCamera(_OWCamera);

			// Put player back to normal
			var player = Locator.GetPlayerBody();
            foreach (var renderer in player.GetComponentsInChildren<Renderer>())
            {
                renderer.forceRenderingOff = false;
            }

            try
            {
                _playerAttachPoint.DetachPlayer();
            }
            // This happens if the ship was just destroyed
            catch (Exception)
            {
                var playerController = Locator.GetPlayerController();
                var playerTransform = Locator.GetPlayerTransform();
                playerController.SetColliderActivation(true);
                playerTransform.parent = null;
                PlayerBody.MakeNonKinematic();
                PlayerBody.SetVelocity(_dummyPlayer.GetAttachedOWRigidbody(false).GetPointVelocity(_dummyPlayer.transform.position));
                playerController.UnlockMovement();
                GlobalMessenger.FireEvent("DetachPlayerFromPoint");

                _modelShip.GetComponent<PromptHandler>().ShowPrompts(false);
            }

            PlayerBody.WarpToPositionRotation(_dummyPlayer.transform.position, _dummyPlayer.transform.rotation);

            GameObject.Destroy(_dummyPlayer);

            _playerResources._invincible = false;
            Locator.GetDeathManager()._invincible = false;

            GlobalMessenger.FireEvent("PlayerRepositioned");

            if (!Physics.autoSyncTransforms)
            {
                Physics.SyncTransforms();
            }

            // Put load radius back to normal
            GameObject.Find("TimberHearth_Body/Sector_TH/Sector_Streaming").GetComponent<SectorStreaming>()._softLoadRadius = 2500;
            GameObject.Find("TimberHearth_Body/Sector_TH").GetComponent<SphereShape>().radius = 1500;

            if (_isSuited)
            {
                Locator.GetPlayerSuit().SuitUp();
            }
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if (camera.gameObject.name.Equals("PlayerCamera") && IsPilotingModelShip)
            {
                // Send us back to the piloting camera
                // Has to be next tick else the game gets mad about recursive FireEvent calls
                ModHelper.Events.Unity.FireOnNextUpdate(EnterCameraView);
            }
        }

        private void Update()
        {
            if (!IsPilotingModelShip) return;

            if (OWInput.IsNewlyPressed(InputLibrary.rollMode)) IsRollMode = true;
            if (OWInput.IsNewlyReleased(InputLibrary.rollMode)) IsRollMode = false;

            // Make sure cheats and debug doesnt turn it off smh
            _playerResources._invincible = true;
            Locator.GetDeathManager()._invincible = true;
        }
    }
}
