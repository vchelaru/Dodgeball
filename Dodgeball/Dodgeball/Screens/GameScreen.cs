using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dodgeball.AI;
using Dodgeball.Entities;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;
using Dodgeball.DataRuntime;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;

namespace Dodgeball.Screens
{
	public partial class GameScreen
	{
        #region Fields/Properties

        private SoundEffectInstance playerHitSound;
	    private List<AIController> AIControllers;

	    private float PlayAreaTop => -WorldComponentInstance.PlayArea.Y + (FlatRedBall.Camera.Main.OrthogonalHeight/2);
	    private float PlayAreaBottom => PlayAreaTop - WorldComponentInstance.PlayArea.Height;

        
        #endregion

        #region Initialize

        void CustomInitialize()
        {
            playerHitSound = GlobalContent.player_hit_0.CreateInstance();
            SharePlayerReferences();

            AssignAIControllers();

            InitializeInput();

            InitializePlayerEvents();
        }

        private void AssignAIControllers()
	    {
	        AIControllers = new List<AIController>();

            foreach (var player in this.PlayerList)
	        {
	            var newAI = new AIController(player, BallInstance);
                AIControllers.Add(newAI);
	        }
        }

	    private void SharePlayerReferences()
        {
            foreach(var player in this.PlayerList)
            {
                player.AllPlayers = PlayerList;
                player.WorldComponent = WorldComponentInstance;
            }
        }

        private void InitializeInput()
        {
            if(InputManager.NumberOfConnectedGamePads != 0)
            {
                if (InputManager.Xbox360GamePads[0].IsConnected) { Player1.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]); }
                if (InputManager.Xbox360GamePads[1].IsConnected) { Player2.InitializeXbox360Controls(InputManager.Xbox360GamePads[1]); }
                if (InputManager.Xbox360GamePads[2].IsConnected) { Player3.InitializeXbox360Controls(InputManager.Xbox360GamePads[2]); }
                if (InputManager.Xbox360GamePads[3].IsConnected) { Player4.InitializeXbox360Controls(InputManager.Xbox360GamePads[3]); }
            }
            else
            {
                Player1.InitializeKeyboardControls();
            }

        }

        private void InitializePlayerEvents()
        {
            foreach(var player in PlayerList)
            {
                player.Dying += () => ShowNumberOfPlayersForTeam(player.TeamIndex);
            }
        }

        #endregion

        #region Activity

        void CustomActivity(bool firstTimeCalled)
		{
            CollisionActivity();

		    AIActivity();

            CheckForEndOfGame();

#if DEBUG
            DebugActivity();
#endif
        }

	    private void AIActivity()
	    {
	        foreach (var AI in AIControllers)
	        {
	            AI.Activity();
	        }
	    }

#if DEBUG
        private void DebugActivity()
        {
            if(InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.R))
            {
                this.RestartScreen(reloadContent: true);
            }
        }
