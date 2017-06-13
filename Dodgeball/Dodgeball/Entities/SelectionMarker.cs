using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;

namespace Dodgeball.Entities
{
	public partial class SelectionMarker
	{
        IPressableInput RightPress { get; set; }
        IPressableInput LeftPress { get; set; }
        IPressableInput ActionButton { get; set; }
        enum MarkerPosition {Left, Center, Right};
        public bool isSelected = false;

        MarkerPosition CurrentPosition = MarkerPosition.Center;
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
		private void CustomInitialize()
		{
            this.CurrentPosition = MarkerPosition.Center;
        }
        public void InitializeXbox360Controls(Xbox360GamePad gamePad)
        {
            RightPress = InputManager.Xbox360GamePads[0].GetButton(Xbox360GamePad.Button.DPadRight);
            LeftPress = InputManager.Xbox360GamePads[0].GetButton(Xbox360GamePad.Button.DPadLeft);


            ActionButton = gamePad.GetButton(Xbox360GamePad.Button.RightShoulder);
            ActionButton = gamePad.GetButton(Xbox360GamePad.Button.A);
        }

        private void CustomActivity()
		{
            if(RightPress.WasJustReleased)
            {
                if(CurrentPosition == MarkerPosition.Left)
                {
                    CurrentPosition = MarkerPosition.Center;
                }
                else if(CurrentPosition == MarkerPosition.Center)
                {
                    CurrentPosition = MarkerPosition.Right;
                }
            }
            if (LeftPress.WasJustReleased)
            {
                if (CurrentPosition == MarkerPosition.Right)
                {
                    CurrentPosition = MarkerPosition.Center;
                }
                else if (CurrentPosition == MarkerPosition.Center)
                {
                    CurrentPosition = MarkerPosition.Left;
                }
            }

            switch(CurrentPosition)
            {
                case MarkerPosition.Left: X = -500; break;
                case MarkerPosition.Center: X = 0; break;
                case MarkerPosition.Right: X = 500; break;
            }


            if (ActionButton.WasJustReleased)
            {
                if (this.CurrentPosition != MarkerPosition.Center)
                {
                    if (isSelected)
                    {
                        isSelected = false;
                    }
                    else
                    {
                        isSelected = true;
                    }
                }
            }

        }


		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
	}
}
