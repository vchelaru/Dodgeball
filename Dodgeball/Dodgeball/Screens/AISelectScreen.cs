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

	    private int Team0Difficulty = 5;
	    private int Team1Difficulty = 5;

        private bool Team0Ready => AISelectBoxesInstance.CurrentTeam0ArrowsState ==
                                   AISelectBoxesRuntime.Team0Arrows.Ready ||
            AISelectBoxesInstance.CurrentTeamsPresentState == AISelectBoxesRuntime.TeamsPresent.NoTeam0;

        private bool Team1Ready => AISelectBoxesInstance.CurrentTeam1ArrowsState ==
                                   AISelectBoxesRuntime.Team1Arrows.Ready ||
                                   AISelectBoxesInstance.CurrentTeamsPresentState == AISelectBoxesRuntime.TeamsPresent.NoTeam1;

	    private bool BothTeamsHaveAIButOnlyOneTeamHasPlayers { get; set; }

	    private IPressableInput[] Team0UpInputs, Team0DownInputs, Team0AcceptInputs, Team0CancelInputs;
        private IPressableInput[] Team1UpInputs, Team1DownInputs, Team1AcceptInputs, Team1CancelInputs;

        private int NumberOfTeam0Controllers => Team0AcceptInputs.Count(i => i != null);
	    private int NumberOfTeam1Controllers => Team1AcceptInputs.Count(i => i != null);

        void CustomInitialize()
		{
		    InitializeInput();
		    AlterInputsAndDisplayIfNeeded();
		    UpdateDifficultyDisplay();
		}

	    private void AlterInputsAndDisplayIfNeeded()
	    {
	        if (NumberOfTeam0Controllers == 4)
	        {//Four players on a team means no AI
                AISelectBoxesInstance.CurrentTeamsPresentState = AISelectBoxesRuntime.TeamsPresent.NoTeam0;
	            Team1UpInputs = Team0UpInputs;
	            Team1DownInputs = Team0DownInputs;
	            Team1AcceptInputs = Team0AcceptInputs;
                Team1CancelInputs = Team0CancelInputs;
	            BothTeamsHaveAIButOnlyOneTeamHasPlayers = false;
	        }
            else if (NumberOfTeam1Controllers == 4)
	        {//Four players on a team means no AI
                AISelectBoxesInstance.CurrentTeamsPresentState = AISelectBoxesRuntime.TeamsPresent.NoTeam1;
	            Team0UpInputs = Team1UpInputs;
	            Team0DownInputs = Team1DownInputs;
	            Team0AcceptInputs = Team1AcceptInputs;
	            Team0CancelInputs = Team1CancelInputs;
	            BothTeamsHaveAIButOnlyOneTeamHasPlayers = false;
            }
            else if (NumberOfTeam0Controllers == 0 || NumberOfTeam1Controllers == 0)
	        {//Less than four player on one team, and 0 on the other means AI on both teams
	            BothTeamsHaveAIButOnlyOneTeamHasPlayers = true;
	        }
	    }

	    private void InitializeInput()
	    {
	        //Assign Team0's up/down inputs
	        Team0UpInputs = new IPressableInput[4];
	        Team0DownInputs = new IPressableInput[4];
	        Team0AcceptInputs = new IPressableInput[4];
	        Team0CancelInputs = new IPressableInput[4];

            //Assign Team1's up/down inputs
            Team1UpInputs = new IPressableInput[4];
	        Team1DownInputs = new IPressableInput[4];
	        Team1AcceptInputs = new IPressableInput[4];
	        Team1CancelInputs = new IPressableInput[4];

	        bool hasAnyAssigned = GlobalData.JoinStatuses.Any(item => item != JoinStatus.Undecided);

	        if (hasAnyAssigned)
	        {
	            for (int i = 0; i < 4; i++)
	            {
	                var gamepad = InputManager.Xbox360GamePads[i];
	                var joinStatus = GlobalData.JoinStatuses[i];
	                if (joinStatus != JoinStatus.Undecided && gamepad.IsConnected)
	                {
	                    int teamToJoin;
	                    if (joinStatus == JoinStatus.Team1)
	                    {
	                        teamToJoin = 0;
	                    }
	                    else
	                    {
	                        teamToJoin = 1;
	                    }

	                    if (teamToJoin == 0)
	                    {
	                        Team0UpInputs[i] = gamepad.LeftStick.LeftAsButton
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadUp));

	                        Team0DownInputs[i] = gamepad.LeftStick.RightAsButton
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadDown));

	                        Team0AcceptInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.A)
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.Start));

	                        Team0CancelInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.B)
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.Back));
                        }
	                    else
	                    {
	                        Team1UpInputs[i] = gamepad.LeftStick.LeftAsButton
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadUp));

	                        Team1DownInputs[i] = gamepad.LeftStick.RightAsButton
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.DPadDown));

	                        Team1AcceptInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.A)
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.Start));

	                        Team1CancelInputs[i] = gamepad.GetButton(Xbox360GamePad.Button.B)
	                            .Or(gamepad.GetButton(Xbox360GamePad.Button.Back));
                        }
	                }
	            }
	        }
	        else
	        {
                // didn't go through the screen to assign characters, so let's just move on to the next screen
	            MoveToGameScreen();
	        }
        }

	    private void UpdateDifficultyDisplay()
	    {
            //TODO:  Only show difficulty for teams which have AI players

	        AISelectBoxesInstance.Team0TokenText = Team0Difficulty.ToString();
	        AISelectBoxesInstance.Team1TokenText = Team1Difficulty.ToString();
	    }

        void CustomActivity(bool firstTimeCalled)
        {
            if (BothTeamsHaveAIButOnlyOneTeamHasPlayers)
            {
                AnyTeamInputActivity();
            }
            else
            {
                Team0InputActivity();
                Team1InputActivity();
            }

            UpdateDifficultyDisplay();
            DecideOnMovingToScreen();
        }

	    private void AnyTeamInputActivity()
	    {
	        if (Team0Ready)
	        {
	            if (Team0CancelInputs.Any(input => input != null && input.WasJustPressed) || Team1CancelInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam0ArrowsState = AISelectBoxesRuntime.Team0Arrows.NoHighlight;
	            }
                else if (Team0UpInputs.Any(input => input != null && input.WasJustPressed) || Team1UpInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam1Animation.Play();
	                Team1Difficulty++;
	                Team1Difficulty = Team1Difficulty > MaxDifficulty ? MaxDifficulty : Team1Difficulty;
	            }
                else if (Team0DownInputs.Any(input => input != null && input.WasJustPressed) || Team1DownInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam1Animation.Play();
	                Team1Difficulty--;
	                Team1Difficulty = Team1Difficulty < MinDifficulty ? MinDifficulty : Team1Difficulty;
	            }
                else if (Team0AcceptInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                GlobalContent.button_click.Play();
	                AISelectBoxesInstance.CurrentTeam1ArrowsState = AISelectBoxesRuntime.Team1Arrows.Ready;
	            }
            }
	        else
	        {
	            if (Team0UpInputs.Any(input => input != null && input.WasJustPressed) || Team1UpInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam0Animation.Play();
	                Team0Difficulty++;
	                Team0Difficulty = Team0Difficulty > MaxDifficulty ? MaxDifficulty : Team0Difficulty;
	            }
                else if (Team0DownInputs.Any(input => input != null && input.WasJustPressed) || Team1DownInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam0Animation.Play();
	                Team0Difficulty--;
	                Team0Difficulty = Team0Difficulty < MinDifficulty ? MinDifficulty : Team0Difficulty;
	            }
                else if (Team0AcceptInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                GlobalContent.button_click.Play();
	                AISelectBoxesInstance.CurrentTeam0ArrowsState = AISelectBoxesRuntime.Team0Arrows.Ready;
	            }
	        }
        }

	    private void Team0InputActivity()
	    {
	        if (Team0Ready)
	        {
	            if (Team0CancelInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam0ArrowsState = AISelectBoxesRuntime.Team0Arrows.NoHighlight;
	            }
	        }
	        else
	        {
	            if (Team0UpInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam0Animation.Play();
	                Team0Difficulty++;
	                Team0Difficulty = Team0Difficulty > MaxDifficulty ? MaxDifficulty : Team0Difficulty;
	            }

	            if (Team0DownInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam0Animation.Play();
	                Team0Difficulty--;
	                Team0Difficulty = Team0Difficulty < MinDifficulty ? MinDifficulty : Team0Difficulty;
	            }

	            if (Team0AcceptInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                GlobalContent.button_click.Play();
                    AISelectBoxesInstance.CurrentTeam0ArrowsState = AISelectBoxesRuntime.Team0Arrows.Ready;
                }
	        }
	    }

	    private void Team1InputActivity()
	    {
	        if (Team1Ready)
	        {
	            if (Team1CancelInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.CurrentTeam1ArrowsState = AISelectBoxesRuntime.Team1Arrows.NoHighlight;
	            }
	        }
	        else
	        {
	            if (Team1UpInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.IncrementTeam1Animation.Play();
	                Team1Difficulty++;
	                Team1Difficulty = Team1Difficulty > MaxDifficulty ? MaxDifficulty : Team1Difficulty;
	            }

	            if (Team1DownInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                AISelectBoxesInstance.DecrementTeam1Animation.Play();
	                Team1Difficulty--;
	                Team1Difficulty = Team1Difficulty < MinDifficulty ? MinDifficulty : Team1Difficulty;
	            }

	            if (Team1AcceptInputs.Any(input => input != null && input.WasJustPressed))
	            {
	                GlobalContent.button_click.Play();
                    AISelectBoxesInstance.CurrentTeam1ArrowsState = AISelectBoxesRuntime.Team1Arrows.Ready;
	            }
	        }
        }

	    private void DecideOnMovingToScreen()
	    {
	        if (Team0Ready && Team1Ready)
	        {
	            MoveToGameScreen();
	        }
	    }

	    private void MoveToGameScreen()
	    {
	        GlobalContent.button_click.Play();
            GameStats.Team0AIDifficulty = Team0Difficulty;
	        GameStats.Team1AIDifficulty = Team1Difficulty;
	        this.Call(() => MoveToScreen(typeof(GameScreen))).After(0);
        }

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
