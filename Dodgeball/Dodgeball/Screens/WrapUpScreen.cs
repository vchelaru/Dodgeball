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
using Dodgeball.DataRuntime;

namespace Dodgeball.Screens
{
	public partial class WrapUpScreen
	{

		void CustomInitialize()
		{
            TextInstance.Text = $"Team {GameStats.WinningTeam0Based + 1} Wins!";
		    this.Call(() =>
		        {
		            FlatRedBall.Audio.AudioManager.PlaySong(GlobalContent.dodgeball_end_sting, true, true);
                }
		    ).After(.25);
            
		}

		void CustomActivity(bool firstTimeCalled)
		{


		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
