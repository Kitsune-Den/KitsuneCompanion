namespace KitsuneCompanion
{
    public static class BondRules
    {
        public const string CvarBondPoints = "kitsuneBondPoints";

        public const string BondCharmItem = "kitsuneBondCharm";

        public const float BondPerCharm = 25f;
        public const float BondPerTick = 1f / 30f;

        // Hard ceiling on bond points. Plenty of headroom past Kindred (300)
        // for dedicated bonders, but bounded so the cvar doesn't grow without
        // limit over a long playthrough.
        public const float MaxBondPoints = 1000f;

        // 5-tier ladder: Faint (0) → Familiar → Trusted → Bound → Kindred.
        // "Trusted" persists as a name from the prior 4-tier ladder but
        // now lives at tier 2 (was tier 1).
        public const float ThresholdFamiliar = 5f;
        public const float ThresholdTrusted  = 25f;
        public const float ThresholdBound    = 100f;
        public const float ThresholdKindred  = 300f;

        public const string BuffFamiliar = "buffKitsuneBondFamiliar";
        public const string BuffTrusted  = "buffKitsuneBondTrusted";
        public const string BuffBound    = "buffKitsuneBondBound";
        public const string BuffKindred  = "buffKitsuneBondKindred";

        public static readonly string[] AllBondBuffs =
            { BuffFamiliar, BuffTrusted, BuffBound, BuffKindred };

        public static int Tier(float bondPoints)
        {
            if (bondPoints >= ThresholdKindred)  return 4;
            if (bondPoints >= ThresholdBound)    return 3;
            if (bondPoints >= ThresholdTrusted)  return 2;
            if (bondPoints >= ThresholdFamiliar) return 1;
            return 0;
        }

        public static string BuffForTier(int tier)
        {
            switch (tier)
            {
                case 1: return BuffFamiliar;
                case 2: return BuffTrusted;
                case 3: return BuffBound;
                case 4: return BuffKindred;
                default: return null;
            }
        }

        // Clamp a positive bond-point increment so the running total never
        // exceeds MaxBondPoints. Negative deltas (decay, future use) pass
        // through unchanged.
        public static float ClampDelta(float currentBond, float requestedDelta)
        {
            if (requestedDelta <= 0f) return requestedDelta;
            if (currentBond >= MaxBondPoints) return 0f;
            float newValue = currentBond + requestedDelta;
            if (newValue > MaxBondPoints) return MaxBondPoints - currentBond;
            return requestedDelta;
        }

        // Fractional position within the current tier in [0, 1].
        // 0.0 means just entered this tier; 1.0 means at the next-tier
        // threshold. Max tier (Kindred) always returns 1.0.
        public static float TierProgress(float bondPoints)
        {
            if (bondPoints <= 0f) return 0f;

            float lower, upper;
            switch (Tier(bondPoints))
            {
                case 0: lower = 0f;                 upper = ThresholdFamiliar; break;
                case 1: lower = ThresholdFamiliar;  upper = ThresholdTrusted;  break;
                case 2: lower = ThresholdTrusted;   upper = ThresholdBound;    break;
                case 3: lower = ThresholdBound;     upper = ThresholdKindred;  break;
                default: return 1f;
            }

            float span = upper - lower;
            if (span <= 0f) return 1f;
            return (bondPoints - lower) / span;
        }
    }
}
