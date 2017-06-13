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

        public float EffectiveChargePercent => MeterPercent / MaxValidThrowPercent;
        public bool FailedThrow => MeterPercent > MaxValidThrowPercent;

        public void ChargeActivity()
        {
            MeterPercent += GetCurrentChargeRate() * TimeManager.SecondDifference;

            //Reset at top of charge
            if (MeterPercent > 100) MeterPercent = 0;

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
