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
            //Use an existing wander direction to make movement more natural
            if (_timeWandering < _timeToWander && _wanderDirection != AI2DInput.Directions.None)
            {
                //If player has gone out of bounds, remove the offending direction from the enum
                _wanderDirection = RemoveOffendingDirections(_wanderDirection);

                return _wanderDirection;
            }
            else
            {
                //Mark the start of a new wander
                _timeWandering = 0;

                _timeToWander = MaxWanderTime * random.NextDouble();

                //Random directions
                var upDown = random.NextDouble() > 0.5
                    ? AI2DInput.Directions.Up
                    : AI2DInput.Directions.Down;

                var leftRight = random.NextDouble() > 0.5
                    ? AI2DInput.Directions.Left
                    : AI2DInput.Directions.Right;


                //TODO:  Measure size of arena rather than hard-code it
                if (_player.Y > 540) upDown = AI2DInput.Directions.Down;
                else if (_player.Y < -540) upDown = AI2DInput.Directions.Up;

                if (_player.X > 960) leftRight = AI2DInput.Directions.Left;
                else if (_player.Y < -960) leftRight = AI2DInput.Directions.Right;

                return upDown | leftRight;
            }
        }

        private AI2DInput.Directions DodgeDirection()
        {
            //Use an existing dodge direction to make movement more natural
            if (_timeDodging < _timeToDodge && _dodgeDirection != AI2DInput.Directions.None)
            {
                //If player has gone out of bounds, remove the offending direction from the enum
                _dodgeDirection = RemoveOffendingDirections(_dodgeDirection);

                return _dodgeDirection;
            }
            else
            {
                //Mark the start of a new dodge
                _timeDodging = 0;

                _timeToDodge = MaxDodgeTime * random.NextDouble();

                var movement = _ball.Position - _player.Position;
                movement.Normalize();

                var upDown = movement.Y > 0
                    ? AI2DInput.Directions.Up
                    : movement.Y < 0
                        ? AI2DInput.Directions.Down
                        : AI2DInput.Directions.None;
                var leftRight = movement.X > 0
                    ? AI2DInput.Directions.Left
                    : movement.X < 0
                        ? AI2DInput.Directions.Right
                        : AI2DInput.Directions.None;

                //TODO:  Measure size of arena rather than hard-code it
                if (_player.Y > 540 || _player.Y < -540) upDown = AI2DInput.Directions.None;
                if (_player.X > 960 || _player.X < -960) leftRight = AI2DInput.Directions.None;

                return upDown | leftRight;
            }
        }

        private AI2DInput.Directions RetrieveBallDirections()
        {
            var leftRight = _ball.Position.X > _player.Position.X
                ? AI2DInput.Directions.Right
                : AI2DInput.Directions.Left;

            if (_player.Position.X >= 0 && _player.TeamIndex == 0) leftRight = AI2DInput.Directions.None;
            if (_player.Position.X <= 0 && _player.TeamIndex == 1) leftRight = AI2DInput.Directions.None;

            var upDown = _ball.Position.Y > _player.Position.Y
                ? AI2DInput.Directions.Up
                : AI2DInput.Directions.Down;

            return upDown | leftRight;
        }

        private AI2DInput.Directions RemoveOffendingDirections(AI2DInput.Directions originalDirections)
        {
            var newDirections = originalDirections;
            //If player has gone out of bounds, remove the offending direction from the enum
            //TODO:  Measure size of arena rather than hard-code it
            if (_player.Y > 540) newDirections &= ~(AI2DInput.Directions.Up);
            else if (_player.Y < -540) newDirections &= ~(AI2DInput.Directions.Down);

            if (_player.X > 960) newDirections &= ~(AI2DInput.Directions.Right);
            else if (_player.X < -960) newDirections &= ~(AI2DInput.Directions.Left);

            //Don't let player cross center boundary
            if (_player.Position.X >= 0 && _player.TeamIndex == 0)
                newDirections &= ~(AI2DInput.Directions.Right);
            if (_player.Position.X <= 0 && _player.TeamIndex == 1)
                newDirections &= ~(AI2DInput.Directions.Left);

            return newDirections;
        }
    }
}