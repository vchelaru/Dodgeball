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
using Dodgeball.Components;

namespace Dodgeball.Screens
{
    public partial class TitleScreen
    {
        enum MenuItem { Play = 0,  Exit }
        private MenuItem HighlightedSelection = MenuItem.Play;

        OptionsSelectionLogic selectionLogic;

        void CustomInitialize()
        {
            selectionLogic = new OptionsSelectionLogic(PlayButtonInstance, ExitButtonInstance);

        }

        void CustomActivity(bool firstTimeCalled)
        {
            selectionLogic.Activity();

            if(selectionLogic.DidPushAButton)
            {
                if (selectionLogic.SelectedOption == (int)MenuItem.Play)
                {
                    MoveToScreen(typeof(CharacterSelectScreen));
                }
                else if (selectionLogic.SelectedOption == (int)MenuItem.Exit)
                {
                    FlatRedBallServices.Game.Exit();
                }
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
