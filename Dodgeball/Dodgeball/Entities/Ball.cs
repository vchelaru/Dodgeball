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
        public bool WasSuperThrown { get; set; }

        OwnershipState currentOwnershipState;
        public OwnershipState CurrentOwnershipState
        {
            get { return currentOwnershipState; }
            set
            {
                currentOwnershipState = value;

                if(currentOwnershipState == OwnershipState.Free)
                {
                    this.Drag = DragInFreeState;
                }
                else
                {
                    this.Drag = 0;
                }
            }
        }

	    private SoundEffectInstance ballFloorBounceSound;
	    private SoundEffectInstance ballWallBounceSound;
	    private SoundEffectInstance ballThrowSound;

        #endregion

        #region Initialize Methods
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {
            ShadowSprite.RelativeZ = -1;
            Altitude = this.HeightWhenThrown;
		    ballFloorBounceSound = GlobalContent.ball_bounce_0.CreateInstance();
		    ballWallBounceSound = GlobalContent.ball_wall_bounce_0.CreateInstance();
		    ballThrowSound = GlobalContent.ball_throw_0.CreateInstance();
#if DEBUG
            if (DebuggingVariables.ShowDebugShapes)
		    {
		        TrajectoryPolygon.Visible = true;
		        CircleInstance.Visible = true;
		        TrajectoryPolygon.Color = Color.Red;
		    }
		    else
		    {
#endif
		        CircleInstance.Visible = false;
                TrajectoryPolygon.Visible = false;
#if DEBUG
		    }
#endif
        }

        #endregion

        private void CustomActivity()
		{
            PerformFallingAndBouncingActivity();
		    UpdateTrajectoryRotation();
		    SetAnimation();
		    UpdateShadow();
		}

	    private void UpdateShadow()
	    {
            ShadowSprite.Visible = CurrentOwnershipState != OwnershipState.Held;
	        if (ShadowSprite.Visible)
	        {
	            ShadowSprite.RelativeY = CircleInstance.Radius / 2;
	            ShadowSprite.TextureScale = 1.25f - (Altitude / 300);
	        }
	    }

	    private void SetAnimation()
	    {
	        if (CurrentOwnershipState == OwnershipState.Thrown && Velocity.X >= 2000)
	        {
	            SpriteInstance.CurrentChainName = "FastBall";
	        }
	        else
	        {
	            SpriteInstance.CurrentChainName = "SlowBall";
            }

	        SpriteInstance.Visible = CurrentOwnershipState != OwnershipState.Held;
	    }

	    private void UpdateTrajectoryRotation()
	    {
	        TrajectoryPolygon.RelativeRotationZ = (float) Math.Atan2(Velocity.Y, Velocity.X);
#if DEBUG
	        if (DebuggingVariables.ShowDebugShapes)
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

	        SetBallThrowSoundByVelocity();

            var ballThrowPan = MathHelper.Clamp(X / 540f, -1, 1);
	        var ballThrowVol = MathHelper.Clamp(Velocity.Length() / (GameVariables.MaxThrowVelocity/2), 0, 1);
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
                    SetBallBounceSoundByVelocity();

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

	        SetBallWallBounceSoundByVelocity();

            var bouncePan = MathHelper.Clamp(X / 540f,-1,1);
	        ballWallBounceSound.Pan = bouncePan;
	        ballWallBounceSound.Pitch = bounceVolume/4;
	        ballWallBounceSound.Volume = bounceVolume;
            ballWallBounceSound.Play();
            CurrentOwnershipState = Entities.Ball.OwnershipState.Free;
        }

        #region Sound Determination
	    private void SetBallThrowSoundByVelocity()
	    {
	        string ballThrowSoundName;

            var percentRequiredForSpecial = 0.9f;
	        var velocityLength = Velocity.Length();
	        var isFailedThrow = velocityLength <= GameVariables.MinThrowVelocity;
	        var isSpecialThrow = velocityLength > GameVariables.MaxThrowVelocity * percentRequiredForSpecial;
	        var maxVelocity = GameVariables.MaxThrowVelocity;

	        if (isSpecialThrow)
	        {
	            //This normalized velocity to be in the range of minimum for special to max for special
	            var effectiveVelocity = velocityLength - (GameVariables.MaxThrowVelocity * percentRequiredForSpecial);
	            var effectiveMax = GameVariables.MaxThrowVelocity * (1 - percentRequiredForSpecial);

	            velocityLength = effectiveVelocity;
	            maxVelocity = effectiveMax;
	        }

	        if (isFailedThrow)
	        {
	            ballThrowSoundName = "ball_throw_0";
	        }
	        else
	        {
	            //This is the highest numbered sound effect available in GlobalContent: 5 special, 6 regular
	            var maxThrowIndex = isSpecialThrow ? 5 : 6;

                var pctOfPossibleVelocity = velocityLength / maxVelocity;

	            var throwIndex = Convert.ToInt32(pctOfPossibleVelocity * maxThrowIndex);

	            ballThrowSoundName = isSpecialThrow ? $"ball_throw_special_{throwIndex}" : $"ball_throw_{throwIndex}";
	        }

	        var throwSound = GlobalContent.GetFile(ballThrowSoundName) as SoundEffect;

	        if (throwSound != null)
	        {
	            ballThrowSound = throwSound.CreateInstance();
	        }
	    }

	    private void SetBallBounceSoundByVelocity()
	    {
	        string ballFloorBounceSoundName;

	        var velocityLength = Velocity.Length();
	        var isSlowMoving = velocityLength <= GameVariables.MinThrowVelocity;

	        if (isSlowMoving)
	        {
	            ballFloorBounceSoundName = "ball_bounce_0";
	        }
	        else
	        {
	            //This is the highest numbered sound effect available in GlobalContent:
                var maxBounceIndex = 6;

                var effectiveVelocity = velocityLength - GameVariables.MinThrowVelocity;
	            var effectiveMax = GameVariables.MaxThrowVelocity - GameVariables.MinThrowVelocity;

                var pctOfPossibleVelocity = effectiveVelocity / effectiveMax;

	            var bounceIndex = Convert.ToInt32(pctOfPossibleVelocity * maxBounceIndex);

	            ballFloorBounceSoundName =  $"ball_bounce_{bounceIndex}";
            }

	        var floorBounceSound = GlobalContent.GetFile(ballFloorBounceSoundName) as SoundEffect;

	        if (floorBounceSound != null)
	        {
	            ballFloorBounceSound = floorBounceSound.CreateInstance();
	        }
        }



	    private void SetBallWallBounceSoundByVelocity()
	    {
	        string ballWallBounceSoundName;

	        var velocityLength = Velocity.Length();
	        var isSlowMoving = velocityLength <= GameVariables.MinThrowVelocity;

	        if (isSlowMoving)
	        {
	            ballWallBounceSoundName = "ball_wall_bounce_0";
	        }
	        else
	        {
	            //This is the highest numbered sound effect available in GlobalContent:
                var maxBounceIndex = 7;

                var effectiveVelocity = velocityLength - GameVariables.MinThrowVelocity;
	            var effectiveMax = GameVariables.MaxThrowVelocity - GameVariables.MinThrowVelocity;

	            var pctOfPossibleVelocity = effectiveVelocity / effectiveMax;
	            
	            var bounceIndex = Convert.ToInt32(pctOfPossibleVelocity * maxBounceIndex);

	            ballWallBounceSoundName = $"ball_wall_bounce_{bounceIndex}";
	        }

	        var wallBounceSound = GlobalContent.GetFile(ballWallBounceSoundName) as SoundEffect;

	        if (wallBounceSound != null)
	        {
	            ballWallBounceSound = wallBounceSound.CreateInstance();
	        }
        }

        #endregion

        private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
