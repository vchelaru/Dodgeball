using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dodgeball.Entities;
using FlatRedBall.Input;
using FlatRedBall.Math;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region Fields/Properties
        private readonly Player _player;
        private PositionedObjectList<Player> _allPlayers;
        private readonly Ball _ball;
        
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
            _player = player;
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

            AssignInputsToPlayer();
        }
        #endregion

        #region Activity
        public void Activity()
        {
            //Re-assign inputs if the player has taken control of the character for debug logic
            if (!_player.HasInputs) AssignInputsToPlayer();

            MakeDecisions();
        }
        #endregion

        private void AssignInputsToPlayer()
        {
            _player?.InitializeAIControl(this);
        }
    }
}
