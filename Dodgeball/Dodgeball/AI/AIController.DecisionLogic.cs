using System;
using Dodgeball.Components;
using Dodgeball.Entities;
using Microsoft.Xna.Framework.GamerServices;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region Constant Probabilities

        //The base chance of an action, when appropriate, on each frame

        //Bypasses all below actions, used as difficulty modifier
        private readonly float _probOfInaction = 0.1f;

        private readonly float _probOfDodge = 0.03f;
        private readonly float _probOfBallRetrieval = 0.05f;
        private readonly float _probOfEvasion = 0.1f;
        private readonly float _probOfWandering = 0.01f;
        private readonly float _probOfTaunting = 0.003f;
        private readonly float _probOfFailedCatch = 0.02f;
        private readonly float _probOfSuccesfulCatch = 0.02f;

        private readonly float _probOfOptimalThrow = 0.04f;

        //These are probabilities that aren't directly referenced by logic, but are altered then used as a dynamic probability
        private readonly float _baseProbOfSuboptimalThrow = 0.1f;
        #endregion

        #region Dynamic Probabilities
        //The base chance of an action, altered by conditions
        private float _probOfNonOptimalThrow => _baseProbOfSuboptimalThrow * CurrentThrowCharge/100;

        #endregion

        #region Difficulty-adjusted Probabilities
        //Final probability after conditions and difficulty modification have been considered
        private float ProbabilityOfInaction => _probOfInaction * DecreaseWithDifficulty;
        private float ProbabilityOfDodge => _probOfDodge * IncreaseWithDifficulty;
        private float ProbabilityOfBallRetrieval => _probOfBallRetrieval * IncreaseWithDifficulty;
        private float ProbabilityOfEvasion => _probOfEvasion * IncreaseWithDifficulty;
        private float ProbabilityOfWandering => _probOfWandering * IncreaseWithDifficulty;
        private float ProbabilityOfTaunting => _probOfTaunting * (DecreaseWithDifficulty / 2);
        private float ProbabilityOfOptimalThrow => _probOfOptimalThrow * IncreaseWithDifficulty;
        private float ProbabilityOfSuboptimalThrow => _probOfNonOptimalThrow * DecreaseWithDifficulty;
        private float ProbabilityOfFailedCatch => _probOfFailedCatch * DecreaseWithDifficulty;
        private float ProbabilityOfSuccessfulCatch => _probOfFailedCatch * IncreaseWithDifficulty;

        #endregion

        #region Reference Properties
        //Convenient properties that shorten references
        private float CurrentThrowCharge => player.chargeThrowComponent.MeterPercent;

        #endregion

        #region Decision checks

        private bool ShouldDoNothing => !isWandering && !isEvading && !isRetrieving && !isPositioningForThrow && !IsChargingThrow;

        private bool ShouldPositionForThrow => ball.ThrowOwner == player && !player.IsInFront && !IsChargingThrow;

        private bool ShouldThrowBall => player.IsHoldingBall && ballHeldTime > timeToDelayThrow && !IsChargingThrow;

        private bool ShouldTaunt => ball.CurrentOwnershipState == Ball.OwnershipState.Held &&
                                    ball.OwnerTeam == player.TeamIndex && ball.ThrowOwner != player;

        private bool ShouldWander => (ball.CurrentOwnershipState == Ball.OwnershipState.Held ||
                                      ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                     ball.ThrowOwner != player &&
                                     ball.OwnerTeam == player.TeamIndex;

        private bool ShouldEvade => (ball.CurrentOwnershipState == Ball.OwnershipState.Held ||
                                     ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                    ball.OwnerTeam != player.TeamIndex;

        private bool ShouldDodge => (ball.CurrentOwnershipState == Ball.OwnershipState.Thrown &&
                                     ball.OwnerTeam != player.TeamIndex &&
                                     !player.IsDodging &&
                                     !player.IsAttemptingCatch &&
                                     (ball.Position - player.Position).Length() < distanceToConsiderDodging &&
                                     ball.TrajectoryPolygon.CollideAgainst(player.CircleInstance));

        private bool ShouldCatch => (ball.CurrentOwnershipState == Ball.OwnershipState.Thrown &&
                                     ball.OwnerTeam != player.TeamIndex &&
                                     !player.IsDodging &&
                                     !player.IsAttemptingCatch &&
                                     (ball.Position - player.Position).Length() < distanceToConsiderCatching &&
                                     ball.TrajectoryPolygon.CollideAgainst(player.CircleInstance));

        private bool ShouldRetrieveBall => ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private bool ShouldGetOutOfTheWayOfBallHolder => ball.CurrentOwnershipState == Ball.OwnershipState.Held &&
                                                         ball.OwnerTeam == player.TeamIndex &&
                                                         ball.ThrowOwner != player &&
                                                         ((player.TeamIndex == 0 && player.X >= ball.ThrowOwner.X ||
                                                           player.TeamIndex == 1 && player.X <= ball.ThrowOwner.X)) &&
                                                         //Multiply the Y-difference by the X-difference to create a cone of avoidance
                                                         Math.Abs(player.Y - ball.ThrowOwner.Y) <=
                                                         Math.Max(maxTolerableDistanceToBallHolder,
                                                             maxTolerableDistanceToBallHolder *
                                                             Math.Abs(player.X - ball.ThrowOwner.X) / 125);
        #endregion

        #region Condition checks

        private bool IsChargingThrow => player.IsHoldingBall && _actionButton.IsDown;

        private bool ThrowChargeIsOptimal => CurrentThrowCharge > ChargeThrow.MaxValidThrowPercent * 0.8f &&
                                             CurrentThrowCharge <= ChargeThrow.MaxValidThrowPercent;

        private bool CatchAttemptIsOptimal => (ball.Position - player.Position).Length() < distanceToOptimalCatch &&
                                              ball.TrajectoryPolygon.CollideAgainst(player.CircleInstance);

        #endregion

        private void MakeDecisions()
        {
            var hasActed = false;

            ConsiderGettingOutOfTheWay(ref hasActed);

            ConsiderDoingNothing(ref hasActed);

            ConsiderThrowingTheBall(ref hasActed);

            ConsiderPositioningToThrow(ref hasActed);

            ConsiderCatching(ref hasActed);

            ConsiderDodging(ref hasActed);

            ConsiderRetrievingTheBall(ref hasActed);

            ConsiderEvading(ref hasActed);

            ConsiderWanderingAimlessly(ref hasActed);

            ConsiderTaunting(ref hasActed);

            //Release controls /movement if not acting
            ResetInputs(hasActed);
        }

        private void ResetInputs(bool hasActed)
        {
            if (!hasActed)
            {
                currentMovementDirections = AI2DInput.Directions.None;
                _movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
            }
        }

        private void ConsiderGettingOutOfTheWay(ref bool hasActed)
        {
            if (!hasActed && ShouldGetOutOfTheWayOfBallHolder)
            {
                isGettingOutOfTheWay = true;
                _movementInput.Move(RetrieveGetOutOfTheWayDirections());
                hasActed = true;
            }
            else
            {
                isGettingOutOfTheWay = false;
            }
        }

        private void ConsiderPositioningToThrow(ref bool hasActed)
        {
            if (!hasActed && ShouldPositionForThrow)
            {
                isPositioningForThrow = true;
                _movementInput.Move(RetrieveDirectionsForThrowPositioning());
                hasActed = true;
            }
            else
            {
                isPositioningForThrow = false;
            }
        }

        private void ConsiderThrowingTheBall(ref bool hasActed)
        {
            if (!hasActed && ShouldThrowBall)
            {
                isPositioningForThrow = false;
                _aimingInput.Move(AimDirection());
                _movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                ballHeldTime = 0;
                hasActed = true;
            }
            else if (!hasActed && IsChargingThrow)
            {
                var chanceOfThrow = random.NextDouble();
                var decisionToRelease = (!ThrowChargeIsOptimal && (chanceOfThrow < ProbabilityOfSuboptimalThrow)) ||
                                        (ThrowChargeIsOptimal && chanceOfThrow < ProbabilityOfOptimalThrow);
                if (decisionToRelease) _actionButton.Release();
                hasActed = true;
            }
        }

        private void ConsiderDoingNothing(ref bool hasActed)
        {
            if (!hasActed && ShouldDoNothing)
            {
                var decisionToDoNothing = random.NextDouble() < ProbabilityOfInaction;
                hasActed = decisionToDoNothing;
                currentMovementDirections = AI2DInput.Directions.None;
                _movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
            }
        }

        private void ConsiderCatching(ref bool hasActed)
        {
            if (!hasActed && ShouldCatch)
            {
                var chanceOfCatch = random.NextDouble();
                var decisionToCatch = (!CatchAttemptIsOptimal && (chanceOfCatch < ProbabilityOfFailedCatch)) ||
                                      (CatchAttemptIsOptimal && chanceOfCatch < ProbabilityOfSuccessfulCatch);
                if (decisionToCatch)
                {
                    _actionButton.Press();
                    currentMovementDirections = AI2DInput.Directions.None;
                    _movementInput.Move(currentMovementDirections);
                    hasActed = true;
                }
            }
        }

        private void ConsiderDodging(ref bool hasActed)
        {
            if (!hasActed && ShouldDodge)
            {
                var decisionToDodge = random.NextDouble() < ProbabilityOfDodge;
                if (decisionToDodge)
                {
                    _actionButton.Press();
                    currentMovementDirections = DodgeDirection();
                    _movementInput.Move(currentMovementDirections);
                    hasActed = true;
                }
            }
        }

        private void ConsiderRetrievingTheBall(ref bool hasActed)
        {
            if (!hasActed && (ShouldRetrieveBall || isRetrieving))
            {
                var decisionToRetrieveBall = random.NextDouble() < ProbabilityOfBallRetrieval;
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
        }

        private void ConsiderEvading(ref bool hasActed)
        {
            var decisionToEvade = random.NextDouble() < ProbabilityOfEvasion;
            if (!hasActed && !isWandering && (ShouldEvade && (decisionToEvade || isEvading)))
            {
                isEvading = true;
                currentMovementDirections = EvadeDirection();
                _movementInput.Move(currentMovementDirections);
                hasActed = true;
            }
            else
            {
                isEvading = false;
                timeEvading = 0;
            }
        }

        private void ConsiderWanderingAimlessly(ref bool hasActed)
        {
            var decisionToWander = random.NextDouble() < ProbabilityOfWandering;
            if (!hasActed && !isEvading && (ShouldWander && (decisionToWander || isWandering)))
            {
                isWandering = true;
                currentMovementDirections = WanderDirection();
                _movementInput.Move(currentMovementDirections);

                hasActed = true;
            }
            else
            {
                isWandering = false;
                timeWandering = 0;
            }
        }

        private void ConsiderTaunting(ref bool hasActed)
        {
            var decisionToTaunt = random.NextDouble() < ProbabilityOfTaunting;
            if (!hasActed && (ShouldTaunt && decisionToTaunt))
            {
                _tauntButton.Press();
                _tauntButton.Release();
                hasActed = true;
            }
        }
    }
}