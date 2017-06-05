using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using Dodgeball.Entities;
using Dodgeball.Screens;
namespace Dodgeball.Entities
{
	public partial class TargetingLine
	{
        void OnAfterVisibleSet (object sender, EventArgs e)
        {
            this.PolygonInstance.Visible = this.Visible;
        }
		
	}
}
