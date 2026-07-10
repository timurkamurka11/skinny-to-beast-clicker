using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Player
{
    [Serializable]
    public class BodyStage
    {
        public string stageName;
        public double requiredStrength;

        public BodyStage(string stageName, double requiredStrength)
        {
            this.stageName = stageName;
            this.requiredStrength = requiredStrength;
        }
    }

    public class PlayerStats : MonoBehaviour
    {
        [Header("Runtime Stats")]
        [SerializeField] private double coins;
        [SerializeField] private double strength;
        [SerializeField] private double totalReps;
        [SerializeField] private int bodyStageIndex;

        [Header("Body Stages")]
        [SerializeField] private List<BodyStage> bodyStages = new List<BodyStage>();

        public event Action StatsChanged;

        public double Coins => coins;
        public double Strength => strength;
        public double TotalReps => totalReps;
        public int BodyStageIndex => bodyStageIndex;
        public string BodyStageName => bodyStages.Count == 0 ? "Skinny" : bodyStages[bodyStageIndex].stageName;

        private void Awake()
        {
            EnsureDefaultStages();
            UpdateBodyStage();
        }

        public void AddTraining(double reps, double strengthGain, double coinGain)
        {
            totalReps += Math.Max(0, reps);
            strength += Math.Max(0, strengthGain);
            coins += Math.Max(0, coinGain);

            UpdateBodyStage();
            StatsChanged?.Invoke();
        }

        public void AddCoins(double amount)
        {
            coins += Math.Max(0, amount);
            StatsChanged?.Invoke();
        }

        public bool CanSpend(double cost)
        {
            return coins >= cost;
        }

        public bool TrySpend(double cost)
        {
            if (!CanSpend(cost))
            {
                return false;
            }

            coins -= cost;
            StatsChanged?.Invoke();
            return true;
        }

        private void UpdateBodyStage()
        {
            if (bodyStages.Count == 0)
            {
                bodyStageIndex = 0;
                return;
            }

            for (int i = bodyStages.Count - 1; i >= 0; i--)
            {
                if (strength >= bodyStages[i].requiredStrength)
                {
                    bodyStageIndex = i;
                    return;
                }
            }

            bodyStageIndex = 0;
        }

        private void EnsureDefaultStages()
        {
            if (bodyStages.Count > 0)
            {
                return;
            }

            bodyStages.Add(new BodyStage("Skinny", 0));
            bodyStages.Add(new BodyStage("Beginner", 50));
            bodyStages.Add(new BodyStage("Fit", 250));
            bodyStages.Add(new BodyStage("Athletic", 1000));
            bodyStages.Add(new BodyStage("Big", 5000));
            bodyStages.Add(new BodyStage("Beast", 25000));
            bodyStages.Add(new BodyStage("Gym Legend", 100000));
        }
    }
}
