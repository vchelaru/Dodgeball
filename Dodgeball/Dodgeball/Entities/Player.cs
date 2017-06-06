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
using FlatRedBall.Math;
using System.Linq;
using Dodgeball.AI;
using Microsoft.Xna.Framework;

namespace Dodgeball.Entities
{
	public partial class Player
	{
        #region Fields/Properties

        I2DInput MovementInput { get; set; }
        IPressableInput ActionButton { get; set; }

        I2DInput AimingInput { get; set; }
        IPressableInput TauntButton { get; set; }

        public PositionedObjectList<Player> AllPlayers { get; set; }

        public Ball BallHolding { get; set; }

        //Debug property so AI knows when to resume control of player-controlled Player
        public bool HasInputs => MovementInput != null;

	    #endregion

        #region Initialize
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
		{
            this.ActiveMarkerRuntimeInstance.Visible = false;

		    CircleInstance.Color = TeamIndex == 0 ? Color.Red : Color.Blue;
		}

        public void InitializeXbox360Controls(Xbox360GamePad gamePad)
        {
            this.ActiveMarkerRuntimeInstance.Visible = true;

            var movementLocal = new Multiple2DInputs();
            movementLocal.Inputs.Add(gamePad.LeftStick);
            movementLocal.Inputs.Add(gamePad.DPad);

            MovementInput = movementLocal;
            ActionButton = gamePad.GetButton(Xbox360GamePad.Button.RightShoulder);

            AimingInput = gamePad.RightStick;
            TauntButton = gamePad.GetButton(Xbox360GamePad.Button.LeftShoulder);

        }

        public void InitializeAIControl(AIController aicontrol)
	    {
	        this.ActiveMarkerRuntimeInstance.Visible = false;

	        MovementInput = aicontrol.MovementInput;
	        ActionButton = aicontrol.ActionButton;
	        AimingInput = aicontrol.AimingInput;
	        TauntButton = aicontrol.TauntButton;
        }

        public void ClearInput()
        {
            this.ActiveMarkerRuntimeInstance.Visible = false;

            MovementInput = null;
            ActionButton = null;

            AimingInput = null;
            TauntButton = null;

            Velocity = Vector3.Zero;
        }

        internal void PickUpBall(Ball ballInstance)
        {
            BallHolding = ballInstance;
            BallHolding.ThrowOwner = null;
            ballInstance.OwnerTeam = this.TeamIndex;
        }


        #endregion

        #region Activity

        private void CustomActivity()
		{
            MovementActivity();

            ThrowingActivity();

            HudActivity();

        #if DEBUG
		    if (BallHolding != null && DebuggingVariables.ShowTargetedPlayers) ShowTargetedPlayers();
#endif
		}

#if DEBUG
	    private void ShowTargetedPlayers()
	    {
            foreach (var player in AllPlayers)
            {
                player.CircleInstance.Color = (player.TeamIndex == 0 ? Color.Red : Color.Blue);
            }

	        var targetPlayer = GetTargetedPlayer();
	        targetPlayer.CircleInstance.Color = Color.Yellow;
        }
#endif


        private void HudActivity()
        {
            this.ActiveMarkerRuntimeInstance.X = this.X;
            this.ActiveMarkerRuntimeInstance.Y = this.Y + 55;

            this.HealthBarRuntimeInstance.X = this.X - this.HealthBarRuntimeInstance.Width/2;
            this.HealthBarRuntimeInstance.Y = this.Y + 35;
            this.HealthBarRuntimeInstance.HealthWidth = this.HealthPercentage;

        }

        private void ThrowingActivity()
        {
            if(ActionButton != null && ActionButton.WasJustReleased && BallHolding != null)
            {
                ExecuteThrow();
            }
        }

        internal void GetHitBy(Ball ballInstance)
        {
            this.HealthPercentage -= DamageWhenHitting;
        }

        private void ExecuteThrow()
        {
            var targetPlayer = GetTargetedPlayer();

            var direction = targetPlayer.Position - this.Position;
            direction.Normalize();
            BallHolding.Detach();
            BallHolding.Velocity = direction * ThrowVelocity;

            BallHolding.ThrowOwner = this;
            BallHolding.CurrentOwnershipState = Ball.OwnershipState.Thrown;

            BallHolding = null;

#if DEBUG
            if (DebuggingVariables.PlayerAlwaysControlsBallholder)
            {
                this.ClearInput();
                targetPlayer.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
            }
#endif
        }

	    private Player GetTargetedPlayer()
	    {
	        var opposingTeamPlayers = AllPlayers.Where(p => p.TeamIndex != TeamIndex).ToList();

            //Default to closest player
	        var targetedPlayer = opposingTeamPlayers.Aggregate(
	                (p1, p2) => (p1.Position - Position).Length() < (p2.Position - Position).Length() ? p1 : p2);

            //Choose target if player is aiming
	        if (AimingInput != null && Math.Abs(AimingInput.Magnitude) > 0.05)
	        {
	            var minX = opposingTeamPlayers.Min(p => p.Position.X);
	            var maxX = opposingTeamPlayers.Max(p => p.Position.X);
	            var minY = opposingTeamPlayers.Min(p => p.Position.Y);
	            var maxY = opposingTeamPlayers.Max(p => p.Position.Y);

	            var midX = (minX + maxX) / 2;
	            var midY = (minY + maxY) / 2;

	            var aimPositionX = midX + (AimingInput.X * Math.Abs(maxX - minX));
	            var aimPositionY = midY + (AimingInput.Y * Math.Abs(maxY - minY));

	            var aimPosition = new Vector3(aimPositionX, aimPositionY, 0);

	            var closestTargetDistance = double.MaxValue;
	            foreach (var otherTeamPlayer in opposingTeamPlayers)
	            {
	                //Multiply distance by aiming input to infer importance of the difference in direction
	                var relativeVector = otherTeamPlayer.Position - aimPosition;
	                var targetDistance = (Math.Abs(relativeVector.X) * Math.Abs(AimingInput.X)) +
	                                     (Math.Abs(relativeVector.Y) * Math.Abs(AimingInput.Y));

	                if (targetDistance < closestTargetDistance)
	                {
	                    closestTargetDistance = targetDistance;
	                    targetedPlayer = otherTeamPlayer;
	                }
	            }
            }
	        return targetedPlayer;
	    }

        private void MovementActivity()
        {

            if(MovementInput != null)
            {
                this.Velocity.X = MovementInput.X * MovementSpeed;
                this.Velocity.Y = MovementInput.Y * MovementSpeed;

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
