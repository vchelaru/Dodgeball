using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Dodgeball.Entities
{
	public partial class Ball
	{
        #region Enums

        public enum OwnershipState
        {
            Free,
            Thrown,
            Held
        }

        #endregion

        #region Fields / Properties

        public float Altitude { get; set; }
        public float AltitudeVelocity { get; set; } = 0;

        public int OwnerTeam { get; set; }

        public Player ThrowOwner { get; set; }

        public OwnershipState CurrentOwnershipState { get; set; }

	    private SoundEffectInstance ballFloorBounceSound;
	    private SoundEffectInstance ballWallBounceSound;
	    private SoundEffectInstance ballThrowSound;

        #endregion


        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{
            Altitude = this.HeightWhenThrown;
		    ballFloorBounceSound = GlobalContent.ball_bounce.CreateInstance();
		    ballWallBounceSound = GlobalContent.ball_wall_bounce.CreateInstance();
		    ballThrowSound = GlobalContent.ball_throw.CreateInstance();
#if DEBUG
            if (DebuggingVariables.ShowBallTrajectory)
		    {
		        TrajectoryPolygon.Visible = true;
		        TrajectoryPolygon.Color = Color.Red;
		    }
		    else
		    {
#endif
		        TrajectoryPolygon.Visible = false;
#if DEBUG
		    }
#endif
        }

        private void CustomActivity()
		{
            PerformFallingAndBouncingActivity();
		    UpdateTrajectoryRotation();
		}

	    private void UpdateTrajectoryRotation()
	    {
	        TrajectoryPolygon.RelativeRotationZ = (float) Math.Atan2(Velocity.Y, Velocity.X);
#if DEBUG
	        if (DebuggingVariables.ShowBallTrajectory)
	        {
	            TrajectoryPolygon.Visible = Velocity.X != 0 || Velocity.Y != 0;
	        }
#endif
	    }

	    public void PerformThrownLogic(Player player, Vector3 velocity)
	    {
	        Velocity = velocity;
	        ThrowOwner = player;
	        CurrentOwnershipState = Ball.OwnershipState.Thrown;

            var ballThrowPan = MathHelper.Clamp(X / 540f, -1, 1);
	        var ballThrowVol = MathHelper.Clamp(Velocity.Length() / 1000f, 0, 1);
            ballThrowSound.Pan = ballThrowPan;
	        ballThrowSound.Volume = ballThrowVol;
            ballThrowSound.Play();
        }

        private void PerformFallingAndBouncingActivity()
        {
            bool shouldFallAndBounce = this.CurrentOwnershipState != OwnershipState.Held;

            if(shouldFallAndBounce)
            {
                // linear approx. is fine:
                AltitudeVelocity += TimeManager.SecondDifference * -BallGravity;
                Altitude += TimeManager.SecondDifference * AltitudeVelocity;
            }
            else
            {
                Altitude = HeightWhenThrown;
                AltitudeVelocity = 0;
            }

            this.SpriteInstance.RelativeY = Altitude + SpriteInstance.Height / 2.0f;

            if(SpriteInstance.RelativeBottom < 0 && AltitudeVelocity < 0)
            {
                // move it above the ground 
                SpriteInstance.RelativeBottom = 0;
                
                // make it bounce, but lose some height
                AltitudeVelocity *= -BounceCoefficient;

                //Only make bounce sound if it hit hard enough
                if (AltitudeVelocity > 50)
                {
                    var ballPitch = MathHelper.Clamp(AltitudeVelocity / 500f, 0, 1);
                    var ballPan = MathHelper.Clamp(Position.X / (FlatRedBall.Camera.Main.OrthogonalWidth / 2),-1,1);
                    ballFloorBounceSound.Pitch = ballPitch;
                    ballFloorBounceSound.Volume = ballPitch;
                    ballFloorBounceSound.Pan = ballPan;
                    ballFloorBounceSound.Play();
                    
                }

                if (CurrentOwnershipState == OwnershipState.Thrown)
                {
                    CurrentOwnershipState = OwnershipState.Free;
                }
            }
        }


	    public void BounceOffWall(bool isLeftOrRightWall)
	    {
	        var bounceVolume = 0f;
	        if (isLeftOrRightWall)
	        {
	            bounceVolume = MathHelper.Clamp(Math.Abs(XVelocity) / 1000f,0,1);
                XVelocity *= -1;
	        }
	        else
	        {
	            bounceVolume = MathHelper.Clamp(Math.Abs(YVelocity) / 1000f, 0, 1);
                YVelocity *= -1;
            }
	        var bouncePan = MathHelper.Clamp(X / 540f,-1,1);
	        ballWallBounceSound.Pan = bouncePan;
	        ballWallBounceSound.Pitch = bounceVolume/4;
	        ballWallBounceSound.Volume = bounceVolume;
            ballWallBounceSound.Play();
            CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
        }

        private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
