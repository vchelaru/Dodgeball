using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private bool ShouldThrowBall => ball.CurrentOwnershipState == Ball.OwnershipState.Held && player.IsHoldingBall && ballHeldTime > timeToDelayThrow;

        private bool ShouldTaunt => ball.CurrentOwnershipState == Ball.OwnershipState.Held && ball.OwnerTeam == player.TeamIndex && ball.ThrowOwner != player;

        private bool ShouldWander => (ball.CurrentOwnershipState == Ball.OwnershipState.Held || ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) && 
                                    ball.OwnerTeam == player.TeamIndex;

        private bool ShouldDodge => (ball.CurrentOwnershipState == Ball.OwnershipState.Held || ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                    ball.OwnerTeam != player.TeamIndex;

        private bool ShouldRetrieveBall => ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldThrowBall)
            {
                _aimingInput.Move(AimDirection());
                _movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                _actionButton.Release();
                ballHeldTime = 0;
                hasActed = true;
            }

            if (!hasActed && !isWandering && !isDodging && !isRetrieving)
            {
                var decisionToDoNothing = random.NextDouble() < 0.05;
                hasActed = decisionToDoNothing;
            }

            if (!hasActed && (ShouldRetrieveBall || isRetrieving))
            {
                var decisionToRetrieveBall = random.NextDouble() < 0.05;
                if (decisionToRetrieveBall || isRetrieving)
                {
                    _movementInput.Move(RetrieveBallDirections());
                    isRetrieving = true;
                    hasActed = true;
                }
            }
            else
            {
                isRetrieving = false;
            }

            var decisionToDodge = random.NextDouble() < 0.08;
            if (!hasActed && !isWandering && (ShouldDodge && (decisionToDodge || isDodging)))
            {
                isDodging = true;
                dodgeDirection = DodgeDirection();
                _movementInput.Move(dodgeDirection);
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
                _movementInput.Move(wanderDirection);

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
                _tauntButton.Release();
                hasActed = true;
            }

            if (!hasActed)
            {
                _movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
            }
        }
    }
}