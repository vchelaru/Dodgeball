namespace Dodgeball.AI
{
    public partial class AIController
    {
        private AI2DInput.Directions AimDirection()
        {
            //TODO:  Aim at particular player
            return AI2DInput.Directions.Down;
        }

        private AI2DInput.Directions WanderDirection()
        {
            if (timeWandering >= timeToWander)
            {
                isWandering = false;
                wanderDirection = AI2DInput.Directions.None;
            }
            //Use an existing wander direction to make movement more natural
            else if (wanderDirection != AI2DInput.Directions.None)
            {
                //If player has gone out of bounds, start a new direction
                var currentDirection = wanderDirection;
                wanderDirection = RemoveOffendingDirections(wanderDirection);

                if (wanderDirection != currentDirection) wanderDirection = AI2DInput.Directions.None;
            }

            if (isWandering && wanderDirection == AI2DInput.Directions.None)
            {
                //Mark the start of a new wander
                timeWandering = 0;

                timeToWander = MaxWanderTime * random.NextDouble();

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
                wanderDirection = RemoveOffendingDirections(upDown | leftRight);
            }
            return wanderDirection;
        }

        private AI2DInput.Directions DodgeDirection()
        {
            if (timeEvading >= timeToEvade)
            {
                isEvading = false;
                evasionDirection = AI2DInput.Directions.None;
            }
            //Use an existing dodge direction to make movement more natural
            else if (timeEvading < timeToEvade && evasionDirection != AI2DInput.Directions.None)
            {
                //If player has gone out of bounds, start a new direction
                var currentDirection = evasionDirection;
                evasionDirection = RemoveOffendingDirections(evasionDirection);

                if (evasionDirection != currentDirection) evasionDirection = AI2DInput.Directions.None;
            }

            if (isEvading && evasionDirection == AI2DInput.Directions.None)
            {
                //Mark the start of a new dodge
                timeEvading = 0;

                timeToEvade = MaxEvasionTime * random.NextDouble();

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

                evasionDirection = RemoveOffendingDirections(upDown | leftRight);
            }
            return evasionDirection;
        }

        private AI2DInput.Directions RetrieveBallDirections()
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
    }
}