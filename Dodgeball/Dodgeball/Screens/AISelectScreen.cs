using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dodgeball.DataRuntime;
using Dodgeball.GumRuntimes;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;



namespace Dodgeball.Screens
{
	public partial class AISelectScreen
	{
	    private int MaxDifficulty = 9;
	    private int MinDifficulty = 0;

	    private int Team1Difficulty = 5;
	    private int Team2Difficulty = 5;

        private bool Team1Ready => AISelectBoxesInstance.CurrentTeam1ArrowsState ==
                                   AISelectBoxesRuntime.Team1Arrows.AllHighlight;

        private bool Team2Ready => AISelectBoxesInstance.CurrentTeam2ArrowsState ==
                                   AISelectBoxesRuntime.Team2Arrows.AllHighlight;

        private IPressableInput[] Team1UpInputs, Team1DownInputs, Team1AcceptInputs, Team1CancelInputs;
        private IPressableInput[] Team2UpInputs, Team2DownInputs, Team2AcceptInputs, Team2CancelInputs;

        void CustomInitialize()
		{
		    InitializeInput();
		    UpdateDifficultyDisplay();
		}

	    private void InitializeInput()
	    {
            //Assign Team1's up/down inputs
	        Team1UpInputs = new IPressableInput[4];
	        Team1DownInputs = new IPressableInput[4];
	        Team1AcceptInputs = new IPressableInput[4];
	        Team1CancelInputs = new IPressableInput[4];

            for (int i = 0; i < 4; i++)
	        {
	            //TODO:  Use actual controllers from team as assigned in previous screen
                var gamepad = InputManager.Xbox360GamePads[i];

	            Team1UpInputs[i] = gamepad.LeftStick.LeftAsButton
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadUp));

	            Team1DownInputs[i] = gamepad.LeftStick.RightAsButton
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadDown));

	            Team1AcceptInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.A)
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.Start));

	            Team1CancelInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.B)
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.Back));
            }


            //Assign Team2's up/down inputs
	        Team2UpInputs = new IPressableInput[4];
	        Team2DownInputs = new IPressableInput[4];
	        Team2AcceptInputs = new IPressableInput[4];
	        Team2CancelInputs = new IPressableInput[4];

            for (int i = 0; i < 4; i++)
	        {
	            //TODO:  Use actual controllers from team as assigned in previous screen
                var gamepad = InputManager.Xbox360GamePads[i+4];

	            Team2UpInputs[i] = gamepad.LeftStick.LeftAsButton
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadUp));

	            Team2DownInputs[i] = gamepad.LeftStick.RightAsButton
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadDown));

	            Team2AcceptInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.A)
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.Start));

	            Team2CancelInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.B)
	                .Or(gamepad.GetButton(Xbox360GamePad.Button.Back));
            }
        }

	    private void UpdateDifficultyDisplay()
	    {
            //TODO:  Only show difficulty for teams which have AI players

	        AISelectBoxesInstance.Team1TokenText = Team1Difficulty.ToString();
	        AISelectBoxesInstance.Team2TokenText = Team2Difficulty.ToString();
	    }

        void CustomActivity(bool firstTimeCalled)
        {
            Team1InputActivity();
            Team2InputActivity();
            UpdateDifficultyDisplay();
            DecideOnMovingToScreen();
        }

	    private void Team1InputActivity()
	    {
	        if (Team1Ready)
	        {
	            if (Team1CancelInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam1ArrowsState = AISelectBoxesRuntime.Team1Arrows.NoHighlight;
	            }
	        }
	        else
	        {
	            if (Team1UpInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam1Animation.Play();
	                Team1Difficulty++;
	                Team1Difficulty = Team1Difficulty > MaxDifficulty ? MaxDifficulty : Team1Difficulty;
	            }

	            if (Team1DownInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam1Animation.Play();
	                Team1Difficulty--;
	                Team1Difficulty = Team1Difficulty < MinDifficulty ? MinDifficulty : Team1Difficulty;
	            }

	            if (Team1AcceptInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam1ArrowsState = AISelectBoxesRuntime.Team1Arrows.AllHighlight;
                }
	        }
	    }

	    private void Team2InputActivity()
	    {
	        if (Team2Ready)
	        {
	            if (Team2CancelInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam2ArrowsState = AISelectBoxesRuntime.Team2Arrows.NoHighlight;
	            }
	        }
	        else
	        {
	            if (Team2UpInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam2Animation.Play();
	                Team2Difficulty++;
	                Team2Difficulty = Team2Difficulty > MaxDifficulty ? MaxDifficulty : Team2Difficulty;
	            }

	            if (Team2DownInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam2Animation.Play();
	                Team2Difficulty--;
	                Team2Difficulty = Team2Difficulty < MinDifficulty ? MinDifficulty : Team2Difficulty;
	            }

	            if (Team2AcceptInputs.Any(input => input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam2ArrowsState = AISelectBoxesRuntime.Team2Arrows.AllHighlight;
	            }
	        }
        }

	    private void DecideOnMovingToScreen()
	    {
	        if (Team1Ready && Team2Ready)
	        {
	            GameStats.Team1AIDifficulty = Team1Difficulty;
	            GameStats.Team2AIDifficulty = Team2Difficulty;
                this.Call(() => MoveToScreen(typeof(GameScreen))).After(0);
            }
	    }


        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
