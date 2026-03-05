using FrozenFrontier.Core;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class ResourceBarUI : MonoBehaviour
    {
        [SerializeField] private Text woodText;
        [SerializeField] private Text foodText;
        [SerializeField] private Text fuelText;
        [SerializeField] private Text scrapText;
        [SerializeField] private Text heatText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text survivorText;
        [SerializeField] private Text moraleText;
        [SerializeField] private Text tickText;
        [SerializeField] private Button saveButton;

        private ResourceSystem resourceSystem;
        private SurvivorSystem survivorSystem;
        private TimeSystem timeSystem;
        private GameManager gameManager;

        public void Bind(ResourceSystem resources, SurvivorSystem survivors, TimeSystem time, GameManager manager)
        {
            Unbind();

            resourceSystem = resources;
            survivorSystem = survivors;
            timeSystem = time;
            gameManager = manager;

            if (resourceSystem != null)
            {
                resourceSystem.Changed += Refresh;
            }

            if (survivorSystem != null)
            {
                survivorSystem.Changed += Refresh;
            }

            if (timeSystem != null)
            {
                timeSystem.TickRaised += OnTick;
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                    {
                        gameManager.SaveNow(true);
                    }
                });
            }

            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnTick(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (resourceSystem == null)
            {
                return;
            }

            SetText(woodText, $"Wood {resourceSystem.GetAmount(ResourceType.Wood)}/{resourceSystem.GetCap(ResourceType.Wood)}");
            SetText(foodText, $"Food {resourceSystem.GetAmount(ResourceType.Food)}/{resourceSystem.GetCap(ResourceType.Food)}");
            SetText(fuelText, $"Fuel {resourceSystem.GetAmount(ResourceType.Fuel)}/{resourceSystem.GetCap(ResourceType.Fuel)}");
            SetText(scrapText, $"Scrap {resourceSystem.GetAmount(ResourceType.Scrap)}/{resourceSystem.GetCap(ResourceType.Scrap)}");
            SetText(heatText, $"Heat {resourceSystem.Heat}");
            SetText(powerText, $"Power {resourceSystem.Power}");

            if (survivorSystem != null)
            {
                SetText(survivorText, $"Survivors {survivorSystem.Survivors.Count}/{survivorSystem.SurvivorCap}");
                SetText(moraleText, $"Morale {survivorSystem.GetAverageMorale()}%");
            }

            if (timeSystem != null)
            {
                SetText(tickText, $"Tick {timeSystem.TotalTicks}");
            }
        }

        private void SetText(Text textComponent, string value)
        {
            if (textComponent != null)
            {
                textComponent.text = value;
            }
        }

        private void Unbind()
        {
            if (resourceSystem != null)
            {
                resourceSystem.Changed -= Refresh;
            }

            if (survivorSystem != null)
            {
                survivorSystem.Changed -= Refresh;
            }

            if (timeSystem != null)
            {
                timeSystem.TickRaised -= OnTick;
            }
        }
    }
}
