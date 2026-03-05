using FrozenFrontier.Core;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class ResourceBarUI : MonoBehaviour
    {
        private enum DisplayMode
        {
            Full,
            Compact,
            UltraCompact
        }

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
        [SerializeField, Min(400f)] private float compactWidthThreshold = 1500f;
        [SerializeField, Min(400f)] private float ultraCompactWidthThreshold = 1200f;

        private ResourceSystem resourceSystem;
        private SurvivorSystem survivorSystem;
        private TimeSystem timeSystem;
        private GameManager gameManager;

        private void Awake()
        {
            UiScrollLayoutHelper.ConfigureSingleLineText(woodText);
            UiScrollLayoutHelper.ConfigureSingleLineText(foodText);
            UiScrollLayoutHelper.ConfigureSingleLineText(fuelText);
            UiScrollLayoutHelper.ConfigureSingleLineText(scrapText);
            UiScrollLayoutHelper.ConfigureSingleLineText(heatText);
            UiScrollLayoutHelper.ConfigureSingleLineText(powerText);
            UiScrollLayoutHelper.ConfigureSingleLineText(survivorText);
            UiScrollLayoutHelper.ConfigureSingleLineText(moraleText);
            UiScrollLayoutHelper.ConfigureSingleLineText(tickText);
        }

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

            DisplayMode mode = GetDisplayMode();
            int wood = resourceSystem.GetAmount(ResourceType.Wood);
            int food = resourceSystem.GetAmount(ResourceType.Food);
            int fuel = resourceSystem.GetAmount(ResourceType.Fuel);
            int scrap = resourceSystem.GetAmount(ResourceType.Scrap);
            int woodCap = resourceSystem.GetCap(ResourceType.Wood);
            int foodCap = resourceSystem.GetCap(ResourceType.Food);
            int fuelCap = resourceSystem.GetCap(ResourceType.Fuel);
            int scrapCap = resourceSystem.GetCap(ResourceType.Scrap);

            switch (mode)
            {
                case DisplayMode.UltraCompact:
                    SetText(woodText, $"W {wood}");
                    SetText(foodText, $"F {food}");
                    SetText(fuelText, $"Fu {fuel}");
                    SetText(scrapText, $"S {scrap}");
                    SetText(heatText, $"H {resourceSystem.Heat}");
                    SetText(powerText, $"P {resourceSystem.Power}");
                    break;
                case DisplayMode.Compact:
                    SetText(woodText, $"W {wood}/{woodCap}");
                    SetText(foodText, $"F {food}/{foodCap}");
                    SetText(fuelText, $"Fu {fuel}/{fuelCap}");
                    SetText(scrapText, $"S {scrap}/{scrapCap}");
                    SetText(heatText, $"Heat {resourceSystem.Heat}");
                    SetText(powerText, $"Power {resourceSystem.Power}");
                    break;
                default:
                    SetText(woodText, $"Wood {wood}/{woodCap}");
                    SetText(foodText, $"Food {food}/{foodCap}");
                    SetText(fuelText, $"Fuel {fuel}/{fuelCap}");
                    SetText(scrapText, $"Scrap {scrap}/{scrapCap}");
                    SetText(heatText, $"Heat {resourceSystem.Heat}");
                    SetText(powerText, $"Power {resourceSystem.Power}");
                    break;
            }

            if (survivorSystem != null)
            {
                if (mode == DisplayMode.UltraCompact)
                {
                    SetText(survivorText, $"Pop {survivorSystem.Survivors.Count}");
                    SetText(moraleText, $"M {survivorSystem.GetAverageMorale()}%");
                }
                else if (mode == DisplayMode.Compact)
                {
                    SetText(survivorText, $"Pop {survivorSystem.Survivors.Count}/{survivorSystem.SurvivorCap}");
                    SetText(moraleText, $"Mor {survivorSystem.GetAverageMorale()}%");
                }
                else
                {
                    SetText(survivorText, $"Survivors {survivorSystem.Survivors.Count}/{survivorSystem.SurvivorCap}");
                    SetText(moraleText, $"Morale {survivorSystem.GetAverageMorale()}%");
                }
            }

            if (timeSystem != null)
            {
                SetText(tickText, mode == DisplayMode.Full ? $"Tick {timeSystem.TotalTicks}" : $"T {timeSystem.TotalTicks}");
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

        private DisplayMode GetDisplayMode()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null)
            {
                return DisplayMode.Full;
            }

            float width = rt.rect.width;
            if (width <= 0f)
            {
                return DisplayMode.Full;
            }

            if (width <= Mathf.Max(400f, ultraCompactWidthThreshold))
            {
                return DisplayMode.UltraCompact;
            }

            if (width <= Mathf.Max(400f, compactWidthThreshold))
            {
                return DisplayMode.Compact;
            }

            return DisplayMode.Full;
        }
    }
}
