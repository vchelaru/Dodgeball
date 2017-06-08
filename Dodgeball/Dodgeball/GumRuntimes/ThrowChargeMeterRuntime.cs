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
        #region Fields/Properties
        private const float MaxValidThrowPercent = 80;
        private float baseChargeRate = 1.5f;
        private float defaultChargeLevel = 40f;

        private float currentChargeRate => baseChargeRate * (1 + MeterPercent / 50);
        private int chargeDirection = 1;

        public float EffectiveChargePercent => MeterPercent / MaxValidThrowPercent;
        public bool FailedThrow => MeterPercent > MaxValidThrowPercent;
        #endregion

        public void ChargeActivity()
        {
            MeterPercent += currentChargeRate * chargeDirection;

            //Change direction at top/bottom of charge
            if (MeterPercent < 0 || MeterPercent > 100) chargeDirection *= -1;

            //Keep charge within bar
            MeterPercent = MathHelper.Clamp(MeterPercent, 0, 100);

            UpdateIndicatorColor();
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

        public void Reset()
        {
            MeterPercent = defaultChargeLevel;
        }
    }
}
