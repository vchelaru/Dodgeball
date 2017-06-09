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
using Dodgeball.GumRuntimes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using RenderingLibrary;

namespace Dodgeball.Entities
{
	public partial class Player
	{
        #region Fields/Properties

	    private SoundEffectInstance playerDodgeSound;

        I2DInput MovementInput { get; set; }
        IPressableInput ActionButton { get; set; }

        I2DInput AimingInput { get; set; }
        IPressableInput TauntButton { get; set; }

        public PositionedObjectList<Player> AllPlayers { get; set; }

        public Ball BallHolding { get; set; }
	    private bool justReleasedBall = false;
	    public bool IsHoldingBall => BallHolding != null;

        public bool IsDodging { get; private set; }

	    public WorldComponentRuntime WorldComponent;
	    public IPositionedSizedObject TeamRectangle => (TeamIndex == 0
	        ? WorldComponent.LeftTeamRectangle
	        : WorldComponent.RightTeamRectangle);
        
	    public float TeamRectangleRight => (TeamRectangle.X + TeamRectangle.Width) - FlatRedBall.Camera.Main.OrthogonalWidth / 2;
	    public float TeamRectangleLeft => TeamRectangle.X  - FlatRedBall.Camera.Main.OrthogonalWidth / 2;
	    public float TeamRectangleTop => TeamRectangle.Y;
        public float TeamRectangleBottom => TeamRectangle.Y - TeamRectangle.Height;

        //Properties to determine player location in relation to team rectangle
	    public bool IsInBack => TeamIndex == 0
	        ? Position.X <= TeamRectangleLeft + (0.2f * TeamRectangle.Width)
	        : Position.X >= TeamRectangleLeft + (0.8f * TeamRectangle.Width);
	    public bool IsInFront => TeamIndex == 0
	        ? Position.X >= TeamRectangleLeft + (0.8f * TeamRectangle.Width)
	        : Position.X <= TeamRectangleLeft + (0.2f * TeamRectangle.Width);

	    public bool IsOnTop => Position.Y >= TeamRectangleBottom + (0.8f * TeamRectangle.Height);
	    public bool IsOnBottom => Position.Y <= TeamRectangleBottom + (0.2f * TeamRectangle.Height);

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
            playerDodgeSound = GlobalContent.player_dodge.CreateInstance();

            this.ActiveMarkerRuntimeInstance.Visible = false;
            this.EnergyBarRuntimeInstance.Visible = false;

		    CircleInstance.Color = TeamIndex == 0 ? Color.Red : Color.Blue;
		}

        public void InitializeXbox360Controls(Xbox360GamePad gamePad)
        {
            this.ActiveMarkerRuntimeInstance.Visible = true;
            this.EnergyBarRuntimeInstance.Visible = true;

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
            this.EnergyBarRuntimeInstance.Visible = false;

            MovementInput = aicontrol.MovementInput;
	        ActionButton = aicontrol.ActionButton;
	        AimingInput = aicontrol.AimingInput;
	        TauntButton = aicontrol.TauntButton;
        }

        public void ClearInput()
        {
            this.ActiveMarkerRuntimeInstance.Visible = false;
            this.EnergyBarRuntimeInstance.Visible = false;

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

            ThrowOrDodgeActivity();

            HudActivity();

        #if DEBUG
		    if (IsHoldingBall && DebuggingVariables.ShowTargetedPlayers) ShowTargetedPlayers();
#endif

		    SetAnimation();
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
            this.ActiveMarkerRuntimeInstance.Y = this.Y + 200;

            this.HealthBarRuntimeInstance.X = this.X - this.HealthBarRuntimeInstance.Width/2;
            this.HealthBarRuntimeInstance.Y = this.Y + 160;
            this.HealthBarRuntimeInstance.HealthWidth = this.HealthPercentage;


            this.EnergyBarRuntimeInstance.X = this.X + (this.TeamIndex == 0 ? -120 : 120); 
            this.EnergyBarRuntimeInstance.Y = this.Y;
            this.EnergyBarRuntimeInstance.EnergyHeight = this.EnergyPercentage;

            ThrowChargeMeterRuntimeInstance.X = X;
            ThrowChargeMeterRuntimeInstance.Y = Y+200;
        }

        private void ThrowOrDodgeActivity()
        {
            if(ActionButton != null)
            {
                if (ActionButton.WasJustPressed && !IsDodging && !IsHoldingBall)
                {
                    IsDodging = true;
                    var dodgeSoundPan = MathHelper.Clamp(X / 540f, -1, 1);
                    playerDodgeSound.Pan = dodgeSoundPan;
                    playerDodgeSound.Play();
                }

                if (ActionButton.WasJustPressed) ThrowChargeMeterRuntimeInstance.Reset();
                ThrowChargeMeterRuntimeInstance.Visible = ActionButton.IsDown &&  IsHoldingBall;

                if (IsHoldingBall && ActionButton.IsDown) ThrowChargeMeterRuntimeInstance.ChargeActivity();

                if (ActionButton.WasJustReleased && IsHoldingBall) ExecuteThrow();
            }
        }

