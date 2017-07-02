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
        private readonly float _probOfDodge = 0.03f;
        private readonly float _probOfEvasion = 0.1f;
        private readonly float _probOfWandering = 0.05f;
        private readonly float _probOfTaunting = 0.003f;
        private readonly float _probOfFailedCatch = 0.02f;
        private readonly float _probOfSuccesfulCatch = 0.02f;

        private readonly float _probOfOptimalThrow = 0.04f;

        //These are probabilities that aren't directly referenced by logic, but are altered then used as a dynamic probability
        private readonly float _baseProbOfSuboptimalThrow = 0.1f;
        private readonly float _baseProbOfBallRetrieval = 0.15f;
        #endregion

        #region Dynamic Probabilities
        //The base chance of an action, altered by conditions
        private float _probOfNonOptimalThrow => _baseProbOfSuboptimalThrow * CurrentThrowCharge/100;
        private float _probOfBallRetrieval => _baseProbOfBallRetrieval * GetBallLocationProbabilityModifier();

        #endregion

        #region Difficulty-adjusted Probabilities
        //Final probability after conditions and difficulty modification have been considered
        private float ProbabilityOfDodge => _probOfDodge * IncreaseWithDifficulty;
        private float ProbabilityOfBallRetrieval => _probOfBallRetrieval * IncreaseWithDifficulty;
        private float ProbabilityOfEvasion => _probOfEvasion * IncreaseWithDifficulty;
        private float ProbabilityOfWandering => _probOfWandering * IncreaseWithDifficulty;
        private float ProbabilityOfTaunting => _probOfTaunting * (DecreaseWithDifficulty / 2);
        private float ProbabilityOfOptimalThrow => _probOfOptimalThrow * IncreaseWithDifficulty;
        private float ProbabilityOfSuboptimalThrow => _probOfNonOptimalThrow * DecreaseWithDifficulty;
        private float ProbabilityOfFailedCatch => _probOfFailedCatch * DecreaseWithDifficulty;
        private float ProbabilityOfSuccessfulCatch => _probOfSuccesfulCatch * IncreaseWithDifficulty;

        #endregion

        #region Reference Properties
        //Convenient properties that shorten references
        private float CurrentThrowCharge => player.chargeThrowComponent.MeterPercent;

        #endregion

        #region Decision checks

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

        private bool ShouldFindPersonalSpace => !player.IsHoldingBall && teamPlayers.Exists(
            tp => (Math.Abs(tp.X - player.X) <= myPersonalSpace &&
                  Math.Abs(tp.Y - player.Y) <= myPersonalSpace));
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

            ConsiderGettingOutOfTheWayOfBallHolder(ref hasActed);

            ConsiderThrowingTheBall(ref hasActed);

            ConsiderPositioningToThrow(ref hasActed);

            ConsiderCatching(ref hasActed);

            ConsiderDodging(ref hasActed);

            ConsiderRetrievingTheBall(ref hasActed);

            ConsiderFindingPersonalSpace(ref hasActed);

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

        private void ConsiderGettingOutOfTheWayOfBallHolder(ref bool hasActed)
        {
            if (!hasActed && (ShouldGetOutOfTheWayOfBallHolder || isGettingOutOfTheWayOfBallHolder))
            {
                CurrentAction = "GettingOutOfTheWay";
                isGettingOutOfTheWayOfBallHolder = true;
                _movementInput.Move(GetMoveOutOfTheWayOfBallHolderDirection());
                hasActed = true;
            }
            else
            {
                isGettingOutOfTheWayOfBallHolder = false;
            }
        }

        private void ConsiderFindingPersonalSpace(ref bool hasActed)
        {
            var isAlreadyDoingSomething = isRetrieving || isEvading ||
                                          isGettingOutOfTheWayOfBallHolder || isPositioningForThrow ||
                                          isWandering || IsChargingThrow;

            if (!hasActed && !isAlreadyDoingSomething && (ShouldFindPersonalSpace || isFindingPersonalSpace))
            {
                var closestPlayer = GetClosestTeamPlayer();
                if (closestPlayer != null)
                {
                    var personalSpaceDirections = GetPersonalSpaceDirections(closestPlayer.Position);
                    if (personalSpaceDirections != AI2DInput.Directions.None)
                    {
                        CurrentAction = "FindingPersonalSpace";
                        isFindingPersonalSpace = true;
                        currentMovementDirections = personalSpaceDirections;
                        _movementInput.Move(currentMovementDirections);
                        hasActed = true;
                    }
                    else
                    {
                        isFindingPersonalSpace = false;
                    }
                }
                else
                {
                    isFindingPersonalSpace = false;
                }
            }
            else
            {
                isFindingPersonalSpace = false;
            }
        }

        private void ConsiderPositioningToThrow(ref bool hasActed)
        {
            var isAlreadyDoingSomething = isRetrieving || isEvading ||
                                          isGettingOutOfTheWayOfBallHolder || 
                                          isWandering || IsChargingThrow || isFindingPersonalSpace;

            if (!hasActed && !isAlreadyDoingSomething && (ShouldPositionForThrow || isPositioningForThrow))
            {
                CurrentAction = "PositioningForThrow";
                isPositioningForThrow = true;
                _movementInput.Move(GetThrowPositioningDirection());
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
                CurrentAction = "Throwing";
                isPositioningForThrow = false;
                _aimingInput.Move(GetAimDirection());
                _movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                ballHeldTime = 0;
                hasActed = true;
            }
            else if (IsChargingThrow)
            {
                CurrentAction = "ChargingThrow";
                var chanceOfThrow = random.NextDouble();
                var decisionToRelease = (!ThrowChargeIsOptimal && (chanceOfThrow < ProbabilityOfSuboptimalThrow)) ||
                                        (ThrowChargeIsOptimal && chanceOfThrow < ProbabilityOfOptimalThrow);
                if (decisionToRelease) _actionButton.Release();
                hasActed = true;
            }
        }

        private void ConsiderCatching(ref bool hasActed)
        {
            if (!hasActed && ShouldCatch)
            {
                var chanceOfCatch = random.NextDouble();
                var decisionToCatch = (!CatchAttemptIsOptimal && chanceOfCatch < ProbabilityOfFailedCatch) ||
                                      (CatchAttemptIsOptimal && chanceOfCatch < ProbabilityOfSuccessfulCatch);
                if (decisionToCatch)
                {
                    CurrentAction = "Catching";
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
                    CurrentAction = "Dodging";
                    _actionButton.Press();
                    currentMovementDirections = GetDodgeDirection();
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
                    CurrentAction = "Retrieving";
                    _movementInput.Move(GetRetrieveBallDirection());
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
            var isAlreadyDoingSomething = isRetrieving || 
                                          isGettingOutOfTheWayOfBallHolder ||
                                          isWandering || IsChargingThrow || isFindingPersonalSpace;

            var decisionToEvade = random.NextDouble() < ProbabilityOfEvasion;
            if (!hasActed && !isAlreadyDoingSomething && (ShouldEvade && (decisionToEvade || isEvading)))
            {
                CurrentAction = "Evading";
                isEvading = true;
                currentMovementDirections = GetEvadeDirection(ball.Position);
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
            var isAlreadyDoingSomething = isRetrieving || isEvading || 
                                          isGettingOutOfTheWayOfBallHolder ||
                                          IsChargingThrow || isFindingPersonalSpace;

            var decisionToWander = random.NextDouble() < ProbabilityOfWandering;
            if (!hasActed && !isAlreadyDoingSomething && (ShouldWander && (decisionToWander || isWandering)))
            {
                CurrentAction = "Wandering";
                isWandering = true;
                currentMovementDirections = GetWanderDirection();
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
            var isAlreadyDoingSomething = isRetrieving || isEvading || isWandering ||
                                          isGettingOutOfTheWayOfBallHolder ||
                                          IsChargingThrow || isFindingPersonalSpace;

            var decisionToTaunt = random.NextDouble() < ProbabilityOfTaunting;
            if (!hasActed && !isAlreadyDoingSomething && (ShouldTaunt && decisionToTaunt))
            {
                CurrentAction = "Taunting";
                _tauntButton.Press();
                _tauntButton.Release();
                hasActed = true;
            }
        }
    }
}