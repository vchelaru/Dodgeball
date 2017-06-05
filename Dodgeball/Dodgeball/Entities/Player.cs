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
            ballInstance.OwnerTeam = this.TeamIndex;
        }


        #endregion

        #region Activity

        private void CustomActivity()
		{
            MovementActivity();

            ThrowingActivity();

            HudActivity();
		}

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
            float damageToDeal = 20;
            this.HealthPercentage -= damageToDeal;


        }

        private void ExecuteThrow()
        {
            float ThrowSpeed = 400;

            var targetPlayer = AllPlayers.First(player => player.TeamIndex != this.TeamIndex);

            var direction = targetPlayer.Position - this.Position;
            direction.Normalize();
            BallHolding.Detach();
            BallHolding.Velocity = direction * ThrowSpeed;

            BallHolding.ThrowOwner = this;
            BallHolding.CurrentOwnershipState = Ball.OwnershipState.Thrown;

            BallHolding = null;

            this.ClearInput();
            targetPlayer.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);

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
