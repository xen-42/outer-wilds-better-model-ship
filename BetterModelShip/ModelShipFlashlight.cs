using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterModelShip
{
    public class ModelShipFlashlight : MonoBehaviour
    {
        Light _light;
        public void Awake()
        {
            _light = gameObject.AddComponent<Light>();
            _light.range = 50f;
        }

        public void Update()
        {
            if (_light.enabled)
            {
                if (!BetterModelShip.IsPilotingModelShip)
                {
                    _light.enabled = false;
                }
            }
            
            if(BetterModelShip.IsPilotingModelShip)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.flashlight))
                {
                    _light.enabled = !_light.enabled;
                    if (_light.enabled)
                    {
                        Locator.GetPlayerAudioController().PlayTurnOnFlashlight();
                    }
                    else
                    {
                        Locator.GetPlayerAudioController().PlayTurnOffFlashlight();
                    }
                }
            }
        }
    }
}
