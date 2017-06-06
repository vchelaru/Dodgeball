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


namespace Dodgeball.Screens
{
	public partial class GameScreen
	{
	    private List<AIController> AIControllers;
        #region Initialize

        void CustomInitialize()
        {
            SharePlayerReferences();
            AssignAIControllers();
            InitializeInput();
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
            Player1.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
        }

        #endregion

        #region Activity

        void CustomActivity(bool firstTimeCalled)
		{
            CollisionActivity();
		    AIActivity();
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

            BallVsPlayerCollision();

            BallVsWallsCollision();

            CheckForEndOfGame();
        }

	    private void PlayerVsPlayerCollision()
	    {
	        // Destroys aren't being called here so we can forward-loop
	        for (var i = 0; i < PlayerList.Count; i++)
	        {
	            var firstPlayer = PlayerList[i];
	            for (var j = i + 1; j < PlayerList.Count; j++)
	            {
	                var secondPlayer = PlayerList[j];

	                firstPlayer.CircleInstance.CollideAgainstBounce(
	                    secondPlayer.CircleInstance, 1, 1, 0.1f);
	            }
	        }
        }

        private void BallVsWallsCollision()
        {
            if(BallInstance.XVelocity < 0 && BallInstance.X < -1920/2.0f + 30)
            {
                BallInstance.XVelocity *= -1;
                BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
            }
            if (BallInstance.XVelocity > 0 && BallInstance.X > 1920 / 2.0f - 30)
            {
                BallInstance.XVelocity *= -1;
                BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
            }

            float top = 200;
            float bottom = -1080 / 2.0f;

            if(BallInstance.YVelocity > 0 && BallInstance.Y > top)
            {
                BallInstance.YVelocity *= -1;
                BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
            }
            if (  BallInstance.YVelocity < 0 && BallInstance.Y < bottom + 30)
            {
                BallInstance.YVelocity *= -1;
                BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
            }
        }

        private void BallVsPlayerCollision()
        {
            if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Free)
            {
                foreach (var player in PlayerList)
                {
                    if (player.CollideAgainst(BallInstance))
                    {
                        PerformPickupLogic(player);

                        break;
                    }
                }
            }
            else if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Thrown)
            {
                // reverse loop since players can be removed:
                for(int i = PlayerList.Count - 1; i > -1; i--)
                {
                    var player = PlayerList[i];

                    if (BallInstance.ThrowOwner != player && player.CollideAgainst(BallInstance))
                    {
                        PerformGetHitLogic(player);
                    }
                }
            }
            // don't perform collision if the ball is being held
        }

        private void PerformGetHitLogic(Entities.Player player)
        {
            if (BallInstance.OwnerTeam != player.TeamIndex)
            {
                // OUCH
                player.GetHitBy(BallInstance);

            }
            else
            {
                // bounce! No damage though
            }



            // make the ball bounce off the player:
            BallInstance.CollideAgainstBounce(player, 0, 1, 1);
            BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Free;

            // do this after collision:
            // We may want to have animations, flashing, etc, but for now destroy the guy!
            if(player.HealthPercentage <= 0)
            {
                player.Destroy();
            }
        }

        private void PerformPickupLogic(Entities.Player player)
        {
            BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Held;
            BallInstance.Velocity = Vector3.Zero;
            BallInstance.AttachTo(player, false);

            #if DEBUG
            if (DebuggingVariables.PlayerAlwaysControlsBallholder)
            {
                foreach (var playerToClear in PlayerList)
                {
                    playerToClear.ClearInput();
                }

                player.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
            }
            #endif

            player.PickUpBall(BallInstance);
        }

        private void CheckForEndOfGame()
        {
            if(!PlayerList.Any(item => item.TeamIndex == 0))
            {
                // Team 1 wins
                MoveToScreen(typeof(WrapUpScreen));
            }
            else if(!PlayerList.Any(item => item.TeamIndex == 1))
            {
                // Team 0 wins
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
