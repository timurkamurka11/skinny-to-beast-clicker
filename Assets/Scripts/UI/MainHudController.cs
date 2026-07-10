using SkinnyToBeast.Economy;
using SkinnyToBeast.Player;
using SkinnyToBeast.Utils;
using TMPro;
using UnityEngine;

namespace SkinnyToBeast.UI
{
    public class MainHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private UpgradeManager upgradeManager;

        [Header("Main Text")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text strengthText;
        [SerializeField] private TMP_Text repsText;
        [SerializeField] private TMP_Text bodyStageText;

        [Header("Upgrade Text")]
        [SerializeField] private TMP_Text dumbbellsText;
        [SerializeField] private TMP_Text proteinText;
        [SerializeField] private TMP_Text coachText;
        [SerializeField] private TMP_Text betterGymText;

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged += Refresh;
            }

            if (upgradeManager != null)
            {
                upgradeManager.UpgradesChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged -= Refresh;
            }

            if (upgradeManager != null)
            {
                upgradeManager.UpgradesChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (playerStats != null)
            {
                SetText(coinsText, $"Coins: {NumberFormatter.Format(playerStats.Coins)}");
                SetText(strengthText, $"Strength: {NumberFormatter.Format(playerStats.Strength)}");
                SetText(repsText, $"Reps: {NumberFormatter.Format(playerStats.TotalReps)}");
                SetText(bodyStageText, $"Body: {playerStats.BodyStageName}");
            }

            RefreshUpgradeText("dumbbells", dumbbellsText);
            RefreshUpgradeText("protein", proteinText);
            RefreshUpgradeText("coach", coachText);
            RefreshUpgradeText("better_gym", betterGymText);
        }

        public void PurchaseDumbbells()
        {
            upgradeManager?.Purchase("dumbbells");
        }

        public void PurchaseProtein()
        {
            upgradeManager?.Purchase("protein");
        }

        public void PurchaseCoach()
        {
            upgradeManager?.Purchase("coach");
        }

        public void PurchaseBetterGym()
        {
            upgradeManager?.Purchase("better_gym");
        }

        private void RefreshUpgradeText(string upgradeId, TMP_Text targetText)
        {
            if (upgradeManager == null || targetText == null)
            {
                return;
            }

            UpgradeData upgrade = upgradeManager.GetUpgrade(upgradeId);
            if (upgrade == null)
            {
                targetText.text = upgradeId;
                return;
            }

            targetText.text = $"{upgrade.displayName} Lv.{upgrade.level} — {NumberFormatter.Format(upgrade.CurrentCost)} coins";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
