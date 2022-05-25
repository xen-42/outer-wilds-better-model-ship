using UnityEngine;

namespace BetterModelShip
{
    public class PromptHandler : MonoBehaviour, ILateInitializer
    {
        private ScreenPrompt _rollPrompt;
        private ScreenPrompt _flashLightPrompt;
        private ScreenPrompt _horizontalThrustPrompt;
        private ScreenPrompt _verticalThrustPrompt;

        private bool _screenPromptsInitialized;

        private bool _enabled;

        private void Awake()
        {
            _screenPromptsInitialized = false;
            LateInitializerManager.RegisterLateInitializer(this);

            _horizontalThrustPrompt = new ScreenPrompt(InputLibrary.moveXZ, UITextLibrary.GetString(UITextType.HorizontalPrompt), 0, ScreenPrompt.DisplayState.Normal, false);
            _rollPrompt = new ScreenPrompt(InputLibrary.rollMode, InputLibrary.look, $"<CMD1> {UITextLibrary.GetString(UITextType.HoldPrompt)}  +<CMD2>   {UITextLibrary.GetString(UITextType.RollPrompt)}",
                ScreenPrompt.MultiCommandType.CUSTOM_BOTH, 0, ScreenPrompt.DisplayState.Normal, false);
            _flashLightPrompt = new ScreenPrompt(InputLibrary.flashlight, $"<CMD> {UITextLibrary.GetString(UITextType.FlashlightPrompt)}");
            _verticalThrustPrompt = new ScreenPrompt(InputLibrary.thrustDown, InputLibrary.thrustUp, UITextLibrary.GetString(UITextType.DownUpPrompt), ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);

            GlobalMessenger.AddListener("GamePaused", OnGamePaused);
            GlobalMessenger.AddListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);
        }

        public void LateInitialize()
        {
            _screenPromptsInitialized = true;
            Locator.GetPromptManager().AddScreenPrompt(_rollPrompt, PromptPosition.UpperLeft, false);
            Locator.GetPromptManager().AddScreenPrompt(_flashLightPrompt, PromptPosition.UpperLeft, false);
            Locator.GetPromptManager().AddScreenPrompt(_horizontalThrustPrompt, PromptPosition.UpperLeft, false);
            Locator.GetPromptManager().AddScreenPrompt(_verticalThrustPrompt, PromptPosition.UpperLeft, false);
        }

        private void OnDestroy()
        {
            if (!_screenPromptsInitialized)
            {
                LateInitializerManager.UnregisterLateInitializer(this);
                Locator.GetPromptManager().RemoveScreenPrompt(_horizontalThrustPrompt, PromptPosition.UpperLeft);
                Locator.GetPromptManager().RemoveScreenPrompt(_rollPrompt, PromptPosition.UpperLeft);
                Locator.GetPromptManager().RemoveScreenPrompt(_flashLightPrompt, PromptPosition.UpperLeft);
                Locator.GetPromptManager().RemoveScreenPrompt(_verticalThrustPrompt, PromptPosition.UpperLeft);
            }

            GlobalMessenger.RemoveListener("GamePaused", OnGamePaused);
            GlobalMessenger.RemoveListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);
        }

        private void ShowPrompts(bool visible)
        {
            _horizontalThrustPrompt.SetVisibility(visible);
            _rollPrompt.SetVisibility(visible);
            _flashLightPrompt.SetVisibility(visible);
            _verticalThrustPrompt.SetVisibility(visible);
        }

        private void Update()
        {
            ShowPrompts(_enabled && BetterModelShip.IsPilotingModelShip);
        }

        private void OnGamePaused()
        {
            _enabled = false;
        }

        private void OnGameUnpaused()
        {
            _enabled = true;
        }

        private void OnWakeUp()
        {
            _enabled = true;
        }
    }
}
