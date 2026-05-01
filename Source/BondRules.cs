namespace KitsuneCompanion
{
    public static class BondRules
    {
        public const string CvarBondPoints = "kitsuneBondPoints";

        public const string BondCharmItem = "kitsuneBondCharm";

        public const float BondPerCharm = 25f;
        public const float BondPerTick = 1f / 30f;

        public const float ThresholdTrusted  = 10f;
        public const float ThresholdDevoted  = 50f;
        public const float ThresholdAwakened = 200f;

        public const string BuffTrusted  = "buffKitsuneBondTrusted";
        public const string BuffDevoted  = "buffKitsuneBondDevoted";
        public const string BuffAwakened = "buffKitsuneBondAwakened";

        public static readonly string[] AllBondBuffs =
            { BuffTrusted, BuffDevoted, BuffAwakened };

        public static int Tier(float bondPoints)
        {
            if (bondPoints >= ThresholdAwakened) return 3;
            if (bondPoints >= ThresholdDevoted)  return 2;
            if (bondPoints >= ThresholdTrusted)  return 1;
            return 0;
        }

        public static string BuffForTier(int tier)
        {
            switch (tier)
            {
                case 1: return BuffTrusted;
                case 2: return BuffDevoted;
                case 3: return BuffAwakened;
                default: return null;
            }
        }

        // Fractional position within the current tier in [0, 1].
        // 0.0 means just entered this tier; 1.0 means at the next-tier
        // threshold. Max tier (Awakened) always returns 1.0.
        public static float TierProgress(float bondPoints)
        {
            if (bondPoints <= 0f) return 0f;

            float lower, upper;
            switch (Tier(bondPoints))
            {
                case 0: lower = 0f;                upper = ThresholdTrusted;  break;
                case 1: lower = ThresholdTrusted;  upper = ThresholdDevoted;  break;
                case 2: lower = ThresholdDevoted;  upper = ThresholdAwakened; break;
                default: return 1f;
            }

            float span = upper - lower;
            if (span <= 0f) return 1f;
            return (bondPoints - lower) / span;
        }
    }
}
