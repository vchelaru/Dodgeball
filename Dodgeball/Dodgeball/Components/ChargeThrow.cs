using FlatRedBall;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodgeball.Components
{


    public class ChargeThrow
    {
        private const float MaxValidThrowPercent = 80;
        public float MeterPercent { get; set; }
        private float defaultChargeLevel = 10f;

        public float LowEndChargeRate { get; set; }
        public float HighEndChargeRate { get; set; }

        private int chargeDirection = 1;

        public float EffectiveChargePercent => MeterPercent / MaxValidThrowPercent;
        public bool FailedThrow => MeterPercent > MaxValidThrowPercent;

        public void ChargeActivity()
        {
            MeterPercent += GetCurrentChargeRate() * chargeDirection * TimeManager.SecondDifference;

            //Change direction at top/bottom of charge
            if (MeterPercent < 0 || MeterPercent > 100) chargeDirection *= -1;

            //Keep charge within bar
            MeterPercent = MathHelper.Clamp(MeterPercent, 0, 100);
        }

        private float GetCurrentChargeRate()
        {
            float normalizedMeterPercentage = MeterPercent / 100.0f;

            return MathHelper.Lerp(LowEndChargeRate, HighEndChargeRate, normalizedMeterPercentage);
        }

        public void Reset()
        {
            MeterPercent = defaultChargeLevel;
        }
    }
}
