using UnityEngine;

namespace KitsuneCompanion
{
    public class ModApi : IModApi
    {
        // Heavy tick (skin/talisman/charm/bond/temperament/tier) — 2s.
        // Bond accrual rate is calibrated against this cadence.
        private const float HeavyTickInterval = 2f;
        // Fast tick (follow only) — 0.25s. Outruns the entity's Wander AI
        // task so ApproachSpot wins consistently. Cheap.
        private const float FollowTickInterval = 0.25f;

        private static float _nextHeavyTick;
        private static float _nextFollowTick;

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

            if (now >= _nextFollowTick)
            {
                _nextFollowTick = now + FollowTickInterval;
                CompanionTicker.TickFollow(world);
            }

            if (now >= _nextHeavyTick)
            {
                _nextHeavyTick = now + HeavyTickInterval;
                CompanionTicker.Tick(world);
            }
        }
    }
}
