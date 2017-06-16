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
        enum JoinStatus { Team1, Undecided, Team2 };
        private JoinStatus Player1JoinStatus = JoinStatus.Undecided;
        private JoinStatus Player2JoinStatus = JoinStatus.Undecided;
        private JoinStatus Player3JoinStatus = JoinStatus.Undecided;
        private JoinStatus Player4JoinStatus = JoinStatus.Undecided;


        void CustomInitialize()
        {
            InitializeInput();

        }
        private void InitializeInput()
        {

        }

        void CustomActivity(bool firstTimeCalled)
        {
            var player1Marker = TeamSelectionBoxesInstance.Children.Find(item => item.Name == "Player1Marker") as GraphicalUiElement;
            var player2Marker = TeamSelectionBoxesInstance.Children.Find(item => item.Name == "Player2Marker") as GraphicalUiElement;
            var player3Marker = TeamSelectionBoxesInstance.Children.Find(item => item.Name == "Player3Marker") as GraphicalUiElement;
            var player4Marker = TeamSelectionBoxesInstance.Children.Find(item => item.Name == "Player4Marker") as GraphicalUiElement;
            if (InputManager.Xbox360GamePads[0].IsConnected)
            {
                HandleGamePadInput(InputManager.Xbox360GamePads[0].LeftStick.RightAsButton, InputManager.Xbox360GamePads[0].LeftStick.LeftAsButton, ref Player1JoinStatus, player1Marker);
                player1Marker.Visible = true;
            }
            else { player1Marker.Visible = false; }
            if (InputManager.Xbox360GamePads[1].IsConnected)
            {
                HandleGamePadInput(InputManager.Xbox360GamePads[1].LeftStick.RightAsButton, InputManager.Xbox360GamePads[1].LeftStick.LeftAsButton, ref Player2JoinStatus, player2Marker);
                player2Marker.Visible = true;
            }
            else { player1Marker.Visible = false; }
            if (InputManager.Xbox360GamePads[2].IsConnected)
            {
                HandleGamePadInput(InputManager.Xbox360GamePads[2].LeftStick.RightAsButton, InputManager.Xbox360GamePads[2].LeftStick.LeftAsButton, ref Player3JoinStatus, player3Marker);
                player3Marker.Visible = true;
            }
            else { player3Marker.Visible = false; }
            if (InputManager.Xbox360GamePads[3].IsConnected)
            {
                HandleGamePadInput(InputManager.Xbox360GamePads[3].LeftStick.RightAsButton, InputManager.Xbox360GamePads[3].LeftStick.LeftAsButton, ref Player4JoinStatus, player4Marker);
                player4Marker.Visible = true;
            }
            else { player4Marker.Visible = false; }
            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    if ((Player1JoinStatus != JoinStatus.Undecided || InputManager.Xbox360GamePads[0].IsConnected == false) &&
                        (Player2JoinStatus != JoinStatus.Undecided || InputManager.Xbox360GamePads[1].IsConnected == false) &&
                        (Player3JoinStatus != JoinStatus.Undecided || InputManager.Xbox360GamePads[2].IsConnected == false) &&
                        (Player4JoinStatus != JoinStatus.Undecided || InputManager.Xbox360GamePads[3].IsConnected == false))
                    {
                        TeamSelectionBoxesInstance.SelectTeamVisible = false;
                        TeamSelectionBoxesInstance.LoadingVisible = true;
                        this.Call(() => MoveToScreen(typeof(GameScreen))).After(0);
                    }

                }
            }
        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }
        void HandleGamePadInput(IPressableInput rightPress, IPressableInput leftPress, ref JoinStatus joinStatus, GraphicalUiElement playerMarker)
        {
            if (rightPress.WasJustReleased)
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
            if (leftPress.WasJustReleased)
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
