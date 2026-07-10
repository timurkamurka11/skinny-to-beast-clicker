using SkinnyToBeast.Player;
using UnityEngine;

namespace SkinnyToBeast.Training
{
    public class TapTrainingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Tap Balance")]
        [SerializeField] private double repsPerTap = 1;
        [SerializeField] private double strengthPerRep = 1;
        [SerializeField] private double coinsPerRep = 0.25;

        private double tapPowerMultiplier = 1;
        private double coinMultiplier = 1;

        public double RepsPerTap => repsPerTap;
        public double StrengthPerRep => strengthPerRep;
        public double CoinsPerRep => coinsPerRep;
        public double TapPowerMultiplier => tapPowerMultiplier;
        public double CoinMultiplier => coinMultiplier;

        public void SetPlayerStats(PlayerStats stats)
        {
            playerStats = stats;
        }

        public void TrainTap()
        {
            if (playerStats == null)
            {
                Debug.LogWarning("TapTrainingController has no PlayerStats reference.");
                return;
            }

            double reps = repsPerTap * tapPowerMultiplier;
            double strengthGain = reps * strengthPerRep;
            double coinGain = reps * coinsPerRep * coinMultiplier;

            playerStats.AddTraining(reps, strengthGain, coinGain);
        }

        public void AddTapPowerMultiplier(double amount)
        {
            tapPowerMultiplier += amount;
        }

        public void AddCoinMultiplier(double amount)
        {
            coinMultiplier += amount;
        }
    }
}
