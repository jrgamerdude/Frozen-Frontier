using System.Text;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class EventPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private bool showChoiceCosts = true;

        private EventSystem eventSystem;

        private void Awake()
        {
            UiScrollLayoutHelper.EnsureVerticalScroll(GetPopupRootRect());
            UiScrollLayoutHelper.ConfigureMultilineText(titleText);
            UiScrollLayoutHelper.ConfigureMultilineText(descriptionText);
        }

        public void Bind(EventSystem events)
        {
            Unbind();
            eventSystem = events;
            if (eventSystem != null)
            {
                eventSystem.EventOpened += OnEventOpened;
                eventSystem.EventClosed += OnEventClosed;
            }

            SetVisible(false);
            if (eventSystem != null && eventSystem.ActiveEvent != null)
            {
                OnEventOpened(eventSystem.ActiveEvent);
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnEventOpened(EventDef def)
        {
            if (def == null)
            {
                return;
            }

            SetVisible(true);
            if (titleText != null)
            {
                titleText.text = def.title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = def.description;
            }

            if (choiceButtons == null || choiceButtons.Length == 0)
            {
                return;
            }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                Button button = choiceButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool active = def.choices != null && i < def.choices.Length && def.choices[i] != null;
                button.gameObject.SetActive(active);
                button.onClick.RemoveAllListeners();
                if (!active)
                {
                    continue;
                }

                int capturedIndex = i;
                button.onClick.AddListener(() =>
                {
                    if (eventSystem != null)
                    {
                        eventSystem.ResolveChoice(capturedIndex);
                    }
                });

                Text buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    UiScrollLayoutHelper.ConfigureButtonLabel(buttonText);
                    buttonText.text = FormatChoiceLabel(def.choices[i]);
                }
            }
        }

        private void OnEventClosed()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void Unbind()
        {
            if (eventSystem != null)
            {
                eventSystem.EventOpened -= OnEventOpened;
                eventSystem.EventClosed -= OnEventClosed;
            }
        }

        private RectTransform GetPopupRootRect()
        {
            if (panelRoot != null)
            {
                return panelRoot.transform as RectTransform;
            }

            return transform as RectTransform;
        }

        private string FormatChoiceLabel(EventChoiceDef choice)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            string label = string.IsNullOrWhiteSpace(choice.label) ? "Choose" : choice.label.Trim();
            if (!showChoiceCosts || choice.costs == null || choice.costs.Length == 0)
            {
                return label;
            }

            StringBuilder costs = new StringBuilder(32);
            for (int i = 0; i < choice.costs.Length; i++)
            {
                ResourceAmount cost = choice.costs[i];
                if (cost == null || cost.amount <= 0)
                {
                    continue;
                }

                if (costs.Length > 0)
                {
                    costs.Append("  ");
                }

                costs.Append('-')
                    .Append(cost.amount)
                    .Append(' ')
                    .Append(FormatResourceType(cost.type));
            }

            if (costs.Length == 0)
            {
                return label;
            }

            return $"{label}\n{costs}";
        }

        private string FormatResourceType(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return "Wood";
                case ResourceType.Food:
                    return "Food";
                case ResourceType.Fuel:
                    return "Fuel";
                case ResourceType.Scrap:
                    return "Scrap";
                default:
                    return type.ToString();
            }
        }
    }
}
