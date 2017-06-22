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
using Gum.Wireframe;

namespace Dodgeball.Screens
{
	public partial class TitleScreen
	{
        enum MenuItem { Play, Settings, Exit }
        private MenuItem HighlightedSelection = MenuItem.Play;

        void CustomInitialize()
		{


		}

		void CustomActivity(bool firstTimeCalled)
		{
            foreach (Xbox360GamePad gamePad in InputManager.Xbox360GamePads)
            {
                HandleGamePadInput(gamePad.LeftStick.UpAsButton, gamePad.LeftStick.DownAsButton, ref HighlightedSelection);

                if (gamePad.ButtonPushed(Xbox360GamePad.Button.A))
                {
                    if(HighlightedSelection == MenuItem.Play)
                    {
                        MoveToScreen(typeof(CharacterSelectScreen));
                    }
                    if(HighlightedSelection == MenuItem.Settings)
                    {
                        //Settings paghe
                    }
                    if(HighlightedSelection == MenuItem.Exit)
                    {
                        //Exit code here
                    }
                }
            }

        }

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }
        void HandleGamePadInput(IPressableInput upPress, IPressableInput downPress, ref MenuItem highlightedSelection)
        {
            if (upPress.WasJustReleased)
            {
                if (highlightedSelection == MenuItem.Settings)
                {
                    highlightedSelection = MenuItem.Play;
                }
                else if (highlightedSelection == MenuItem.Exit)
                {
                    highlightedSelection = MenuItem.Settings;
                }
            }
            if (downPress.WasJustReleased)
            {
                if (highlightedSelection == MenuItem.Play)
                {
                    highlightedSelection = MenuItem.Settings;
                }
                else if (highlightedSelection == MenuItem.Settings)
                {
                    highlightedSelection = MenuItem.Exit;
                }

            }
            if (highlightedSelection == MenuItem.Play)
            {
                //ui highlight code here
            }
            if (highlightedSelection == MenuItem.Settings)
            {
                //ui highlight code here

            }
            if (highlightedSelection == MenuItem.Exit)
            {
                //ui highlight code here

            }
        }

    }
}
