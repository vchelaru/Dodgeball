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
    public partial class CharacterSelectScreen
    {
        #region Enums



        #endregion

        #region Fields/Properties

        private JoinStatus[] JoinStatuses;
        private IPressableInput[] LeftInputs;
        private IPressableInput[] RightInputs;

        private GraphicalUiElement[] markers;

        #endregion

        void CustomInitialize()
        {
            JoinStatuses = new JoinStatus[]
            {
                JoinStatus.Undecided,
                JoinStatus.Undecided,
                JoinStatus.Undecided,
                JoinStatus.Undecided
            };

            markers = TeamSelectionBoxesInstance.Children
                .Where(item => item.Name.StartsWith("Player") && item.Name.EndsWith("Marker"))
                .OrderBy(item => item.Name)
                .Select(item => item as GraphicalUiElement)
                .ToArray();


            InitializeInput();

        }
        private void InitializeInput()
        {
            LeftInputs = new IPressableInput[4];
            RightInputs = new IPressableInput[4];

            for (int i = 0; i < 4; i++)
            {
                var gamepad = InputManager.Xbox360GamePads[i];

                LeftInputs[i] = gamepad.LeftStick.LeftAsButton
                    .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadLeft));
                
                RightInputs[i] = gamepad.LeftStick.RightAsButton
                    .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadRight));
            }
        }

        void CustomActivity(bool firstTimeCalled)
        {
            
            for(int i = 0; i < 4; i++)
            {
                var gamepad = InputManager.Xbox360GamePads[i];
                markers[i].Visible = gamepad.IsConnected;

                if (InputManager.Xbox360GamePads[i].IsConnected)
                {
                    HandleGamePadInput(RightInputs[i], LeftInputs[i], ref JoinStatuses[i], markers[i]);
                }
            }
            
            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    bool hasAnyUndecided = GetIfHasAnyUndecided();

                    if (hasAnyUndecided == false)
                    {
                        GlobalContent.button_click.Play();
                        TeamSelectionBoxesInstance.SelectTeamVisible = false;
                        TeamSelectionBoxesInstance.LoadingVisible = true;

                        for(int i = 0; i < 4; i++)
                        {
                            GlobalData.JoinStatuses[i] = this.JoinStatuses[i];
                        }

                        // Using Call makes the moving to screen happen next frame
                        this.Call(() => MoveToScreen(typeof(GameScreen))).After(0);
                    }
                }
            }
        }

        private bool GetIfHasAnyUndecided()
        {
            bool hasAnyUndecided = false;
            for (int i = 0; i < 4; i++)
            {
                if (JoinStatuses[i] == JoinStatus.Undecided && InputManager.Xbox360GamePads[i].IsConnected)
                {
                    hasAnyUndecided = true;
                    break;
                }
            }

            return hasAnyUndecided;
        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }
        void HandleGamePadInput(IPressableInput rightPress, IPressableInput leftPress, ref JoinStatus joinStatus, GraphicalUiElement playerMarker)
        {
            if (rightPress.WasJustPressed)
            {
                if (joinStatus == JoinStatus.Team1)
                {
                    joinStatus = JoinStatus.Undecided;
                }
                else if (joinStatus == JoinStatus.Undecided)
                {
                    joinStatus = JoinStatus.Team2;
                }
            }

            if (leftPress.WasJustPressed)
            {
                if (joinStatus == JoinStatus.Team2)
                {
                    joinStatus = JoinStatus.Undecided;
                }
                else if (joinStatus == JoinStatus.Undecided)
                {
                    joinStatus = JoinStatus.Team1;
                }

            }
            if (joinStatus == JoinStatus.Team1)
            {
                playerMarker.X = -650;
            }
            if (joinStatus == JoinStatus.Team2)
            {
                playerMarker.X = 650;
            }
            if (joinStatus == JoinStatus.Undecided)
            {
                playerMarker.X = 0;
            }
        }
    }
}
