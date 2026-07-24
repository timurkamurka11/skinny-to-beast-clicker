using System;
using System.Collections.Generic;
using SkinnyToBeast.Player;
using SkinnyToBeast.Training;
using UnityEngine;

namespace SkinnyToBeast.Economy
{
    public class UpgradeManager : MonoBehaviour
    {
        private const string UpgradeLevelKeyPrefix = "game.upgrade.";

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TapTrainingController tapTrainingController;

        [Header("Upgrades")]
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();

        [Header("Auto Training")]
        [SerializeField] private double autoStrengthPerRep = 0.5;
        [SerializeField] private double autoCoinsPerRep = 0.1;

        private double autoRepsPerSecond;
        private double autoTimer;
        private bool bonusesApplied;

        public event Action UpgradesChanged;
        public IReadOnlyList<UpgradeData> Upgrades => upgrades;
        public double AutoRepsPerSecond => autoRepsPerSecond;

        private void Awake()
        {
            EnsureDefaultUpgrades();
            LoadUpgradeLevels();
        }

        private void Start()
        {
            RebuildBonuses();
        }

        private void Update()
        {
            if (playerStats == null || autoRepsPerSecond <= 0)
            {
                return;
            }

            autoTimer += Time.deltaTime;
            if (autoTimer < 1f)
            {
                return;
            }

            double seconds = Math.Floor(autoTimer);
            autoTimer -= seconds;

            double reps = autoRepsPerSecond * seconds;
            playerStats.AddTraining(reps, reps * autoStrengthPerRep, reps * autoCoinsPerRep);
        }

        public void SetReferences(PlayerStats stats, TapTrainingController trainingController)
        {
            playerStats = stats;
            tapTrainingController = trainingController;
            RebuildBonuses();
        }

        public bool Purchase(string upgradeId)
        {
            UpgradeData upgrade = upgrades.Find(item => item.id == upgradeId);
            if (upgrade == null)
            {
                Debug.LogWarning($"Upgrade not found: {upgradeId}");
                return false;
            }

            double cost = upgrade.CurrentCost;
            if (playerStats == null || !playerStats.TrySpend(cost))
            {
                return false;
            }

            upgrade.level++;
            ApplyUpgrade(upgrade);
            SaveUpgradeLevel(upgrade);
            UpgradesChanged?.Invoke();
            return true;
        }

        public UpgradeData GetUpgrade(string upgradeId)
        {
            return upgrades.Find(item => item.id == upgradeId);
        }

        private void ApplyUpgrade(UpgradeData upgrade)
        {
            if (tapTrainingController != null)
            {
                tapTrainingController.AddTapPowerMultiplier(upgrade.tapPowerBonus);
                tapTrainingController.AddCoinMultiplier(upgrade.coinMultiplierBonus);
            }

            autoRepsPerSecond += upgrade.autoRepsPerSecondBonus;
        }

        private void RebuildBonuses()
        {
            if (bonusesApplied || tapTrainingController == null)
            {
                return;
            }

            tapTrainingController.ResetMultipliers();
            autoRepsPerSecond = 0d;

            foreach (UpgradeData upgrade in upgrades)
            {
                if (upgrade == null || upgrade.level <= 0)
                {
                    continue;
                }

                tapTrainingController.AddTapPowerMultiplier(upgrade.tapPowerBonus * upgrade.level);
                tapTrainingController.AddCoinMultiplier(upgrade.coinMultiplierBonus * upgrade.level);
                autoRepsPerSecond += upgrade.autoRepsPerSecondBonus * upgrade.level;
            }

            bonusesApplied = true;
        }

        private void EnsureDefaultUpgrades()
        {
            if (upgrades.Count > 0)
            {
                return;
            }

            upgrades.Add(new UpgradeData
            {
                id = "dumbbells",
                displayName = "Dumbbells",
                baseCost = 10,
                costMultiplier = 1.18,
                tapPowerBonus = 0.25
            });

            upgrades.Add(new UpgradeData
            {
                id = "protein",
                displayName = "Protein",
                baseCost = 25,
                costMultiplier = 1.2,
                coinMultiplierBonus = 0.15
            });

            upgrades.Add(new UpgradeData
            {
                id = "coach",
                displayName = "Coach",
                baseCost = 100,
                costMultiplier = 1.25,
                autoRepsPerSecondBonus = 1
            });

            upgrades.Add(new UpgradeData
            {
                id = "better_gym",
                displayName = "Better Gym",
                baseCost = 500,
                costMultiplier = 1.3,
                tapPowerBonus = 0.5,
                coinMultiplierBonus = 0.25,
                autoRepsPerSecondBonus = 2
            });
        }

        private void LoadUpgradeLevels()
        {
            foreach (UpgradeData upgrade in upgrades)
            {
                if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.id))
                {
                    continue;
                }

                upgrade.level = Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(UpgradeLevelKeyPrefix + upgrade.id, upgrade.level));
            }
        }

        private static void SaveUpgradeLevel(UpgradeData upgrade)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.id))
            {
                return;
            }

            PlayerPrefs.SetInt(UpgradeLevelKeyPrefix + upgrade.id, Mathf.Max(0, upgrade.level));
            PlayerPrefs.Save();
        }
    }
}
