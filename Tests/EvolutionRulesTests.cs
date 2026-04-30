using Xunit;
using KitsuneCompanion;

namespace KitsuneCompanion.Tests
{
    public class EvolutionRulesTests
    {
        [Fact]
        public void TalismanMist_MapsToFormMist()
        {
            Assert.Equal(EvolutionRules.FormMist, EvolutionRules.GetFormFromTalisman(EvolutionRules.TalismanMist));
        }

        [Fact]
        public void UnknownTalisman_MapsToNull()
        {
            Assert.Null(EvolutionRules.GetFormFromTalisman("notARealTalisman"));
            Assert.Null(EvolutionRules.GetFormFromTalisman(""));
        }

        [Fact]
        public void TeleportDistance_MistFormShortens()
        {
            Assert.Equal(EvolutionRules.MistTeleportDistance, EvolutionRules.GetTeleportDistance(EvolutionRules.FormMist));
            Assert.True(EvolutionRules.MistTeleportDistance < EvolutionRules.DefaultTeleportDistance);
        }

        [Fact]
        public void TeleportDistance_NoFormUsesDefault()
        {
            Assert.Equal(EvolutionRules.DefaultTeleportDistance, EvolutionRules.GetTeleportDistance(null));
            Assert.Equal(EvolutionRules.DefaultTeleportDistance, EvolutionRules.GetTeleportDistance("buffKitsuneCurious"));
        }

        [Fact]
        public void AllForms_ContainsMist()
        {
            Assert.Contains(EvolutionRules.FormMist, EvolutionRules.AllForms);
        }
    }
}
