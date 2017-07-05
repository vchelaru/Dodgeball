using System;
using System.Linq;
using Dodgeball.Entities;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        private AI2DInput.Directions GetAimDirection()
        {
            var aimDirections = AI2DInput.Directions.None;

            var lowestHealth = otherTeamsPlayers.Where(p => !p.IsDying)?.Select(p => p.HealthPercentage).Concat(new[] {100f}).Min();

            if (!(lowestHealth < 100f)) return aimDirections;

            var weakPlayers = otherTeamsPlayers.Where(otp => otp.HealthPercentage <= lowestHealth).ToList();
            var targetPlayer = weakPlayers.Count() > 1 ? FlatRedBallServices.Random.In<Player>(weakPlayers) : weakPlayers.FirstOrDefault();

            if (targetPlayer == null) return aimDirections;
            
            var maxX = otherTeamsPlayers.Max(otp => otp.X);
            var minX = otherTeamsPlayers.Min(otp => otp.X);
            var maxY = otherTeamsPlayers.Max(otp => otp.Y);
            var minY = otherTeamsPlayers.Min(otp => otp.Y);

            var percentX = (targetPlayer.X - minX) / (maxX - minX);
            var percentY = (targetPlayer.Y - minY) / (maxY - minY);

            var leftRight = percentX > 0.5f ? AI2DInput.Directions.Right : percentX < 0.5f ? AI2DInput.Directions.Left : AI2DInput.Directions.None;
            var upDown = percentY > 0.5f ? AI2DInput.Directions.Up : percentX < 0.5f ? AI2DInput.Directions.Down : AI2DInput.Directions.None;

            aimDirections = leftRight | upDown;
            
            return aimDirections;
        }

        private AI2DInput.Directions GetWanderDirection()
        {
            var wanderDirections = AI2DInput.Directions.None;

            //Random directions
            var upDown = AI2DInput.Directions.None;
            var upDownRandom = random.NextDouble();
            if (upDownRandom > 0.5)
            {
                upDown = upDownRandom > 0.75
                    ? AI2DInput.Directions.Up
                    : AI2DInput.Directions.Down;
            }

            var leftRight = AI2DInput.Directions.None;
            var leftRightRandom = random.NextDouble();
            if (leftRightRandom > 0.5)
            {
                leftRight = leftRightRandom > (0.8 - (player.TeamIndex/10f))
                    ? AI2DInput.Directions.Left
                    : AI2DInput.Directions.Right;
            }
            wanderDirections = RemoveOffendingDirections(upDown | leftRight);

            return wanderDirections;
        }

        private AI2DInput.Directions GetEvadeDirection(Vector3 positionToEvade)
        {
            var evadeDirections = AI2DInput.Directions.None;

            var movement = positionToEvade - player.Position;
            movement.Normalize();

            var upDown = movement.Y < 0
                ? AI2DInput.Directions.Up
                : movement.Y > 0
                    ? AI2DInput.Directions.Down
                    : AI2DInput.Directions.None;
            var leftRight = movement.X > 0
                ? AI2DInput.Directions.Left
                : movement.X < 0
                    ? AI2DInput.Directions.Right
                    : AI2DInput.Directions.None;

            evadeDirections = RemoveOffendingDirections(upDown | leftRight);
            return evadeDirections;
        }

        private AI2DInput.Directions GetPersonalSpaceDirections(Vector3 positionToEvade)
        {
            var spaceDirections = AI2DInput.Directions.None;

            var movement = positionToEvade - player.Position;
            movement.Normalize();

            var upDown = movement.Y < 0
                ? AI2DInput.Directions.Up
                : movement.Y > 0
                    ? AI2DInput.Directions.Down
                    : AI2DInput.Directions.None;
            var leftRight = movement.X > 0
                ? AI2DInput.Directions.Left
                : movement.X < 0
                    ? AI2DInput.Directions.Right
                    : AI2DInput.Directions.None;

            spaceDirections = RemoveOffendingDirections(upDown | leftRight);

            return spaceDirections;
        }

        private AI2DInput.Directions GetDodgeDirection()
        {
            var dodgeDirections = AI2DInput.Directions.None;

            var movement = ball.Position - player.Position;
            movement.Normalize();

            var upDown = movement.Y < 0
                ? AI2DInput.Directions.Up
                : movement.Y > 0
                    ? AI2DInput.Directions.Down
                    : AI2DInput.Directions.None;

            var leftRight = movement.X > 0
                ? AI2DInput.Directions.Left
                : movement.X < 0
                    ? AI2DInput.Directions.Right
                    : AI2DInput.Directions.None;

            dodgeDirections = RemoveOffendingDirections(upDown | leftRight);
            return dodgeDirections;
        }

        private AI2DInput.Directions GetRetrieveBallDirection()
        {
            var leftRight = ball.Position.X > player.Position.X
                ? AI2DInput.Directions.Right
                : AI2DInput.Directions.Left;

            if (player.Position.X >= player.TeamRectangleRight && player.TeamIndex == 0) leftRight = AI2DInput.Directions.None;
            if (player.Position.X <= player.TeamRectangleLeft && player.TeamIndex == 1) leftRight = AI2DInput.Directions.None;

            var upDown = ball.Position.Y > player.Position.Y
                ? AI2DInput.Directions.Up
                : AI2DInput.Directions.Down;

            return upDown | leftRight;
        }

        private AI2DInput.Directions GetMoveOutOfTheWayOfBallHolderDirection()
        {
            var getOutOfWayDirections = AI2DInput.Directions.None;

            var upDown = AI2DInput.Directions.None;
            var leftRight = AI2DInput.Directions.None;
            if (player.Y > ball.ThrowOwner.Y)
            {
                upDown = player.Y < (player.TeamRectangleTop - player.CircleInstance.Radius) ? AI2DInput.Directions.Up : AI2DInput.Directions.Down;
            }
            else
            {
                upDown = player.Y > (player.TeamRectangleBottom + player.CircleInstance.Radius) ? AI2DInput.Directions.Down : AI2DInput.Directions.Up;
            }
            if (player.TeamIndex == 0)
            {
                leftRight = player.X > player.TeamRectangleLeft + player.CircleInstance.Radius ? AI2DInput.Directions.Left : AI2DInput.Directions.Right;
            }
            else
            {
                leftRight = player.X < player.TeamRectangleRight - player.CircleInstance.Radius ? AI2DInput.Directions.Right : AI2DInput.Directions.Left;
            }
            getOutOfWayDirections = upDown | leftRight;
            
            return getOutOfWayDirections;
        }

        private AI2DInput.Directions RemoveOffendingDirections(AI2DInput.Directions originalDirections)
        {
            var newDirections = originalDirections;
            //If player has gone out of bounds, remove the offending direction from the enum
            if (player.IsOnTop) newDirections &= ~(AI2DInput.Directions.Up);
            else if (player.IsOnBottom) newDirections &= ~(AI2DInput.Directions.Down);

            if ((player.TeamIndex == 0 && player.IsInFront) ||
                (player.TeamIndex == 1 && player.IsInBack)) newDirections &= ~(AI2DInput.Directions.Right);
            else if ((player.TeamIndex == 0 && player.IsInBack) ||
                    (player.TeamIndex == 1 && player.IsInFront)) newDirections &= ~(AI2DInput.Directions.Left);

            return newDirections;
        }

        private AI2DInput.Directions GetThrowPositioningDirection()
        {
            var positioningDirections = AI2DInput.Directions.None;

            positioningDirections = player.TeamIndex == 0
                    ? AI2DInput.Directions.Right
                    : AI2DInput.Directions.Left;
            var randomUpDown = random.NextDouble();
            if (randomUpDown > 0.5)
            {
                positioningDirections = positioningDirections |
                                            (randomUpDown > 0.75
                                                ? AI2DInput.Directions.Up
                                                : AI2DInput.Directions.Down);
            }
            
            return positioningDirections;
        }

        private Player GetClosestTeamPlayer()
        {
            var closestPlayer = myTeamOtherPlayers.FirstOrDefault();
            var closestPlayerDistance = float.MaxValue;

            foreach (var teamPlayer in myTeamOtherPlayers)
            {
                var teamPlayerDistance = (player.Position - teamPlayer.Position).Length();
                if (teamPlayerDistance < closestPlayerDistance)
                {
                    closestPlayer = teamPlayer;
                    closestPlayerDistance = teamPlayerDistance;
                }
            }

            return closestPlayer;
        }

        private bool BallIsInMyCourtOrTravelingTowardsIt()
        {
            var ballIsInMyCourt = false;
            var ballIsTravelingTowardsMyCourt = false;

            float halfwayX;

            //Left side of the screen
            if (player.TeamIndex == 0)
            {
                halfwayX = player.TeamRectangleRight;
                ballIsInMyCourt = ball.Position.X <= halfwayX;
            }
            else
            {
                halfwayX = player.TeamRectangleLeft;
                ballIsInMyCourt = ball.Position.X >= halfwayX;
            }

            if (ballIsInMyCourt == false)
            {
                //The closer it gets, the more likely, but still not as likely as if it was in my court
                var distanceOfBallToMiddle = ball.Position.X - halfwayX;
                var ballIsTravelingAway = Math.Sign(distanceOfBallToMiddle) == Math.Sign(ball.Velocity.X);

                ballIsTravelingTowardsMyCourt = !ballIsTravelingAway;
            }

            return ballIsInMyCourt || ballIsTravelingTowardsMyCourt;
        }


        private float GetBallLocationProbabilityModifier()
        {
            float halfwayX;
            bool ballIsInMyCourt;

            float toReturn = 1f;

            var onlyPlayerLeft = !myTeamOtherPlayers.Any(p => !p.IsDying);
            if (onlyPlayerLeft)
            {
                toReturn = 2f;
            }

            //Left side of the screen
            if (player.TeamIndex == 0)
            {
                halfwayX = player.TeamRectangleRight;
                ballIsInMyCourt = ball.Position.X <= halfwayX;
            }
            else
            {
                halfwayX =  player.TeamRectangleLeft;
                ballIsInMyCourt = ball.Position.X >= halfwayX;
            }

            if (ballIsInMyCourt == false)
            {
                //The closer it gets, the more likely, but still not as likely as if it was in my court
                var distanceOfBallToMiddle = ball.Position.X - halfwayX;
                var relativeDistanceToMiddle = distanceOfBallToMiddle / player.TeamRectangle.Width;
                var ballIsTravelingAway = Math.Sign(distanceOfBallToMiddle) == Math.Sign(ball.Velocity.X);

                toReturn = relativeDistanceToMiddle * (ballIsTravelingAway ? 0.25f : 1f);
            }

            return toReturn;
        }
    }
}