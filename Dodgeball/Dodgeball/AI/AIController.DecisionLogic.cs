using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private bool ShouldThrowBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && player.IsHoldingBall && _ballHeldTime > _timeToDelayThrow;

        private bool ShouldTaunt => _ball.CurrentOwnershipState == Ball.OwnershipState.Held && _ball.OwnerTeam == player.TeamIndex && _ball.ThrowOwner != player;

        private bool ShouldWander => (_ball.CurrentOwnershipState == Ball.OwnershipState.Held || _ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) && 
                                    _ball.OwnerTeam == player.TeamIndex;

        private bool ShouldDodge => (_ball.CurrentOwnershipState == Ball.OwnershipState.Held || _ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                    _ball.OwnerTeam != player.TeamIndex;

        private bool ShouldRetrieveBall => _ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldThrowBall)
            {
                _aimingInput.Move(AimDirection());
                movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                _actionButton.Release();
                _ballHeldTime = 0;
                hasActed = true;
            }

            if (!hasActed && !isWandering && !isDodging)
            {
                var decisionToDoNothing = random.NextDouble() < 0.01;
                hasActed = decisionToDoNothing;
            }

            if (!hasActed && ShouldRetrieveBall)
            {
                var decisionToRetrieveBall = random.NextDouble() < 0.6;
                if (decisionToRetrieveBall || isRetrieving)
                {
                    movementInput.Move(RetrieveBallDirections());
                    isRetrieving = true;
                    hasActed = true;
                }
            }
            else
            {
                isRetrieving = false;
            }

            var decisionToDodge = random.NextDouble() < 0.2;
            if (!hasActed && !isWandering && (ShouldDodge && (decisionToDodge || isDodging)))
            {
                isDodging = true;
                dodgeDirection = DodgeDirection();
                movementInput.Move(dodgeDirection);
                hasActed = true;
            }
            else
            {
                isDodging = false;
                dodgeDirection = AI2DInput.Directions.None;
                timeDodging = 0;
            }

            var decisionToWander = random.NextDouble() < 0.01;
            if (!hasActed && !isDodging && (ShouldWander && (decisionToWander || isWandering)))
            {
                isWandering = true;
                wanderDirection = WanderDirection();
                movementInput.Move(wanderDirection);

                hasActed = true;
            }
            else
            {
                isWandering = false;
                wanderDirection = AI2DInput.Directions.None;
                timeWandering = 0;
            }

            var decisionToTaunt = random.NextDouble() < 0.003;
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
                movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
                _actionButton.Release();
            }
        }
    }
}