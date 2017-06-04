using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private bool ShouldThrowBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.ThrowOwner == _player;

        private bool ShouldTaunt => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.OwnerTeam == _player.TeamIndex && _ball.ThrowOwner != _player;

        private bool ShouldWander => _ball.CurrentOwnershipState == Ball.OwnershipState.Free ||
                                     (_ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.OwnerTeam != _player.TeamIndex);

        private bool ShouldDodge => _ball.CurrentOwnershipState == Ball.OwnershipState.Thrown &&
                                    _ball.OwnerTeam != _player.TeamIndex;

        private AI2DInput.Directions AimDirection()
        {
            //TODO:  Aim at particular player
            return AI2DInput.Directions.Down;
        }

        private AI2DInput.Directions WanderDirection()
        {
            //TODO:  Wander an area
            return AI2DInput.Directions.None;
        }

        private AI2DInput.Directions DodgeDirection()
        {
            //TODO: Dodge intelligently away from ball
            return (AI2DInput.Directions.Down | AI2DInput.Directions.Right);
        }

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldThrowBall)
            {
                var aimingDirections = AimDirection();
                _aimingInput.Move(aimingDirections);

                _actionButton.Press();
                hasActed = true;
            }
            else
            {
                _actionButton.Release();
                _aimingInput.Move(AI2DInput.Directions.None);
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

            if (!hasActed && ShouldWander)
            {
                var wanderDirections = WanderDirection();
                _movementInput.Move(wanderDirections);
                hasActed = true;
            }
            else
            {
                _aimingInput.Move(AI2DInput.Directions.None);
            }

            if (!hasActed && ShouldDodge)
            {
                var dodgeDirections = DodgeDirection();
                _movementInput.Move(dodgeDirections);
                hasActed = true;
            }
            else
            {
                _movementInput.Move(AI2DInput.Directions.None);
            }
        }
    }
}