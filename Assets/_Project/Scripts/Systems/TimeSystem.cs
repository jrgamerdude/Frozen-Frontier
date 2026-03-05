using System;
using System.Globalization;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class TimeSystem : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float tickIntervalSeconds = 1f;
        [SerializeField, Min(0)] private int offlineProgressCapHours = 4;

        private float tickAccumulator;
        private int totalTicks;

        public event Action<int> TickRaised;

        public int TotalTicks => totalTicks;
        public float TickIntervalSeconds => tickIntervalSeconds;
        public int OfflineProgressCapHours => offlineProgressCapHours;

        private void Update()
        {
            float safeInterval = Mathf.Max(0.01f, tickIntervalSeconds);
            tickAccumulator += Time.deltaTime;
            while (tickAccumulator >= safeInterval)
            {
                tickAccumulator -= safeInterval;
                totalTicks++;
                TickRaised?.Invoke(totalTicks);
            }
        }

        public void SetTotalTicks(int value)
        {
            totalTicks = Mathf.Max(0, value);
        }

        public int ComputeOfflineTicks(string lastSaveUtcIso)
        {
            if (string.IsNullOrWhiteSpace(lastSaveUtcIso))
            {
                return 0;
            }

            if (!DateTime.TryParse(
                    lastSaveUtcIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime lastSaveUtc))
            {
                return 0;
            }

            TimeSpan elapsed = DateTime.UtcNow - lastSaveUtc;
            if (elapsed.TotalSeconds <= 0)
            {
                return 0;
            }

            float maxSeconds = offlineProgressCapHours * 3600f;
            float clampedSeconds = Mathf.Min((float)elapsed.TotalSeconds, maxSeconds);
            return Mathf.FloorToInt(clampedSeconds / Mathf.Max(tickIntervalSeconds, 0.01f));
        }

        public string GetUtcNowIso()
        {
            return DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
