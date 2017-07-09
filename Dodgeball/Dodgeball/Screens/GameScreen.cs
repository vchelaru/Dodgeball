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
	    private SoundEffectInstance playerCatchSound;

	    private float PlayAreaTop => -WorldComponentInstance.PlayArea.Y + (FlatRedBall.Camera.Main.OrthogonalHeight/2);
	    private float PlayAreaBottom => PlayAreaTop - WorldComponentInstance.PlayArea.Height;
        private float PlayAreaLeft = -1920 / 2.0f;
        private float PlayAreaRight = 1920 / 2.0f;

        #endregion

        #region Initialize

        void CustomInitialize()
        {
            InitializeSounds();

            ShareReferences();

            InitializePlayerEvents();

            PositionPlayers();

            AssignAIControllers();

            InitializeInput();

            InitializeUi();
        }

        private void InitializeUi()
        {
            // moves this above the health bars:
            this.WrapUpComponentInstance.Z = 3;
        }

        private void InitializeSounds()
        {
            playerHitSound = GlobalContent.player_hit_0.CreateInstance();
            playerCatchSound = GlobalContent.player_catch.CreateInstance();
        }

        private void PositionPlayers()
        {
            var team1 = PlayerList.Where(item => item.TeamIndex == 0).ToArray();
            var team2 = PlayerList.Where(item => item.TeamIndex == 1).ToArray();

            var top = PlayAreaTop - PlayerList[0].CircleInstance.Radius;
            var bottom = PlayAreaBottom + PlayerList[0].CircleInstance.Radius;
            var distanceBetween = (top - bottom) / 3.0f;
            for (int i = 0; i < team1.Length; i++)
            {
                team1[i].X = PlayAreaLeft;
                team1[i].Y = top - distanceBetween * i;
            }

            for (int i = 0; i < team2.Length; i++)
            {
                team2[i].X = PlayAreaRight;
                team2[i].Y = top - distanceBetween * i;
            }
        }

        private void AssignAIControllers()
	    {

            foreach (var player in this.PlayerList)
	        {
                player.InitializeAIControl();
	        }
        }

	    private void ShareReferences()
        {
            foreach(var player in this.PlayerList)
            {
                player.AllPlayers = PlayerList;
                player.WorldComponent = WorldComponentInstance;
                player.Ball = BallInstance;

                player.MoveUiTo(UILayer, UILayerGum);
            }
        }

        private void InitializeInput()
        {
            if(InputManager.NumberOfConnectedGamePads != 0)
            {
                bool hasAnyAssigned = GlobalData.JoinStatuses.Any(item => item != JoinStatus.Undecided);

                if(hasAnyAssigned)
                {
                    for(int i = 0; i < 4; i++)
                    {
                        var gamepad = InputManager.Xbox360GamePads[i];
                        var joinStatus = GlobalData.JoinStatuses[i];
                        if (joinStatus != JoinStatus.Undecided && gamepad.IsConnected)
                        {
                            int teamToJoin;
                            if(joinStatus == JoinStatus.Team1)
                            {
                                teamToJoin = 0;
                            }
                            else
                            {
                                teamToJoin = 1;
                            }

                            var playerToAssign = PlayerList.First(item => item.TeamIndex == teamToJoin && item.IsAiControlled);
                            playerToAssign.InitializeXbox360Controls(gamepad);
                        }
                    }
                }
                else
                {
                    // didn't go through the screen to assign characters, so let's default to one control
                    PlayerList[0].InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
                }
            }
            else
            {
                // no controllers connected, so just use a keyboard:
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
            SuperThrowZoomIfNecessary();

            SuperHitZoomIfNecessary();

            CollisionActivity();

            EndGameActivity();

		    PlayMusicActivity();

#if DEBUG
            DebugActivity();
#endif
        }

	    private void SuperHitZoomIfNecessary()
	    {
	        var superHitPlayer = PlayerList.FirstOrDefault(p => p.IsHitBySuperThrow);
	        var shouldSuperHitZoom = superHitPlayer != null;

	        if (shouldSuperHitZoom)
	        {
	            superHitPlayer.IsHitBySuperThrow = false;

	            var superHitAnimation = superHitPlayer.TeamIndex == 0
	                ? SuperThrowHitInstance.Team0SuperHitAnimation
	                : SuperThrowHitInstance.Team1SuperHitAnimation;

	            PauseThisScreen();
	            GlobalContent.superThrow.Play();
                SuperThrowHitInstance.SetColors(shirtColor: superHitPlayer.ShirtColor, shortsColor: superHitPlayer.ShortsColor);
	            SuperThrowHitInstance.Visible = true;
	            superHitAnimation.Play();
	            this.Call(() =>
	            {
	                SuperThrowHitInstance.Visible = false;
	                UnpauseThisScreen();
	            }).After(superHitAnimation.Length);
	        }
        }

	    private void SuperThrowZoomIfNecessary()
	    {
	        var superThrowPlayer = PlayerList.FirstOrDefault(p => p.IsPerformingSuperThrow);
	        var shouldSuperThrowZoom = superThrowPlayer != null;

	        if (shouldSuperThrowZoom)
	        {
	            superThrowPlayer.IsPerformingSuperThrow = false;

	            var superThrowAnimation = superThrowPlayer.TeamIndex == 0
	                ? SuperThrowZoomInstance.Team0SuperThrowAnimation
	                : SuperThrowZoomInstance.Team1SuperThrowAnimation;

                PauseThisScreen();
	            GlobalContent.superHit.Play();
	            SuperThrowZoomInstance.SetColors(shirtColor: superThrowPlayer.ShirtColor, shortsColor:superThrowPlayer.ShortsColor);
	            SuperThrowZoomInstance.Visible = true;
	            superThrowAnimation.Play();
	            this.Call(() =>
	            {
	                SuperThrowZoomInstance.Visible = false;
                    UnpauseThisScreen();
	            }).After(superThrowAnimation.Length);
	        }
	    }

	    private void PlayMusicActivity()
	    {
            bool shouldLoop = FlatRedBall.Audio.AudioManager.CurrentlyPlayingSong == null && 
                // If the wrap component is shown, don't play again, we want it to stay quiet.
                !this.WrapUpComponentInstance.Visible;

            if (shouldLoop)
	        {
	            FlatRedBall.Audio.AudioManager.PlaySong(GlobalContent.dodgeball_bgm, true, true);
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
            if (BallInstance.XVelocity < 0 && BallInstance.X < PlayAreaLeft + BallInstance.CircleInstance.Radius ||
                BallInstance.XVelocity > 0 && BallInstance.X > PlayAreaRight - BallInstance.CircleInstance.Radius)
            { 
                BallInstance.BounceOffWall(isLeftOrRightWall: true);
            }

            if((BallInstance.YVelocity > 0 && (BallInstance.Y - (BallInstance.CircleInstance.Radius / 2) > PlayAreaTop)) ||
                (BallInstance.YVelocity < 0 && BallInstance.Y - BallInstance.CircleInstance.Radius < PlayAreaBottom))
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
                    PickUpFreeBallActivity();
                }
                else if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Thrown)
                {
                    CatchAndGetHitByBallCollisionActivity();
                }
                // don't perform collision if the ball is being held
            }
        }

        private void CatchAndGetHitByBallCollisionActivity()
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

        private void PickUpFreeBallActivity()
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

        private void PerformCatchBallLogic(Player playerCatchingBall)
	    {
	        var playerCatchPan = MathHelper.Clamp(playerCatchingBall.X / 540f, -1, 1);

            playerCatchSound.Pan = playerCatchPan;
	        playerCatchSound.Play();

            playerCatchingBall.CatchBall(BallInstance);

            TrySwitchingControlToPlayerGettingBall(playerCatchingBall);
        }

        private void PerformGetHitLogic(Entities.Player player)
        {
            //Let player react and determine if they'll take damage
            player.GetHitBy(BallInstance);

            if (!player.IsAiControlled && player.IsDying) TrySwitchingControlToAliveAIPlayer(player);

            //Make a hit sound
            var ballVelocity = BallInstance.Velocity.Length();
            var maxVelocity = GameVariables.MaxThrowVelocity;
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

	    private void TrySwitchingControlToAliveAIPlayer(Player playerDying)
	    {
	        var eligibleAIControlledTeammate =
	            PlayerList
	                .FirstOrDefault(item => item.IsAiControlled == true &&
	                                item.IsDying == false &&
	                                item.TeamIndex == playerDying.TeamIndex);

	        if (eligibleAIControlledTeammate != null)
	        {
	            playerDying.SwitchInputTo(eligibleAIControlledTeammate);
	        }
        }

	    private void SetPlayerHitSoundByVelocity(float ballVelocity, float maxVelocity)
	    {
	        //This is the highest numbered sound effect available in GlobalContent: 8 player_hit sounds
            var maxHitIndex = 8;

            var pctOfPossibleVelocity = ballVelocity / maxVelocity;
	        var hitIndex = Convert.ToInt32(pctOfPossibleVelocity * maxHitIndex);

	        var playerHitSoundName = $"player_hit_{hitIndex}";

	        var hitSound = GlobalContent.GetFile(playerHitSoundName) as SoundEffect;
            
	        if (hitSound != null)
	        {
	            playerHitSound = hitSound.CreateInstance();
	        }
	    }

	    private void PerformPickupLogic(Entities.Player playerPickingUpBall)
        {
            playerPickingUpBall.PickUpBall();

            TrySwitchingControlToPlayerGettingBall(playerPickingUpBall);

        }

        private void TrySwitchingControlToPlayerGettingBall(Player playerGettingBall)
        {
            if (playerGettingBall.IsAiControlled)
            {
                var eligiblePlayerControlledTeammates =
                    PlayerList
                    .Where(item => item.IsAiControlled == false &&
                        item.IsDying == false &&
                        item.TeamIndex == playerGettingBall.TeamIndex);

                var playerToSwitchInputFrom = eligiblePlayerControlledTeammates
                    .OrderBy(item => (item.Position - playerGettingBall.Position).LengthSquared())
                    .FirstOrDefault();

                if (playerToSwitchInputFrom != null)
                {
                    playerToSwitchInputFrom.SwitchInputTo(playerGettingBall);
                }
            }
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

        private void EndGameActivity()
        {
            if(WrapUpComponentInstance.Visible == false)
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

                if (didTeam0Win || didTeam1Win)
                {
                    // todo: stop the normal song...
                    this.Call(() =>
                    {
                        FlatRedBall.Audio.AudioManager.StopSong();

                        FlatRedBall.Audio.AudioManager.PlaySong(GlobalContent.dodgeball_end_sting, true, true);
                    }).After(.25);

                    WrapUpComponentInstance.Visible = true;

                    foreach (var player in PlayerList)
                    {
                        player.ClearInput();
                    }
                }
                if (didTeam1Win)
                {
                    WrapUpComponentInstance.CurrentTeamNumberState = GumRuntimes.WrapUpComponentRuntime.TeamNumber.Team01;
                }
                else if(didTeam0Win)
                {
                    WrapUpComponentInstance.CurrentTeamNumberState = GumRuntimes.WrapUpComponentRuntime.TeamNumber.Team02;

                }
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
