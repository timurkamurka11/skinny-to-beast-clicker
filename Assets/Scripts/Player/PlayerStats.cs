using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string CoinsKey = "game.player.coins";
        private const string StrengthKey = "game.player.strength";
        private const string RepsKey = "game.player.reps";
        private const float SaveDelay = 1.25f;

        [Header("Runtime Stats")]
        [SerializeField] private double coins;
        [SerializeField] private double strength;
        [SerializeField] private double totalReps;
        [SerializeField] private int bodyStageIndex;

        [Header("Body Stages")]
        [SerializeField] private List<BodyStage> bodyStages = new List<BodyStage>();

        private bool savePending;
        private float saveAt;

        public event Action StatsChanged;

        public double Coins => coins;
        public double Strength => strength;
        public double TotalReps => totalReps;
        public int BodyStageIndex => bodyStageIndex;
        public int BodyStageCount => bodyStages.Count;
        public string BodyStageName => bodyStages.Count == 0 ? "Skinny" : bodyStages[bodyStageIndex].stageName;
        public string NextBodyStageName =>
            bodyStages.Count == 0 || bodyStageIndex >= bodyStages.Count - 1
                ? BodyStageName
                : bodyStages[bodyStageIndex + 1].stageName;
        public double NextBodyStageStrength =>
            bodyStages.Count == 0 || bodyStageIndex >= bodyStages.Count - 1
                ? strength
                : bodyStages[bodyStageIndex + 1].requiredStrength;
        public float BodyStageProgress
        {
            get
            {
                if (bodyStages.Count == 0 || bodyStageIndex >= bodyStages.Count - 1)
                {
                    return 1f;
                }

                double currentRequirement = bodyStages[bodyStageIndex].requiredStrength;
                double nextRequirement = bodyStages[bodyStageIndex + 1].requiredStrength;
                double range = Math.Max(1d, nextRequirement - currentRequirement);
                return Mathf.Clamp01((float)((strength - currentRequirement) / range));
            }
        }

        private void Awake()
        {
            EnsureDefaultStages();
            LoadProgress();
            UpdateBodyStage();
        }

        private void Update()
        {
            if (savePending && Time.unscaledTime >= saveAt)
            {
                SaveNow();
            }
        }

        public void AddTraining(double reps, double strengthGain, double coinGain)
        {
            totalReps += Math.Max(0, reps);
            strength += Math.Max(0, strengthGain);
            coins += Math.Max(0, coinGain);

            UpdateBodyStage();
            QueueSave();
            StatsChanged?.Invoke();
        }

        public void AddCoins(double amount)
        {
            coins += Math.Max(0, amount);
            QueueSave();
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
            QueueSave();
            StatsChanged?.Invoke();
            return true;
        }

        public void SaveNow()
        {
            WriteProgressToPrefs();
            PlayerPrefs.Save();
            savePending = false;
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

        private void LoadProgress()
        {
            coins = ReadDouble(CoinsKey, coins);
            strength = ReadDouble(StrengthKey, strength);
            totalReps = ReadDouble(RepsKey, totalReps);
        }

        private void QueueSave()
        {
            WriteProgressToPrefs();
            savePending = true;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        private void WriteProgressToPrefs()
        {
            PlayerPrefs.SetString(CoinsKey, coins.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(StrengthKey, strength.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(RepsKey, totalReps.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double ReadDouble(string key, double fallback)
        {
            string raw = PlayerPrefs.GetString(key, string.Empty);
            return double.TryParse(
                raw,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed)
                ? Math.Max(0d, parsed)
                : fallback;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && savePending)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            if (savePending)
            {
                SaveNow();
            }
        }

        private void OnDisable()
        {
            if (savePending)
            {
                SaveNow();
            }
        }
    }
}
