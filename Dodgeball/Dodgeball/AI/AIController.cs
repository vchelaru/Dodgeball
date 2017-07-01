using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dodgeball.Entities;
using Dodgeball.GumRuntimes;
using FlatRedBall.Input;
using FlatRedBall.Math;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;

namespace Dodgeball.AI
{
    public partial class AIController
    {

        #region Difficulty Fields/Properties
        /// <summary>
        /// This determines how difficult the AI is on a scale of 1-9
        /// </summary>
        private int difficultyLevel = 5;
        public int DifficultyLevel {
            get { return difficultyLevel; }
            set { difficultyLevel = (int)MathHelper.Clamp(value, 1, 9); }
        }

        /// <summary>
        /// Multiply probabilities by this property if we want them to increase with difficulty
        /// </summary>
        private float IncreaseWithDifficulty => DifficultyLevel / 9f;

        /// <summary>
        /// Multiply probabilities by this property if we want them to decrease with difficulty
        /// </summary>
        private float DecreaseWithDifficulty => 1 - IncreaseWithDifficulty;
        #endregion

        #region Input/Control Fields & Properties
        //Public interfaces for use by Player expecting a controller
        public I2DInput MovementInput { get; private set; }
        public IPressableInput ActionButton { get; private set; }
        public I2DInput AimingInput { get; private set; }
        public IPressableInput TauntButton { get; private set; }

        //Local simulated inputs controlled by AI
        private readonly AI2DInput _movementInput;
        private readonly AI2DInput _aimingInput;
        private readonly AIPressableInput _actionButton;
        private readonly AIPressableInput _tauntButton;

        private AI2DInput.Directions currentMovementDirections = AI2DInput.Directions.None;
        #endregion

        #region Other Fields/Properties
        //Object references
        private readonly Player player;
        private List<Player> teamPlayers;
        private readonly Ball ball;

        private float myPersonalSpace;

        //Random determinations
        private Random random;

        //Getting out of the way logic
        private bool isGettingOutOfTheWay = false;
        private float maxTolerableDistanceToBallHolder;

        //Ball-throwing logic
        private bool isPositioningForThrow = false;
        private double ballHeldTime;
        private double timeToDelayThrow = 1;

        //Wandering logic
        private const double MaxWanderTime = 1.5;
        private bool isWandering;
        private double timeToWander = 2;
        private double timeWandering = 0;
        
        //Retrieving logic
        private bool isRetrieving;

        //Evasion logic
        private const double MaxEvasionTime = 1;
        private bool isEvading;
        private double timeToEvade = 2;
        private double timeEvading = 0;

        //Dodging logic
        private const double distanceToConsiderDodging = 500;

        //Catching logic
        private const double distanceToConsiderCatching = 500;
        private double distanceToOptimalCatch => player.CatchEffectivenessDuration * ball.Velocity.Length();
        #endregion

        #region Initialize
        public AIController(Player player, Ball ball, int difficulty = 5)
        {
            DifficultyLevel = difficulty;

            //Object references
            this.player = player;
            teamPlayers = player.AllPlayers.Where(p => p.TeamIndex == player.TeamIndex && p != this.player).ToList();
            this.ball = ball;

            myPersonalSpace = player.CircleInstance.Radius * 1.5f;

            //Local simulated inputs
            _movementInput = new AI2DInput();
            _actionButton = new AIPressableInput();
            _aimingInput = new AI2DInput();
            _tauntButton = new AIPressableInput();

            //Public inputs for use by Player
            MovementInput = _movementInput;
            ActionButton = _actionButton;
            TauntButton = _tauntButton;
            AimingInput = _aimingInput;

            //Private random with seed
            random = new Random(Guid.NewGuid().GetHashCode());

            //Determine spacing between players
            maxTolerableDistanceToBallHolder = player.CircleInstance.Radius * 2f;

            AssignInputsToPlayer();
        }
        #endregion

        #region Activity
        public void Activity()
        {
            //Re-assign inputs if the player has taken control of the character for debug logic
            if (!player.HasInputs) AssignInputsToPlayer();
            UpdateInputs();
            UpdateConditions();
            MakeDecisions();
        }

        private void UpdateConditions()
        {
            if (player.IsHoldingBall)
            {
                ballHeldTime += FlatRedBall.TimeManager.LastSecondDifference;
            }
            if (isWandering)
            {
                timeWandering += FlatRedBall.TimeManager.LastSecondDifference;
            }
            if (isEvading)
            {
                timeEvading += FlatRedBall.TimeManager.LastSecondDifference;
            }
            if (isRetrieving)
            {
                isRetrieving = ShouldRetrieveBall;
            }
            if (!isEvading && !isPositioningForThrow && !isWandering && !isRetrieving && !isGettingOutOfTheWay)
            {
                currentMovementDirections = AI2DInput.Directions.None;
            }
        }

        #endregion

        #region Input methods
        private void AssignInputsToPlayer()
        {
            player?.InitializeAIControl(this);
        }

        private void UpdateInputs()
        {
            //release action button from previous dodge
            if (!player.IsHoldingBall && _actionButton.IsDown)
            {
                _actionButton.Release();
            }

            _actionButton.Update();
            _tauntButton.Update();
        }
        #endregion
    }
}
