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

        #endregion


        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{
            Altitude = this.HeightWhenThrown;

		}

		private void CustomActivity()
		{
            PerformFallingAndBouncingActivity();
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
                GlobalContent.ball_bounce.Play();
                // make it bounce, but lose some height
                AltitudeVelocity *= -BounceCoefficient;

                if(CurrentOwnershipState == OwnershipState.Thrown)
                {
                    CurrentOwnershipState = OwnershipState.Free;
                }
            }
        }

        private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