        internal void GetHitBy(Ball ballInstance)
        {
            this.HealthPercentage -= DamageWhenHitting;
        }

        private void ExecuteThrow()
        {
            if (ThrowChargeMeterRuntimeInstance.FailedThrow)
            {
                //TODO:  They failed!  Now what?  Using half of minimum velocity for now
                ThrowVelocity = MinThrowVelocity / 2;
            }
            else
            {
                ThrowVelocity = MinThrowVelocity + ((MaxThrowVelocity - MinThrowVelocity) *
                                                    ThrowChargeMeterRuntimeInstance.EffectiveChargePercent);
            }

            var targetPlayer = GetTargetedPlayer();

            var direction = targetPlayer.Position - this.Position;

            var distanceToTarget = direction.Length();

            var timeToTarget = .5f * distanceToTarget / ThrowVelocity;

            // arc that badboy:
            float desiredYVelocity = BallHolding.BallGravity * timeToTarget;

            BallHolding.AltitudeVelocity = desiredYVelocity;

            direction.Normalize();
            BallHolding.Detach();
            BallHolding.PerformThrownLogic(this, direction * ThrowVelocity);

            BallHolding = null;
            justReleasedBall = true;
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
            if (MovementInput != null &&
                SpriteInstance.CurrentChainName != "Throw" && SpriteInstance.CurrentChainName != "Aim")
            {
                this.Velocity.X = MovementInput.X * MovementSpeed;
                this.Velocity.Y = MovementInput.Y * MovementSpeed;
            }
            else
            {
                this.Velocity.X = 0;
                this.Velocity.Y = 0;
            }

            //Keep player from moving over lines
            if (Position.X > TeamRectangleRight)
            {
                Position.X = TeamRectangleRight;
            }
            else if (Position.X < TeamRectangleLeft)
            {
                Position.X = TeamRectangleLeft;
            }

            if (Position.Y > TeamRectangleTop)
            {
                Position.Y = TeamRectangleTop;
            }
            else if (Position.Y < TeamRectangleBottom)
            {
                Position.Y = TeamRectangleBottom;
            }
        }

	    private void SetAnimation()
	    {
	        if (ActionButton != null)
	        {
	            if (justReleasedBall)
	            {
	                SpriteInstance.SetAnimationChain("Throw");
	                justReleasedBall = false;
	            }
                else if (ActionButton.IsDown && IsHoldingBall)
	            {
	                SpriteInstance.SetAnimationChain("Aim");
	            }
                else if (IsDodging)
	            {
	                if (SpriteInstance.CurrentChainName == "Dodge" && SpriteInstance.JustCycled)
	                {
	                    IsDodging = false;
	                }
	                else if (SpriteInstance.CurrentChainName != "Dodge")
	                {
	                    SpriteInstance.SetAnimationChain("Dodge");
                    }
	            }

	            if (TeamIndex == 0)
	            {
	                SpriteInstance.FlipHorizontal = true;
	            }
	            else
	            {
	                SpriteInstance.FlipHorizontal = false;
	            }
            }

	        if ((SpriteInstance.CurrentChainName != "Throw" && SpriteInstance.CurrentChainName != "Aim" && SpriteInstance.CurrentChainName != "Dodge") ||
	            SpriteInstance.JustCycled)
	        {

	            if (MovementInput?.X != 0 || MovementInput?.Y != 0)
	            {
	                if (IsHoldingBall)
	                {
	                    SpriteInstance.SetAnimationChain(("RunHold"));
	                }
	                else
	                {
	                    SpriteInstance.SetAnimationChain("Run");
                    }
	                
	                if (Velocity.X < 0)
	                {
	                    SpriteInstance.FlipHorizontal = false;
	                }
	                else
	                {
	                    SpriteInstance.FlipHorizontal = true;
	                }
	            }
	            else
	            {
	                SpriteInstance.SetAnimationChain("Idle");
	                if (TeamIndex == 0)
	                {
	                    SpriteInstance.FlipHorizontal = true;
	                }
	                else
	                {
	                    SpriteInstance.FlipHorizontal = false;
	                }
	            }
	        }

	        SpriteInstance.Animate = SpriteInstance.CurrentChainName != "Aim";
            this.SpriteInstance.RelativeY = this.SpriteInstance.Height / 2.0f;
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
