using UnityEngine;

namespace KitsuneCompanion
{
    public class ModApi : IModApi
    {
        private const float TickInterval = 2f;
        private static float _nextTick;

        public static string ModPath { get; private set; }

        public void InitMod(Mod _modInstance)
        {
            ModPath = _modInstance != null ? _modInstance.Path : null;
            Log.Out($"[KitsuneCompanion] InitMod (path={ModPath})");
            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
        }

        private static void OnGameUpdate(ref ModEvents.SGameUpdateData data)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var world = gm.World;
            if (world == null) return;

            float now = Time.time;
            if (now < _nextTick) return;
            _nextTick = now + TickInterval;

            CompanionTicker.Tick(world);
        }
    }
}
