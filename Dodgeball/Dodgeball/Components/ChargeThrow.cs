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
        private float baseChargeRate = 1.5f;
        private float defaultChargeLevel = 10f;

        private float currentChargeRate => baseChargeRate * (1 + MeterPercent / 50);
        private int chargeDirection = 1;

        public float EffectiveChargePercent => MeterPercent / MaxValidThrowPercent;
        public bool FailedThrow => MeterPercent > MaxValidThrowPercent;

        public void ChargeActivity()
        {
            MeterPercent += currentChargeRate * chargeDirection;

            //Change direction at top/bottom of charge
            if (MeterPercent < 0 || MeterPercent > 100) chargeDirection *= -1;

            //Keep charge within bar
            MeterPercent = MathHelper.Clamp(MeterPercent, 0, 100);
        }

        public void Reset()
        {
            MeterPercent = defaultChargeLevel;
        }
    }
}
