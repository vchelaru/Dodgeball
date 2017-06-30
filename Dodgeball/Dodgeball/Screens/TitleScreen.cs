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
        enum MenuItem { Play, Settings, Exit }
        enum SettingsMenuItem { Volume, Back }
        private SettingsMenuItem SettingHighlightedSelection = SettingsMenuItem.Volume;
        private MenuItem HighlightedSelection = MenuItem.Play;

        void CustomInitialize()
        {


        }

        void CustomActivity(bool firstTimeCalled)
        {
            bool isSettingsPopupVisible = SettingsComponentInstance.Visible;
            if(isSettingsPopupVisible)
            {
                SettingsPopupActivity();
            }
            else
            {
                foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
                {
                    HandleGamePadInput(gamePad, ref HighlightedSelection);
                    TryHandleSelectConfirm(gamePad);
                }
            }
        }

        private void SettingsPopupActivity()
        {
            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                SettingsMenuHandleGamePadInput(gamePad.LeftStick.UpAsButton, gamePad.LeftStick.DownAsButton, ref SettingHighlightedSelection);
                if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    if (SettingHighlightedSelection == SettingsMenuItem.Volume)
                    {
                        //handle volume control
                    }
                    if (SettingHighlightedSelection == SettingsMenuItem.Back)
                    {
                        OnSettingsComponentInstanceSettingsButtonInstanceClick(SettingsButtonInstance);
                    }
                }
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
                if (HighlightedSelection == MenuItem.Settings)
                {
                    SettingsComponentInstance.Visible = true;

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
            if(gamePad.ButtonPushed(Xbox360GamePad.Button.DPadUp) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Up))
            {
                if (highlightedSelection == MenuItem.Settings)
                {
                    highlightedSelection = MenuItem.Play;
                }
                else if (highlightedSelection == MenuItem.Exit)
                {
                    highlightedSelection = MenuItem.Settings;
                }
            }
            if (gamePad.ButtonPushed(Xbox360GamePad.Button.DPadDown) || gamePad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Down))
            {
                if (highlightedSelection == MenuItem.Play)
                {
                    highlightedSelection = MenuItem.Settings;
                }
                else if (highlightedSelection == MenuItem.Settings)
                {
                    highlightedSelection = MenuItem.Exit;
                }

            }
            if (highlightedSelection == MenuItem.Play)
            {
                SettingsButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                SettingsButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                ExitButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                ExitButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                PlayButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                if (PlayButtonInstance.PulseAnimation.IsPlaying() == false) { PlayButtonInstance.PulseAnimation.Play(); }
            }
            if (highlightedSelection == MenuItem.Settings)
            {
                PlayButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                PlayButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                ExitButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                ExitButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                SettingsButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                if (SettingsButtonInstance.PulseAnimation.IsPlaying() == false) { SettingsButtonInstance.PulseAnimation.Play(); }
            }
            if (highlightedSelection == MenuItem.Exit)
            {
                SettingsButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                SettingsButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                PlayButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.Off;
                PlayButtonInstance.CurrentSizeCategoryState = GumRuntimes.TextButtonRuntime.SizeCategory.Regular;
                ExitButtonInstance.CurrentHighlightCategoryState = GumRuntimes.TextButtonRuntime.HighlightCategory.On;
                if (ExitButtonInstance.PulseAnimation.IsPlaying() == false) { ExitButtonInstance.PulseAnimation.Play(); }
            }
        }

        void SettingsMenuHandleGamePadInput(IPressableInput upPress, IPressableInput downPress, ref SettingsMenuItem SettingHighlightedSelection)
        {
            if (upPress.WasJustReleased)
            {
                if (SettingHighlightedSelection == SettingsMenuItem.Back)
                {
                    SettingHighlightedSelection = SettingsMenuItem.Volume;
                }
            }
            if (downPress.WasJustReleased)
            {
                if (SettingHighlightedSelection == SettingsMenuItem.Volume)
                {
                    SettingHighlightedSelection = SettingsMenuItem.Back;
                }
            }
            if (SettingHighlightedSelection == SettingsMenuItem.Volume)
            {
                SettingsComponentInstance.CurrentHighlightCategoryState = GumRuntimes.SettingsComponentRuntime.HighlightCategory.On;

            }
            if (SettingHighlightedSelection == SettingsMenuItem.Back)
            {
                SettingsComponentInstance.CurrentHighlightCategoryState = GumRuntimes.SettingsComponentRuntime.HighlightCategory.Off;
            }
        }

    }
    }
