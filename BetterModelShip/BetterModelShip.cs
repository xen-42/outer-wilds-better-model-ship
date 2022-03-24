using OWML.ModHelper;
using OWML.Common;
using OWML.Utils;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using System;

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

        private bool _UpdateCameraNextTick = false;

        private void Start()
        {
            Instance = this;

            Patches.Apply();

            ModHelper.Console.WriteLine($"My mod {nameof(BetterModelShip)} is loaded!", MessageType.Success);

            GlobalMessenger<OWRigidbody>.AddListener("EnterRemoteFlightConsole", new Callback<OWRigidbody>(OnEnterRemoteFlightConsole));
            GlobalMessenger.AddListener("ExitRemoteFlightConsole", new Callback(OnExitRemoteFlightConsole));
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem") return;

            _modelShip = GameObject.Find("ModelRocket_Body");

            // Set up camera
            ModelShipCamera = new GameObject();
            ModelShipCamera.SetActive(false);

            _camera = ModelShipCamera.AddComponent<Camera>();
            _camera.enabled = false;

            _OWCamera = ModelShipCamera.AddComponent<OWCamera>();
            _OWCamera.renderSkybox = true;

            FlashbackScreenGrabImageEffect temp = ModelShipCamera.AddComponent<FlashbackScreenGrabImageEffect>();
            temp._downsampleShader = Locator.GetPlayerCamera().gameObject.GetComponent<FlashbackScreenGrabImageEffect>()._downsampleShader;

            PlanetaryFogImageEffect _image = ModelShipCamera.AddComponent<PlanetaryFogImageEffect>();
            _image.fogShader = Locator.GetPlayerCamera().gameObject.GetComponent<PlanetaryFogImageEffect>().fogShader;

            PostProcessingBehaviour _postProcessiong = ModelShipCamera.AddComponent<PostProcessingBehaviour>();
            _postProcessiong.profile = Locator.GetPlayerCamera().gameObject.GetAddComponent<PostProcessingBehaviour>().profile;

            ModelShipCamera.SetActive(true);
            _camera.CopyFrom(Locator.GetPlayerCamera().mainCamera);
            _camera.cullingMask &= ~(1 << 27) | (1 << 22) ;

            ModelShipCamera.name = "ModelShipCamera";

            ModelShipCamera.transform.parent = _modelShip.transform;
            ModelShipCamera.transform.position = _modelShip.transform.position;
            ModelShipCamera.transform.rotation = _modelShip.transform.rotation;
            ModelShipCamera.transform.localPosition = 3 * Vector3.back + Vector3.up;

            // By default the model ship only experiences gravity from Timber Hearth
            Destroy(_modelShip.transform.Find("Detector").GetComponent<ConstantForceDetector>());
            _modelShip.transform.Find("Detector").gameObject.AddComponent<DynamicForceDetector>();

            IsPilotingModelShip = false;
        }

        private void OnEnterRemoteFlightConsole(OWRigidbody _)
        {
            ModHelper.Console.WriteLine($"OnEnterRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = true;
            _previousCamera = Locator.GetActiveCamera();
            if (_OWCamera == null)
            {
                ModHelper.Console.WriteLine("OWCamera is null", MessageType.Error);
            }
            else
            {
                _camera.enabled = true;
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _OWCamera);
            }
        }

        private void OnExitRemoteFlightConsole()
        {
            ModHelper.Console.WriteLine($"OnExitRemoteFlightConsole", MessageType.Info);

            IsPilotingModelShip = false;
            if (_OWCamera == null)
            {
                ModHelper.Console.WriteLine("Previous camera is null", MessageType.Error);
            }
            else
            {
                _camera.enabled = false;
                GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _previousCamera);
            }
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if(camera != _OWCamera && IsPilotingModelShip)
            {
                _UpdateCameraNextTick = true;
            }
        }

        private void Update()
        {
            if(_UpdateCameraNextTick)
            {
                if(IsPilotingModelShip && Locator.GetActiveCamera() != _OWCamera)
                {
                    OnEnterRemoteFlightConsole(null);
                }
            }

            if (!IsPilotingModelShip) return;

            if (OWInput.IsNewlyPressed(InputLibrary.rollMode)) IsRollMode = true;
            if (OWInput.IsNewlyReleased(InputLibrary.rollMode)) IsRollMode = false;
        }
    }
}
