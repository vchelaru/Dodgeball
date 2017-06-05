using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private bool ShouldThrowBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _player.BallHolding != null && _ballHeldTime > _timeToDelayThrow;

        private bool ShouldTaunt => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.OwnerTeam == _player.TeamIndex && _ball.ThrowOwner != _player;

        private bool ShouldWander => _ball.CurrentOwnershipState == Ball.OwnershipState.Held || 
            (_ball.CurrentOwnershipState == Ball.OwnershipState.Thrown && _ball.ThrowOwner.TeamIndex == _player.TeamIndex);

        private bool ShouldDodge => (_ball.CurrentOwnershipState == Ball.OwnershipState.Thrown || _ball.CurrentOwnershipState == Ball.OwnershipState.Held) &&
                                    _ball.OwnerTeam != _player.TeamIndex;

        private bool ShouldRetrieveBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldThrowBall)
            {
                _aimingInput.Move(AimDirection());

                _actionButton.Press();
                _actionButton.Release();
                _ballHeldTime = 0;
                hasActed = true;
            }
            else
            {
                _actionButton.Release();
                _aimingInput.Move(AI2DInput.Directions.None);
            }

            if (!hasActed && ShouldWander)
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

            if (!hasActed && ShouldDodge)
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

            if (!hasActed && ShouldRetrieveBall)
            {
                _movementInput.Move(RetrieveBallDirections());
                hasActed = true;
            }

            if (!hasActed && ShouldTaunt)
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
            }
        }
    }
}