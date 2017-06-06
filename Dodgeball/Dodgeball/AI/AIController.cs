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

        //Object references
        private readonly Player player;
        private PositionedObjectList<Player> _allPlayers;
        private readonly Ball _ball;

        //Random determinations
        private Random random;

        //Ball-throwing logic
        private double _ballHeldTime;
        private double _timeToDelayThrow = 1;

        //Wandering logic
        private const double MaxWanderTime = 1.5;
        private bool isWandering;
        private double timeToWander = 2;
        private double timeWandering = 0;
        private AI2DInput.Directions wanderDirection = AI2DInput.Directions.None;

        //Retrieving logic
        private bool isRetrieving;

        //Dodge logic
        private const double MaxDodgeTime = 1;
        private bool isDodging;
        private double timeToDodge = 2;
        private double timeDodging = 0;
        private AI2DInput.Directions dodgeDirection = AI2DInput.Directions.None;

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
            _allPlayers = player.AllPlayers;
            _ball = ball;

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

            AssignInputsToPlayer();
        }
        #endregion

        #region Activity
        public void Activity()
        {
            //Re-assign inputs if the player has taken control of the character for debug logic
            if (!player.HasInputs) AssignInputsToPlayer();

            UpdateConditions();
            MakeDecisions();
        }

        private void UpdateConditions()
        {
            if (player.IsHoldingBall)
            {
                _ballHeldTime += FlatRedBall.TimeManager.LastSecondDifference;
            }
            if (isWandering)
            {
                timeWandering += FlatRedBall.TimeManager.LastSecondDifference;
            }
            if (isDodging)
            {
                timeDodging += FlatRedBall.TimeManager.LastSecondDifference;
            }
        }

        #endregion

        private void AssignInputsToPlayer()
        {
            player?.InitializeAIControl(this);
        }
    }
}
