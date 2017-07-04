using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Dodgeball.GumRuntimes
{
    partial class SuperThrowZoomRuntime
    {
        public void SetColors(Color shirtColor, Color shortsColor)
        {
            ShirtSpriteRed = shirtColor.R;
            ShirtSpriteGreen = shirtColor.G;
            ShirtSpriteBlue = shirtColor.B;

            ShortsSpriteRed = shortsColor.R;
            ShortsSpriteGreen = shortsColor.G;
            ShortsSpriteBlue = shortsColor.B;
        }
    }
}
