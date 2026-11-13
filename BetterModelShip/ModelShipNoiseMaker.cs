using UnityEngine;

namespace BetterModelShip
{
    public class ModelShipNoiseMaker : NoiseMaker
    {
        private ThrusterModel _thrusterModel;

        //Taken from the player jetpack, seems right for a smaller ship.
        private readonly AnimationCurve _thrustNoiseRadius = AnimationCurve.Linear(0f, 0f, 6f, 200f);

        public override void Awake()
        {
            base.Awake();

            _thrusterModel = GetComponent<ThrusterModel>();
            GlobalMessenger.AddListener("PlayerFogWarp", OnPlayerFogWarp);
        }

        private void Update()
        {
            _noiseRadius = _thrustNoiseRadius.Evaluate(_thrusterModel.GetLocalAcceleration().magnitude);
        }

        private void OnPlayerFogWarp()
        {
            if (BetterModelShip.IsPilotingModelShip)
            {
                OnFogWarp();
            }
        }

        private void OnDestroy()
        {
            GlobalMessenger.RemoveListener("PlayerFogWarp", OnPlayerFogWarp);
        }
    }
}