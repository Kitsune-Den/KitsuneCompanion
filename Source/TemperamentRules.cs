namespace KitsuneCompanion
{
    public static class TemperamentRules
    {
        public const string BuffCurious    = "buffKitsuneCurious";
        public const string BuffProtective = "buffKitsuneProtective";
        public const string BuffPlayful    = "buffKitsunePlayful";
        public const string BuffSerene     = "buffKitsuneSerene";

        public static readonly string[] All =
            { BuffCurious, BuffProtective, BuffPlayful, BuffSerene };

        public static string Choose(float playerHealthPct, bool isNight)
        {
            return Choose(playerHealthPct, isNight, 0);
        }

        public static string Choose(float playerHealthPct, bool isNight, int bondTier)
        {
            if (playerHealthPct < 0.4f) return BuffProtective;
            if (isNight)                return BuffSerene;
            if (playerHealthPct > 0.8f) return BuffPlayful;

            // Bonded kitsunes lean playful at mid health rather than curious —
            // joy of the bond replaces wary watchfulness.
            if (bondTier >= 2) return BuffPlayful;
            return BuffCurious;
        }
    }
}
