﻿using System;
using Dodgeball.Entities;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region Constant Decision Probabilities
        //The % chance of an action, when appropriate, on each frame

        //Always get out of the way of the player throwing a ball
        private float probOfGettingOutOfTheWay = 1f;

        //Bypasses all below actions, used as difficulty modifier
        private float probOfInaction = 0.05f;

        private float probOfDodge = 0.015f;
        private float probOfBallRetrieval = 0.03f;
        private float probOfEvasion = 0.08f;
        private float probOfWandering = 0.005f;
        private float probOfTaunting = 0.003f;
        #endregion

        #region Decision checks
        private bool ShouldThrowBall => ball.CurrentOwnershipState == Ball.OwnershipState.Held && player.IsHoldingBall && ballHeldTime > timeToDelayThrow;

        private bool ShouldTaunt => ball.CurrentOwnershipState == Ball.OwnershipState.Held && ball.OwnerTeam == player.TeamIndex && ball.ThrowOwner != player;

        private bool ShouldWander => (ball.CurrentOwnershipState == Ball.OwnershipState.Held || ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) && 
                                    ball.OwnerTeam == player.TeamIndex;

        private bool ShouldEvade => (ball.CurrentOwnershipState == Ball.OwnershipState.Held || ball.CurrentOwnershipState == Ball.OwnershipState.Thrown) &&
                                    ball.OwnerTeam != player.TeamIndex;

        private bool ShouldDodge => (ball.CurrentOwnershipState == Ball.OwnershipState.Thrown &&
                                     ball.OwnerTeam != player.TeamIndex &&
                                    !player.IsDodging &&
                                     (ball.Position - player.Position).Length() < distanceToConsiderDodging &&
                                     ball.TrajectoryPolygon.CollideAgainst(player.CircleInstance));

        private bool ShouldRetrieveBall => ball.CurrentOwnershipState == Ball.OwnershipState.Free;

        private bool ShouldGetOutOfTheWayOfBallHolder => ball.CurrentOwnershipState == Ball.OwnershipState.Held &&
                                                         ball.OwnerTeam == player.TeamIndex &&
                                                         ((player.TeamIndex == 0 && player.X >= ball.ThrowOwner.X ||
                                                           player.TeamIndex == 1 && player.X <= ball.ThrowOwner.X)) &&
                        //Multiply the Y-difference by the X-difference to create a cone of avoidance
                        Math.Abs(player.Y - ball.ThrowOwner.Y) <= Math.Max(maxTolerableDistanceToBallHolder, maxTolerableDistanceToBallHolder * Math.Abs(player.X - ball.ThrowOwner.X)/125);

        #endregion

        private void MakeDecisions()
        {
            var hasActed = false;

            if (ShouldGetOutOfTheWayOfBallHolder)
            {
                var decisionToGetOutOfTheWay = random.NextDouble() <= probOfGettingOutOfTheWay;
                if (decisionToGetOutOfTheWay)
                {
                    //Remove the directions that got them here in the first place
                    wanderDirection = AI2DInput.Directions.None;

                    _movementInput.Move(RetrieveGetOutOfTheWayDirections());
                    hasActed = true;
                }
            }
            else
            {
                getOutOfTheWayDirections = AI2DInput.Directions.None;
            }

            if (ShouldThrowBall)
            {
                _aimingInput.Move(AimDirection());
                _movementInput.Move(AI2DInput.Directions.None);
                _actionButton.Press();
                _actionButton.Release();
                ballHeldTime = 0;
                hasActed = true;
            }

            if (!hasActed && !isWandering && !isEvading && !isRetrieving)
            {
                var decisionToDoNothing = random.NextDouble() < probOfInaction;
                hasActed = decisionToDoNothing;
            }

            if (!hasActed && ShouldDodge)
            {
                var decisionToDodge = random.NextDouble() < probOfDodge;
                if (decisionToDodge)
                {
                    _actionButton.Press();
                    hasActed = true;
                }
            } else if (_actionButton.IsDown)
            {
                _actionButton.Release();
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
                evasionDirection = DodgeDirection();
                _movementInput.Move(evasionDirection);
                hasActed = true;
            }
            else
            {
                isEvading = false;
                evasionDirection = AI2DInput.Directions.None;
                timeEvading = 0;
            }

            var decisionToWander = random.NextDouble() < probOfWandering;
            if (!hasActed && !isEvading && (ShouldWander && (decisionToWander || isWandering)))
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

            var decisionToTaunt = random.NextDouble() < probOfTaunting;
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