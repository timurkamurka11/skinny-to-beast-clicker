using SkinnyToBeast.Economy;
using SkinnyToBeast.Player;
using SkinnyToBeast.Training;
using SkinnyToBeast.UI;
using UnityEngine;

namespace SkinnyToBeast.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TapTrainingController tapTrainingController;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private MainHudController hudController;

        private void Start()
        {
            if (tapTrainingController != null && playerStats != null)
            {
                tapTrainingController.SetPlayerStats(playerStats);
            }

            hudController?.Refresh();
        }
    }
}
