using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using Gum.Wireframe;

namespace Dodgeball.Screens
{
    public partial class TitleScreen
    {
        enum MenuItem { Play,  Exit }
        private MenuItem HighlightedSelection = MenuItem.Play;

        void CustomInitialize()
        {


        }

        void CustomActivity(bool firstTimeCalled)
        {
            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                HandleGamePadInput(gamePad, ref HighlightedSelection);
                TryHandleSelectConfirm(gamePad);
            }
        }
        
        private void TryHandleSelectConfirm(Xbox360GamePad gamePad)
        {
            if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
            {
                if (HighlightedSelection == MenuItem.Play)
                {
                    MoveToScreen(typeof(CharacterSelectScreen));
                }
                if (HighlightedSelection == MenuItem.Exit)
                {
                    FlatRedBallServices.Game.Exit();
                }
            }
        }


        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }
        void HandleGamePadInput(Xbox360GamePad gamePad, ref MenuItem highlightedSelection)
        {
            if(gamePad.ButtonPushed(Xbox360GamePad.Button.DPadUp) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Up) ||
                gamePad.ButtonPushed(Xbox360GamePad.Button.DPadDown) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Down))
            {
                if (highlightedSelection == MenuItem.Play)
                {
                    highlightedSelection = MenuItem.Exit;
                }
                else if (highlightedSelection == MenuItem.Exit)
                {
                    highlightedSelection = MenuItem.Play;
                }
            }
            if (highlightedSelection == MenuItem.Play)
            {
                ExitButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                ExitButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                PlayButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                if (PlayButtonInstance.PulseAnimation.IsPlaying() == false) { PlayButtonInstance.PulseAnimation.Play(); }
            }
            if (highlightedSelection == MenuItem.Exit)
            {
                PlayButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                PlayButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                ExitButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                if (ExitButtonInstance.PulseAnimation.IsPlaying() == false) { ExitButtonInstance.PulseAnimation.Play(); }
            }
        }


    }
}
