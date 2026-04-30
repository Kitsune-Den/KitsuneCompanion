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
    }
}
