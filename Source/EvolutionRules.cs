namespace KitsuneCompanion
{
    public static class EvolutionRules
    {
        public const string FormMist = "buffKitsuneFormMist";

        public const string TalismanMist = "kitsuneMistTalisman";

        public static readonly string[] AllForms = { FormMist };

        public const float DefaultTeleportDistance = 60f;
        public const float MistTeleportDistance = 30f;

        public static float GetTeleportDistance(string activeForm)
        {
            if (activeForm == FormMist) return MistTeleportDistance;
            return DefaultTeleportDistance;
        }

        public static string GetFormFromTalisman(string talismanItemName)
        {
            if (talismanItemName == TalismanMist) return FormMist;
            return null;
        }
    }
}
