using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Dodgeball.GumRuntimes
{
    partial class ThrowChargeMeterRuntime
    {

        partial void CustomInitialize()
        {
            this.MeterPercentChanged +=
                (not, used) => UpdateIndicatorColor();
        }
        
        private void UpdateIndicatorColor()
        {
            if (MeterPercent > 80)
            {
                SlidingIndicatorRectangle.Red = 255;
                SlidingIndicatorRectangle.Green = 0;
            }
            else
            {
                SlidingIndicatorRectangle.Green = 255;
                SlidingIndicatorRectangle.Red = (int)(255 * (1-(MeterPercent / 80)));
            }
        }


    }
}
