using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;



namespace Dodgeball.Screens
{
	public partial class CharacterSelectScreen
	{

		void CustomInitialize()
		{
            InitializeInput();

        }
        private void InitializeInput()
        {
           SelectionMarkerInstance0.InitializeXbox360Controls(InputManager.Xbox360GamePads[0]);
        }

        void CustomActivity(bool firstTimeCalled)
		{
            TeamSelectionBoxesInstance.Circle0X = SelectionMarkerInstance0.X;
            if (SelectionMarkerInstance0.isSelected)
            {
                TeamSelectionBoxesInstance.SelectedVisible = true;
                TeamSelectionBoxesInstance.SelectedX = SelectionMarkerInstance0.X;
                TeamSelectionBoxesInstance.SelectedY = SelectionMarkerInstance0.Y;
            }
                          
        }

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
