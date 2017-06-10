using System;
using FlatRedBall.Input;

namespace Dodgeball.AI
{
    public partial class AIController
    {
        #region IPressableInput
        private class AIPressableInput : IPressableInput
        {
            #region Interface Properties
            public bool IsDown { get; private set; }
            public bool WasJustPressed { get; private set; }
            public bool WasJustReleased { get; private set; }
            #endregion

            #region Constructor
            public AIPressableInput()
            {
                IsDown = false;
                WasJustPressed = false;
                WasJustReleased = false;
            }
            #endregion

            public void Press()
            {
                WasJustPressed = !IsDown;
                IsDown = true;
            }

            public void Release()
            {
                WasJustReleased = IsDown;
                IsDown = false;
                WasJustPressed = false;
            }

            public void Reset()
            {
                WasJustReleased = false;
                IsDown = false;
                WasJustPressed = false;
            }
        }
        #endregion

        #region I2DInput
        private class AI2DInput : I2DInput
        {
            #region Interface Properties
            public float X { get; private set; }
            public float Y { get; private set; }
            public float XVelocity { get; private set; }
            public float YVelocity { get; private set; }
            public float Magnitude { get; private set; }
            #endregion

            #region Constructor
            public AI2DInput()
            {
                X = 0;
                XVelocity = 0;
                Y = 0;
                YVelocity = 0;
                Magnitude = 0;
            }
            #endregion

            [Flags]
            public enum Directions
            {
                None = 0,
                Up = 1,
                Down = 2,
                Left = 4,
                Right = 8
            }

            public void Move(Directions directions)
            {
                if (directions == Directions.None)
                {
                    X = 0;
                    XVelocity = 0;
                    Y = 0;
                    YVelocity = 0;
                    Magnitude = 0;
                }
                else
                {
                    var moveUp = (directions & Directions.Up) == Directions.Up;
                    var moveDown = !moveUp && (directions & Directions.Down) == Directions.Down;
                    var moveLeft = (directions & Directions.Left) == Directions.Left;
                    var moveRight = !moveLeft && (directions & Directions.Right) == Directions.Right;

                    X = moveRight ? 1 : moveLeft ? -1 : 0;
                    Y = moveUp ? 1 : moveDown ? -1 : 0;
                    Magnitude = (Math.Abs(X) + Math.Abs(Y)) / 2;
                }
            }
        }
        #endregion
    }
}