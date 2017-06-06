using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private bool ShouldThrowBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _player.IsHoldingBall && _ballHeldTime > _timeToDelayThrow;

        private bool ShouldTaunt => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.OwnerTeam == _player.TeamIndex && _ball.ThrowOwner != _player;

        private bool ShouldWander => _ball.CurrentOwnershipState == Ball.OwnershipState.Held || 
            (_ball.CurrentOwnershipState == Ball.OwnershipState.Thrown && _ball.ThrowOwner.TeamIndex == _player.TeamIndex);

        private bool ShouldDodge => (_ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                    _ball.OwnerTeam != _player.TeamIndex;

        private bool ShouldRetrieveBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldThrowBall)
            {
                _aimingInput.Move(AimDirection());
                _movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                _actionButton.Release();
                _ballHeldTime = 0;
                hasActed = true;
            }

            if (!hasActed && !_isWandering && !_isDodging)
            {
                var decisionToDoNothing = random.NextDouble() > 0.001;
                hasActed = decisionToDoNothing;
            }

            if (!hasActed && ShouldRetrieveBall)
            {
                var decisionToRetrieveBall = random.NextDouble() > 0.005;
                if (decisionToRetrieveBall)
                {
                    _movementInput.Move(RetrieveBallDirections());
                    hasActed = true;
                }
            }

            var decisionToDodge = random.NextDouble() > 0.01;
            if (!hasActed && !_isWandering && (ShouldDodge && (decisionToDodge || _isDodging)))
            {
                _isDodging = true;
                _dodgeDirection = DodgeDirection();
                _movementInput.Move(_dodgeDirection);
                hasActed = true;
            }
            else
            {
                _isDodging = false;
                _dodgeDirection = AI2DInput.Directions.None;
                _timeDodging = 0;
            }

            var decisionToWander = random.NextDouble() > 0.01;
            if (!hasActed && !_isDodging && (ShouldWander && (decisionToWander || _isWandering)))
            {
                _isWandering = true;
                _wanderDirection = WanderDirection();
                _movementInput.Move(_wanderDirection);

                hasActed = true;
            }
            else
            {
                _isWandering = false;
                _wanderDirection = AI2DInput.Directions.None;
                _timeWandering = 0;
            }

            var decisionToTaunt = random.NextDouble() > 0.003;
            if (!hasActed && (ShouldTaunt && decisionToTaunt))
            {
                _tauntButton.Press();
                hasActed = true;
            }
            else
            {
                _tauntButton.Release();
            }

            if (!hasActed)
            {
                _movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
                _actionButton.Release();
            }
        }
    }
}