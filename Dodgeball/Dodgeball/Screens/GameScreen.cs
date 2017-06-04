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



namespace Dodgeball.Screens
{
	public partial class GameScreen
	{
        #region Initialize

        void CustomInitialize()
        {
            InitializeInput();

            SharePlayerReferences();

        }

        private void SharePlayerReferences()
        {
            foreach(var player in this.PlayerList)
            {
                player.AllPlayers = PlayerList;
            }
        }

        private void InitializeInput()
        {
            Player1.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
        }

        #endregion

        void CustomActivity(bool firstTimeCalled)
		{
            CollisionActivity();

		}

        private void CollisionActivity()
        {
            BallVsPlayerCollision();

            BallVsWallsCollision();
        }

        private void BallVsWallsCollision()
        {

            if(BallInstance.XVelocity < 0 && BallInstance.X < -1920/2.0f)
            {
                BallInstance.XVelocity *= -1;
            }
            if (BallInstance.XVelocity > 0 && BallInstance.X > 1920 / 2.0f)
            {
                BallInstance.XVelocity *= -1;
            }

            float top = 400;
            float bottom = -1080 / 2.0f;

            if(BallInstance.YVelocity > 0 && BallInstance.Y > top)
            {
                BallInstance.YVelocity *= -1;
            }
            if(  BallInstance.YVelocity < 0 && BallInstance.Y < bottom)
            {
                BallInstance.YVelocity *= -1;
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
                        BallInstance.CurrentOwnershipState = Entities.Ball.OwnershipState.Held;
                        BallInstance.AttachTo(player, false);

                        player.PickUpBall(BallInstance);

                        break;
                    }
                }
            }
            else if (BallInstance.CurrentOwnershipState == Entities.Ball.OwnershipState.Thrown)
            {
                foreach (var player in PlayerList)
                {
                    if (BallInstance.ThrowOwner != player && player.CollideAgainst(BallInstance))
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
                    }
                }
            }
            // don't perform collision if the ball is being held
        }

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