#endif

        private void CollisionActivity()
        {
            PlayerVsPlayerCollision();

            if (BallInstance.CurrentOwnershipState != Ball.OwnershipState.Held)
            {
                BallVsPlayerCollision();

                BallVsWallsCollision();
            }
        }

	    private void PlayerVsPlayerCollision()
	    {
	        // Destroys aren't being called here so we can forward-loop
	        for (var i = 0; i < PlayerList.Count; i++)
	        {
	            var firstPlayer = PlayerList[i];
                if(firstPlayer.IsDying == false)
                {
	                for (var j = i + 1; j < PlayerList.Count; j++)
	                {
	                    var secondPlayer = PlayerList[j];

                        if(secondPlayer.IsDying == false)
                        {
	                        firstPlayer.CircleInstance.CollideAgainstBounce(
	                            secondPlayer.CircleInstance, 1, 1, 0.1f);
                        }
	                }
                }
	        }
        }

        private void BallVsWallsCollision()
        {
            if (BallInstance.XVelocity < 0 && BallInstance.X < -1920/2.0f + 30 ||
                BallInstance.XVelocity > 0 && BallInstance.X > 1920 / 2.0f - 30)
            { 
                BallInstance.BounceOffWall(isLeftOrRightWall: true);
            }

            if(BallInstance.YVelocity > 0 && BallInstance.Y > PlayAreaTop ||
                BallInstance.YVelocity < 0 && BallInstance.Y < PlayAreaBottom + 30)
            {
                BallInstance.BounceOffWall(isLeftOrRightWall: false);
            }
        }

        private void BallVsPlayerCollision()
        {
            bool isAbovePlayers = BallInstance.Altitude > Player.PlayerHeight;

            if (isAbovePlayers == false)
            {
                if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Free)
                {
                    //Can't catch a ball you're dodging or hit
                    var validPlayers = PlayerList.Where(player => !player.IsDodging && !player.IsHit).ToList();
                    foreach (var player in validPlayers)
                    {
                        if (player.IsDying == false && player.CollideAgainst(BallInstance))
                        {
                            PerformPickupLogic(player);

                            break;
                        }
                    }
                }
                else if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Thrown)
                {
                    // reverse loop since players can be removed:
                    for (int i = PlayerList.Count - 1; i > -1; i--)
                    {
                        var player = PlayerList[i];

                        if (BallInstance.ThrowOwner != player &&
                            player.IsDying == false && 
                            player.IsDodging == false &&
                            player.IsHit == false &&
                            player.CollideAgainst(BallInstance))
                        {
                            if (player.IsAttemptingCatch && player.CatchIsEffective)
                            {
                                PerformCatchBallLogic(player);
                            }
                            else
                            {
                                PerformGetHitLogic(player);
                            }
                        }
                    }
                }
                // don't perform collision if the ball is being held
            }
        }

	    private void PerformCatchBallLogic(Player player)
	    {
	        player.CatchBall(BallInstance);

            #if DEBUG
            if (DebuggingVariables.PlayerAlwaysControlsBallholder)
	        {
	            foreach (var playerToClear in PlayerList)
	            {
	                playerToClear.ClearInput();
	            }

	            if (InputManager.NumberOfConnectedGamePads != 0)
	            {
	                player.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
	            }
	            else
	            {
	                Player1.InitializeKeyboardControls();
	            }
	        }
            #endif
	    }

	    private void PerformGetHitLogic(Entities.Player player)
        {
            //Let player react and determine if they'll take damage
            player.GetHitBy(BallInstance);

            //Make a hit sound
            var ballVelocity = BallInstance.Velocity.Length();
            var maxVelocity = player.MaxThrowVelocity;
            var playerHitPan = MathHelper.Clamp(player.X / 540f, -1, 1);
            var playerHitVol = MathHelper.Clamp((ballVelocity * 2) / maxVelocity, 0.1f, 1);

            SetPlayerHitSoundByVelocity(ballVelocity, maxVelocity);

            playerHitSound.Pan = playerHitPan;
            playerHitSound.Volume = playerHitVol;
            playerHitSound.Play();

            // make the ball bounce off the player:
            BallInstance.CollideAgainstBounce(player, 0, 1, 1);
            BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
        }

	    private void SetPlayerHitSoundByVelocity(float ballVelocity, float maxVelocity)
	    {
	        var pctOfPossibleVelocity = ballVelocity / maxVelocity;
	        var hitIndex = Convert.ToInt32(pctOfPossibleVelocity * 8);
	        hitIndex = MathHelper.Clamp(hitIndex, 0, 8);

	        var playerHitSoundName = $"player_hit_{hitIndex}";

	        var hitSound = GlobalContent.GetFile(playerHitSoundName) as SoundEffect;
            
	        if (hitSound != null)
	        {
	            playerHitSound = hitSound.CreateInstance();
	        }
	    }

	    private void PerformPickupLogic(Entities.Player player)
        {
            player.PickUpBall(BallInstance);

            #if DEBUG
            if (DebuggingVariables.PlayerAlwaysControlsBallholder)
            {
                foreach (var playerToClear in PlayerList)
                {
                    playerToClear.ClearInput();
                }

                if(InputManager.NumberOfConnectedGamePads != 0)
                {
                    player.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
                }
                else
                {
                    Player1.InitializeKeyboardControls();
                }
            }
            #endif
        }

        private void ShowNumberOfPlayersForTeam(int teamIndex)
        {
            string playersRemaining = "Players Remaining";

            var playerCount = PlayerList.Count(item => 
                item.TeamIndex == teamIndex && item.IsDying == false);

            if (playerCount == 1) { playersRemaining = "Player Remaining"; }

            GumRuntimes.TextRuntime textToShow = null;
            if(teamIndex == 0)
            {
                textToShow = PlayersRemaingTextTeam1;
            }
            else
            {
                textToShow = PlayersRemaingTextTeam2;
            }

            textToShow.Text = $"{playerCount} {playersRemaining}";
            textToShow.Visible = true;
            this.Call(() => textToShow.Visible = false).After(2);
        }

        private void CheckForEndOfGame()
        {
            bool didTeam0Win = !PlayerList.Any(item => item.TeamIndex == 1);
            bool didTeam1Win = !PlayerList.Any(item => item.TeamIndex == 0);

#if DEBUG
            var keyboard = InputManager.Keyboard;
            bool ctrlDown = keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl);
            if (keyboard.KeyPushed(Keys.D1) && ctrlDown)
            {
                didTeam0Win = true;
            }
            if (keyboard.KeyPushed(Keys.D2) && ctrlDown)
            {
                didTeam1Win = true;
            }
#endif

            if (didTeam1Win)
            {
                GameStats.WinningTeam0Based = 1;

                MoveToScreen(typeof(WrapUpScreen));
            }
            else if(didTeam0Win)
            {
                GameStats.WinningTeam0Based = 0;

                MoveToScreen(typeof(WrapUpScreen));
            }
        }

        #endregion

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
