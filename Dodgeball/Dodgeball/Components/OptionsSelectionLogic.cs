using Dodgeball.GumRuntimes;
using FlatRedBall.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodgeball.Components
{
    class OptionsSelectionLogic
    {
        TextButtonRuntime firstButton;
        TextButtonRuntime secondButton;

        public bool DidPushAButton { get; private set; }

        int selectedOption = -1;
        public int SelectedOption
        {
            get
            {
                return selectedOption;
            }
            set
            {
                if(selectedOption != value)
                {
                    selectedOption = value;

                    TextButtonRuntime buttonToTurnOff = null;
                    TextButtonRuntime buttonToTurnOn = null;

                    if (selectedOption == 0)
                    {
                        buttonToTurnOn = firstButton;
                        buttonToTurnOff = secondButton;
                    }
                    if (selectedOption == 1)
                    {
                        buttonToTurnOn = secondButton;
                        buttonToTurnOff = firstButton;
                    }

                    if(buttonToTurnOff != null)
                    {
                        buttonToTurnOff.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                        buttonToTurnOff.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                        buttonToTurnOff.PulseAnimation.Stop();

                        buttonToTurnOn.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                        if (buttonToTurnOn.PulseAnimation.IsPlaying() == false)
                        {
                            buttonToTurnOn.PulseAnimation.Play();
                        }

                    }
                }
            }
        }


        // normally I'd write something to handle any number of options, but the game only
        // uses 2, so I'll code against that for simplicity
        public OptionsSelectionLogic(TextButtonRuntime first, TextButtonRuntime second)
        {
            this.firstButton = first;
            this.secondButton = second;

            SelectedOption = 0;
        }


        public void Activity()
        {
            DidPushAButton = false;

            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                HandleGamePadInput(gamePad);

                if(gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    DidPushAButton = true;
                }
            }


        }

        private void HandleGamePadInput(Xbox360GamePad gamePad)
        {
            if (gamePad.ButtonPushed(Xbox360GamePad.Button.DPadUp) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Up) ||
                gamePad.ButtonPushed(Xbox360GamePad.Button.DPadDown) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Down))
            {
                if (SelectedOption == 0)
                {
                    SelectedOption = 1;
                }
                else 
                {
                    SelectedOption = 0;
                }
            }
        }
    }
}
