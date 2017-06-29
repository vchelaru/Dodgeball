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
using Dodgeball.Components;
using Microsoft.Xna.Framework.Input;

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

	    public bool IsAttemptingCatch { get; private set; }
        private double CurrentCatchAttemptTime;
	    public bool CatchIsEffective => CurrentCatchAttemptTime <= CatchEffectivenessDuration;
        public bool IsPerformingSuccessfulCatch { get; private set; }
	    private bool IsHardCatch;

	    public WorldComponentRuntime WorldComponent;
	    public IPositionedSizedObject TeamRectangle => (TeamIndex == 0
	        ? WorldComponent.LeftTeamRectangle
	        : WorldComponent.RightTeamRectangle);
        
	    public float TeamRectangleRight => (TeamRectangle.X + TeamRectangle.Width) - FlatRedBall.Camera.Main.OrthogonalWidth / 2;
	    public float TeamRectangleLeft => TeamRectangle.X  - FlatRedBall.Camera.Main.OrthogonalWidth / 2;
	    public float TeamRectangleTop => TeamRectangle.Y;
        public float TeamRectangleBottom => TeamRectangle.Y - TeamRectangle.Height;

        public ChargeThrow chargeThrowComponent;

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

	    public bool IsCharging => SpriteInstance.CurrentChainName == "Aim";
	    public bool IsThrowing => new[] { "Aim", "Throw" }.Contains(SpriteInstance.CurrentChainName);
        public bool IsHit => new[] {"Hit", "Fall", "Down"}.Contains(SpriteInstance.CurrentChainName);
	    private bool ShouldFlipHitAnimation;

        public bool IsDying { get; private set; }


        #endregion

        public event Action Dying;

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

            InstantiateChargeThrowComponent();

            CircleInstance.Color = TeamIndex == 0 ? Color.Red : Color.Blue;
        }

        private void InstantiateChargeThrowComponent()
        {
            chargeThrowComponent = new ChargeThrow();
            chargeThrowComponent.LowEndChargeRate = LowEndChargeRate;
            chargeThrowComponent.HighEndChargeRate = HighEndChargeRate;
        }

        public void InitializeKeyboardControls()
        {
            var keyboard = InputManager.Keyboard;

            MovementInput = keyboard.Get2DInput(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
            ActionButton = keyboard.GetKey(Keys.Space);

            AimingInput = keyboard.Get2DInput(Keys.J, Keys.L, Keys.I, Keys.K);

            TauntButton = keyboard.GetKey(Keys.T);

        }

        public void InitializeXbox360Controls(Xbox360GamePad gamePad)
        {
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
            IsAttemptingCatch = false;
            IsDodging = false;

            BallHolding = ballInstance;
            BallHolding.ThrowOwner = this;
            ballInstance.OwnerTeam = this.TeamIndex;
        }


        #endregion

        #region Activity

        private void CustomActivity()
		{
            MovementActivity();

            DodgeActivity();

		    CatchActivity();

		    ThrowActivity();

            HudActivity();

        #if DEBUG
		    if (IsHoldingBall && DebuggingVariables.ShowTargetedPlayers) ShowTargetedPlayers();
        #endif

		    SetAnimation();
		}

	    private void ThrowActivity()
	    {
            //Early out for null inputs, or performing other action
            if (ActionButton == null || MovementInput == null || IsDodging || IsPerformingSuccessfulCatch) return;

	        if (ActionButton.WasJustPressed)
	        {
	            chargeThrowComponent.Reset();
	        }

	        if (ActionButton.WasJustReleased && IsHoldingBall)
	        {
	            ExecuteThrow();
	        }

	        bool isCharging = IsHoldingBall && ActionButton.IsDown;
	        if (isCharging)
	        {
	            chargeThrowComponent.ChargeActivity();
	            ThrowChargeMeterRuntimeInstance.MeterPercent = chargeThrowComponent.MeterPercent;
	        }
	        ThrowChargeMeterRuntimeInstance.Visible = isCharging;
        }

	    private void CatchActivity()
	    {
            //Check current catching status
	        if (IsAttemptingCatch)
	        {
	            CurrentCatchAttemptTime += FlatRedBall.TimeManager.SecondDifference;
                
                //Catch has ended
	            if (CurrentCatchAttemptTime > CatchEffectivenessDuration + CatchFailRecoveryDuration)
	            {
	                IsAttemptingCatch = false;
	            }
	        }

	        //Early out for null inputs or performing other activity
            if (MovementInput == null || ActionButton == null || IsHoldingBall || IsDodging || IsAttemptingCatch) return;

            //Actionbutton pressed and not pressing any direction
	        if (ActionButton.WasJustPressed && 
                (MovementInput.Y == 0 && MovementInput.X == 0))
	        {
	            IsAttemptingCatch = true;
	            CurrentCatchAttemptTime = 0;
	        }
	    }

        private void DodgeActivity()
        {
            //Early out for null inputs or performing other activity
            if (ActionButton == null || MovementInput == null || IsHoldingBall || IsAttemptingCatch) return;

            //Check if player pressed action button in combination with direction
            if (ActionButton.WasJustPressed && 
                (MovementInput.X != 0 || MovementInput.Y != 0))
            {
                IsDodging = true;
                var dodgeSoundPan = MathHelper.Clamp(X / 540f, -1, 1);
                playerDodgeSound.Pan = dodgeSoundPan;
                playerDodgeSound.Play();
            }
        }

	    private void MovementActivity()
	    {
	        if (MovementInput != null &&
	            !IsThrowing && !IsHit && !IsAttemptingCatch && !IsPerformingSuccessfulCatch)
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


	    private void HudActivity()
	    {
	        this.ActiveMarkerRuntimeInstance.X = this.X;
	        this.ActiveMarkerRuntimeInstance.Y = this.Y + 230;

	        this.HealthBarRuntimeInstance.X = this.X - this.HealthBarRuntimeInstance.Width / 2;
	        this.HealthBarRuntimeInstance.Y = this.Y + 195;
	        this.HealthBarRuntimeInstance.HealthWidth = this.HealthPercentage;


	        this.EnergyBarRuntimeInstance.X = this.X + (this.TeamIndex == 0 ? -120 : 120);
	        this.EnergyBarRuntimeInstance.Y = this.Y;
	        this.EnergyBarRuntimeInstance.EnergyHeight = this.EnergyPercentage;

	        ThrowChargeMeterRuntimeInstance.X = X;
	        ThrowChargeMeterRuntimeInstance.Y = Y + 215;
	    }


#if DEBUG
	    private void ShowTargetedPlayers()
	    {
	        foreach (var player in AllPlayers)
	        {
	            player.CircleInstance.Color = (player.TeamIndex == 0 ? Color.Red : Color.Blue);
	        }

	        var targetPlayer = GetTargetedPlayer();
	        if (targetPlayer != null) targetPlayer.CircleInstance.Color = Color.Yellow;
	    }
#endif

        #endregion

        #region Actions and Reactions
        internal void CatchBall(Ball ballInstance)
	    {
	        IsAttemptingCatch = false;
            IsPerformingSuccessfulCatch = true;
	        IsHardCatch = ballInstance.Velocity.Length() > 1800;

            BallHolding = ballInstance;
	        BallHolding.CurrentOwnershipState = Ball.OwnershipState.Held;
            BallHolding.ThrowOwner = this;
	        BallHolding.OwnerTeam = this.TeamIndex;
        }

        internal void GetHitBy(Ball ballInstance)
        {
            IsAttemptingCatch = false;

            SpriteInstance.CurrentChainName = "Hit";
            //Only take damage from other team
            if (ballInstance.OwnerTeam != TeamIndex)
            {
                bool wasAlive = HealthPercentage > 0;

                this.HealthPercentage -= DamageWhenHitting;

                bool isAlive = this.HealthPercentage > 0;

                if(wasAlive && !isAlive)
                {
                    IsDying = true;
                    Dying?.Invoke();
                }
            }


            //Set their reaction based on where the ball came from
            ShouldFlipHitAnimation = (ballInstance.X > X);
            SpriteInstance.FlipHorizontal = ShouldFlipHitAnimation;
        }

        private void ExecuteThrow()
        {
            bool isFailedThrow = chargeThrowComponent.FailedThrow;

            if ( isFailedThrow)
            {
                ThrowVelocity = MinThrowVelocity;
            }
            else
            {
                ThrowVelocity = MinThrowVelocity + ((MaxThrowVelocity - MinThrowVelocity) *
                                                    chargeThrowComponent.EffectiveChargePercent);
            }

            var targetPlayer = GetTargetedPlayer();

            if (targetPlayer != null)
            {
                var direction = targetPlayer.Position - this.Position;

                if (isFailedThrow)
                {
                    // Throw it straight, slow, it'll hit the ground right away
                    BallHolding.AltitudeVelocity = 0;
                }
                else
                {
                    var distanceToTarget = direction.Length();

                    var timeToTarget = .5f * distanceToTarget / ThrowVelocity;

                    // arc that badboy:
                    float desiredYVelocity = BallHolding.BallGravity * timeToTarget;

                    BallHolding.AltitudeVelocity = desiredYVelocity;
                }


                direction.Normalize();
                BallHolding.Detach();
                BallHolding.PerformThrownLogic(this, direction * ThrowVelocity);

                BallHolding = null;
                justReleasedBall = true;

#if DEBUG
                if (DebuggingVariables.PlayerAlwaysControlsBallholder)
                {
                    this.ClearInput();
                    if(InputManager.NumberOfConnectedGamePads != 0)
                    {
                        targetPlayer.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
                    }
                    else
                    {
                        targetPlayer.InitializeKeyboardControls();
                    }
                }
#endif
            }
        }

	    private Player GetTargetedPlayer()
	    {
            //Exclude our players, and exclude players that are knocked out
	        var opposingTeamPlayers = AllPlayers.Where(p => p.TeamIndex != TeamIndex && !p.IsHit).ToList();
            
	        if (!opposingTeamPlayers.Any())
	        {
	            //No players left!
                return null;
	        }

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
        #endregion

        #region Animation
        private void SetAnimation()
	    {
	        var canThrowOrDodge = ActionButton != null && !IsHit && !IsAttemptingCatch && !IsPerformingSuccessfulCatch;
	        if (canThrowOrDodge)
	        {
	            if (justReleasedBall)
	            {
	                SpriteInstance.CurrentChainName = "Throw";
	                justReleasedBall = false;
	            }
                else if (ActionButton.IsDown && IsHoldingBall)
	            {
	                SpriteInstance.CurrentChainName = "Aim";
	            }
                else if (IsDodging)
	            {
	                if (SpriteInstance.CurrentChainName == "Dodge" && SpriteInstance.JustCycled)
	                {
	                    IsDodging = false;
	                }
	                else if (SpriteInstance.CurrentChainName != "Dodge")
	                {
	                    SpriteInstance.CurrentChainName = "Dodge";
                    }
	            }

	            SpriteInstance.FlipHorizontal = TeamIndex == 0;
            }

	        var canCatch = (IsAttemptingCatch || (IsPerformingSuccessfulCatch));
	        if (canCatch)
	        {
	            if (IsAttemptingCatch && CatchIsEffective)
	            {
	                SpriteInstance.CurrentChainName = "Catch";
	            }
	            else if (IsAttemptingCatch && !CatchIsEffective)
	            {
	                SpriteInstance.CurrentChainName = "StaleCatch";
	            }
	            else if (IsPerformingSuccessfulCatch)
	            {
	                if (new[] {"HardCatch", "SoftCatch"}.Contains(SpriteInstance.CurrentChainName) &&
	                    SpriteInstance.JustCycled)
	                {
	                    IsPerformingSuccessfulCatch = false;
	                }
	                else
	                {
	                    SpriteInstance.CurrentChainName = IsHardCatch ? "HardCatch" : "SoftCatch";
	                }
	            }

	            SpriteInstance.FlipHorizontal = TeamIndex == 0;
	        }
	        else if (IsPerformingSuccessfulCatch && SpriteInstance.JustCycled)
	        {
	            IsPerformingSuccessfulCatch = false;
	        }

            var canStandOrRun = !IsHit && !IsAttemptingCatch && !IsPerformingSuccessfulCatch &&
	                            ((!IsThrowing && !IsDodging) || SpriteInstance.JustCycled);
            if (canStandOrRun)
	        {

	            if (MovementInput?.X != 0 || MovementInput?.Y != 0)
	            {
	                SpriteInstance.CurrentChainName = IsHoldingBall ? "RunHold" : "Run";
	                SpriteInstance.FlipHorizontal = Velocity.X > 0;
	            }
	            else
	            {
	                SpriteInstance.CurrentChainName = IsHoldingBall ? "IdleHold" : "Idle";
	                SpriteInstance.FlipHorizontal = TeamIndex == 0;
	            }
	        }

	        if (IsHit)
	        {
	            SpriteInstance.FlipHorizontal = ShouldFlipHitAnimation;

	            if (SpriteInstance.CurrentChainName == "Down" && !SpriteInstance.JustCycled)
	            {
                    //Make player blink before disappearing
	                var currentTime = FlatRedBall.TimeManager.CurrentTime;
	                SpriteInstance.Visible = currentTime % 2 == 1 || currentTime % 0.5 < 0.25;
	            }
                else if (SpriteInstance.JustCycled)
	            {
	                if (HealthPercentage > 0)
	                {
	                    //Player still has health, goes back to normal
	                    SpriteInstance.CurrentChainName = "Idle";
	                    SpriteInstance.FlipHorizontal = (TeamIndex == 0);
	                }
	                else if (SpriteInstance.CurrentChainName == "Hit")
	                {
	                    //Player is out of health, down they go
	                    SpriteInstance.CurrentChainName = "Fall";
	                    SpriteInstance.IgnoreAnimationChainTextureFlip = false;
	                    SpriteInstance.FlipHorizontal = ShouldFlipHitAnimation;
	                }
	                else if (SpriteInstance.CurrentChainName == "Fall")
	                {
	                    //Lay on the ground for the duration of the down animation
	                    SpriteInstance.CurrentChainName = "Down";
	                    SpriteInstance.IgnoreAnimationChainTextureFlip = false;
	                    SpriteInstance.FlipHorizontal = ShouldFlipHitAnimation;
	                }
	                else
	                {
	                    //Now they're out
	                    this.Destroy();
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
