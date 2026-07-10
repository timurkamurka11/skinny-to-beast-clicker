using System;

namespace SkinnyToBeast.Economy
{
    [Serializable]
    public class UpgradeData
    {
        public string id;
        public string displayName;
        public double baseCost;
        public double costMultiplier = 1.15;
        public double tapPowerBonus;
        public double coinMultiplierBonus;
        public double autoRepsPerSecondBonus;
        public int level;

        public double CurrentCost
        {
            get
            {
                return baseCost * Math.Pow(costMultiplier, level);
            }
        }
    }
}
