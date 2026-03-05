using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text toastText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.2f)] private float showDuration = 2f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(1)] private int maxQueuedMessages = 6;

        private readonly Queue<string> queue = new Queue<string>();
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            UiScrollLayoutHelper.ConfigureMultilineText(toastText);
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            int maxQueue = Mathf.Max(1, maxQueuedMessages);
            while (queue.Count >= maxQueue)
            {
                queue.Dequeue();
            }

            queue.Enqueue(message);
            if (activeRoutine == null)
            {
                activeRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (queue.Count > 0)
            {
                string message = queue.Dequeue();
                if (toastText != null)
                {
                    toastText.text = message;
                }

                yield return FadeTo(1f);
                yield return new WaitForSeconds(showDuration);
                yield return FadeTo(0f);
            }

            activeRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
