﻿using System;
using Dodgeball.Components;
using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region Constant Decision Probabilities

        //The % chance of an action, when appropriate, on each frame

        //Bypasses all below actions, used as difficulty modifier
        private float probOfInaction = 0.05f;

        private float probOfDodge = 0.015f;
        private float probOfBallRetrieval = 0.03f;
        private float probOfEvasion = 0.08f;
        private float probOfWandering = 0.005f;
        private float probOfTaunting = 0.003f;

        private float probOfOptimalThrow = 0.015f;

        //These are probabilities that aren't directly referenced by logic, but are altered then used as a dynamic probability
        private float baseProbOfNonOptimalThrow = 0.03f;
        #endregion

        #region Dynamic Decision Probabilities
        //The base % chance of an action, altered by conditions

        private float probOfNonOptimalThrow => baseProbOfNonOptimalThrow * CurrentThrowCharge/100;

        #endregion

        #region Reference Properties
        //Convenient properties that shorten references
        private float CurrentThrowCharge => player.chargeThrowComponent.MeterPercent;

        #endregion

        #region Decision checks

        private bool ShouldPositionForThrow => ball.ThrowOwner == player && !player.IsInFront && !IsChargingThrow;

        private bool ShouldThrowBall => player.IsHoldingBall && ballHeldTime > timeToDelayThrow && !IsChargingThrow;

        private bool IsChargingThrow => player.IsHoldingBall && _actionButton.IsDown;

        private bool ThrowChargeIsOptimal => CurrentThrowCharge > ChargeThrow.MaxValidThrowPercent * 0.8f &&
                                             CurrentThrowCharge <= ChargeThrow.MaxValidThrowPercent;

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
                                     (ball.Position - player.Position).Length() < distanceToConsiderDodging &&
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

        private void MakeDecisions()
        {
            //release action button from previous dodge
            if (!player.IsHoldingBall && _actionButton.IsDown)
            {
                _actionButton.Release();
            }

            var hasActed = false;

            if (ShouldGetOutOfTheWayOfBallHolder)
            {
                isGettingOutOfTheWay = true;
                _movementInput.Move(RetrieveGetOutOfTheWayDirections());
                hasActed = true;
            }
            else
            {
                isGettingOutOfTheWay = false;
            }

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
                var decisionToRelease = (!ThrowChargeIsOptimal && (chanceOfThrow < probOfNonOptimalThrow)) ||
                                        (ThrowChargeIsOptimal && chanceOfThrow < probOfOptimalThrow);
                if (decisionToRelease) _actionButton.Release();
                hasActed = true;
            }

            if (!hasActed && !isWandering && !isEvading && !isRetrieving)
            {
                var decisionToDoNothing = random.NextDouble() < probOfInaction;
                hasActed = decisionToDoNothing;
                currentMovementDirections = AI2DInput.Directions.None;
            }

            if (!hasActed && ShouldDodge)
            {
                var decisionToDodge = random.NextDouble() < probOfDodge;
                if (decisionToDodge)
                {
                    _actionButton.Press();
                    hasActed = true;
                }
            }

            if (!hasActed && (ShouldRetrieveBall || isRetrieving))
            {
                var decisionToRetrieveBall = random.NextDouble() < probOfBallRetrieval;
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

            var decisionToEvade = random.NextDouble() < probOfEvasion;
            if (!hasActed && !isWandering && (ShouldEvade && (decisionToEvade || isEvading)))
            {
                isEvading = true;
                currentMovementDirections = DodgeDirection();
                _movementInput.Move(currentMovementDirections);
                hasActed = true;
            }
            else
            {
                isEvading = false;
                timeEvading = 0;
            }

            var decisionToWander = random.NextDouble() < probOfWandering;
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

            var decisionToTaunt = random.NextDouble() < probOfTaunting;
            if (!hasActed && (ShouldTaunt && decisionToTaunt))
            {
                _tauntButton.Press();
                _tauntButton.Release();
                hasActed = true;
            }

            if (!hasActed)
            {
                currentMovementDirections = AI2DInput.Directions.None;
                _movementInput.Move(AI2DInput.Directions.None);
                _aimingInput.Move(AI2DInput.Directions.None);
            }
        }
    }
}