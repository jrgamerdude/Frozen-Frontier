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

        private EventSystem eventSystem;

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
                    buttonText.text = def.choices[i].label;
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
    }
}
