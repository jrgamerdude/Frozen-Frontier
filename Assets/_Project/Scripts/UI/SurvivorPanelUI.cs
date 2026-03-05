using System.Text;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FrozenFrontier.UI
{
    public class SurvivorPanelUI : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button assignLumberjackButton;
        [SerializeField] private Button assignCookButton;
        [SerializeField] private Button assignBuilderButton;
        [SerializeField] private Button assignExplorerButton;
        [SerializeField] private Button assignMedicButton;
        [SerializeField] private Button assignCollectorButton;
        [SerializeField] private Button resetJobsButton;

        [Header("Text")]
        [SerializeField] private Text summaryText;
        [SerializeField] private Text listText;

        private SurvivorSystem survivorSystem;

        private void Awake()
        {
            UiScrollLayoutHelper.EnsureVerticalScroll(transform as RectTransform);
            UiScrollLayoutHelper.ConfigureMultilineText(summaryText);
            UiScrollLayoutHelper.ConfigureMultilineText(listText);
        }

        public void Bind(SurvivorSystem survivors)
        {
            Unbind();
            survivorSystem = survivors;

            if (survivorSystem != null)
            {
                survivorSystem.Changed += Refresh;
            }

            Wire(assignLumberjackButton, () => Assign(SurvivorJob.Lumberjack));
            Wire(assignCookButton, () => Assign(SurvivorJob.Cook));
            Wire(assignBuilderButton, () => Assign(SurvivorJob.Builder));
            Wire(assignExplorerButton, () => Assign(SurvivorJob.Explorer));
            Wire(assignMedicButton, () => Assign(SurvivorJob.Medic));
            Wire(assignCollectorButton, () => Assign(SurvivorJob.Collector));

            if (resetJobsButton != null)
            {
                resetJobsButton.onClick.RemoveAllListeners();
                resetJobsButton.onClick.AddListener(() =>
                {
                    if (survivorSystem != null)
                    {
                        survivorSystem.ResetJobs();
                    }
                });
            }

            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Assign(SurvivorJob job)
        {
            if (survivorSystem == null)
            {
                return;
            }

            bool assignAllIdle = IsShiftPressed();
            if (assignAllIdle)
            {
                int moved = survivorSystem.AssignIdleToJob(job, int.MaxValue);
                if (moved == 0)
                {
                    survivorSystem.AssignOneToJob(job);
                }

                return;
            }

            survivorSystem.AssignOneToJob(job);
        }

        private void Refresh()
        {
            if (survivorSystem == null)
            {
                return;
            }

            if (summaryText != null)
            {
                summaryText.text =
                    $"Population {survivorSystem.Survivors.Count}/{survivorSystem.SurvivorCap}\n" +
                    $"Health {survivorSystem.GetAverageHealth()}%  Morale {survivorSystem.GetAverageMorale()}%\n" +
                    $"Idle {survivorSystem.GetCountByJob(SurvivorJob.Idle)}  " +
                    $"Lumber {survivorSystem.GetCountByJob(SurvivorJob.Lumberjack)}  " +
                    $"Cook {survivorSystem.GetCountByJob(SurvivorJob.Cook)}  " +
                    $"Builder {survivorSystem.GetCountByJob(SurvivorJob.Builder)}  " +
                    $"Explorer {survivorSystem.GetCountByJob(SurvivorJob.Explorer)}\n" +
                    "Tip: Hold Shift while clicking a job to assign all idle survivors.";
            }

            if (listText != null)
            {
                StringBuilder sb = new StringBuilder(256);
                for (int i = 0; i < survivorSystem.Survivors.Count; i++)
                {
                    SurvivorRuntimeData survivor = survivorSystem.Survivors[i];
                    sb.Append(survivor.displayName)
                        .Append(" | ")
                        .Append(survivor.job)
                        .Append(" | H:")
                        .Append(survivor.health)
                        .Append(" M:")
                        .Append(survivor.morale)
                        .Append(" Hu:")
                        .Append(survivor.hunger)
                        .Append(" W:")
                        .Append(survivor.warmth)
                        .Append('\n');
                }

                listText.text = sb.ToString().TrimEnd('\n');
            }
        }

        private void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void Unbind()
        {
            if (survivorSystem != null)
            {
                survivorSystem.Changed -= Refresh;
            }
        }

        private static bool IsShiftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }
    }
}
