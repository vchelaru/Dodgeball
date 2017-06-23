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

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region Fields/Properties

        private const double distanceToConsiderDodging = 500;

        private AI2DInput.Directions currentMovementDirections = AI2DInput.Directions.None;

        //Object references
        private readonly Player player;
        private PositionedObjectList<Player> allPlayers;
        private readonly Ball ball;

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
        #endregion

        #region Initialize
        public AIController(Player player, Ball ball)
        {
            //Object references
            this.player = player;
            allPlayers = player.AllPlayers;
            this.ball = ball;

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

        private void AssignInputsToPlayer()
        {
            player?.InitializeAIControl(this);
        }

        private void UpdateInputs()
        {
            _actionButton.Update();
            _tauntButton.Update();
        }
    }
}
