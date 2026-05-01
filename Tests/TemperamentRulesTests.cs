using Xunit;
using KitsuneCompanion;

namespace KitsuneCompanion.Tests
{
    public class TemperamentRulesTests
    {
        [Fact]
        public void LowHealth_DayOrNight_GoesProtective()
        {
            Assert.Equal(TemperamentRules.BuffProtective, TemperamentRules.Choose(0.3f, false));
            Assert.Equal(TemperamentRules.BuffProtective, TemperamentRules.Choose(0.3f, true));
        }

        [Fact]
        public void Night_NotLowHealth_GoesSerene()
        {
            Assert.Equal(TemperamentRules.BuffSerene, TemperamentRules.Choose(0.9f, true));
            Assert.Equal(TemperamentRules.BuffSerene, TemperamentRules.Choose(0.6f, true));
        }

        [Fact]
        public void Day_HighHealth_GoesPlayful()
        {
            Assert.Equal(TemperamentRules.BuffPlayful, TemperamentRules.Choose(0.9f, false));
        }

        [Fact]
        public void Day_MidHealth_LowBond_GoesCurious()
        {
            Assert.Equal(TemperamentRules.BuffCurious, TemperamentRules.Choose(0.6f, false));
            Assert.Equal(TemperamentRules.BuffCurious, TemperamentRules.Choose(0.5f, false));
        }

        [Fact]
        public void Day_MidHealth_TrustedOrHigher_LeansPlayful()
        {
            // 5-tier ladder: tier 2 = Trusted, tier 3 = Bound, tier 4 = Kindred.
            Assert.Equal(TemperamentRules.BuffPlayful, TemperamentRules.Choose(0.6f, false, 2));
            Assert.Equal(TemperamentRules.BuffPlayful, TemperamentRules.Choose(0.5f, false, 3));
            Assert.Equal(TemperamentRules.BuffPlayful, TemperamentRules.Choose(0.7f, false, 4));
        }

        [Fact]
        public void Day_MidHealth_FamiliarStillCurious()
        {
            // Tier 1 = Familiar (just below the Playful flip threshold).
            Assert.Equal(TemperamentRules.BuffCurious, TemperamentRules.Choose(0.6f, false, 1));
        }

        [Fact]
        public void BondTier_DoesNotOverrideLowHealthOrNight()
        {
            // Even at top tier (Kindred = 4), Protective and Serene priority wins.
            Assert.Equal(TemperamentRules.BuffProtective, TemperamentRules.Choose(0.3f, false, 4));
            Assert.Equal(TemperamentRules.BuffSerene,    TemperamentRules.Choose(0.9f, true,  4));
        }

        [Fact]
        public void HealthBoundary_AtExactly40Percent_IsNotProtective()
        {
            Assert.NotEqual(TemperamentRules.BuffProtective, TemperamentRules.Choose(0.4f, false));
        }

        [Fact]
        public void HealthBoundary_AtExactly80Percent_IsNotPlayful()
        {
            Assert.NotEqual(TemperamentRules.BuffPlayful, TemperamentRules.Choose(0.8f, false));
        }

        [Fact]
        public void All_ContainsFourDistinctBuffs()
        {
            Assert.Equal(4, TemperamentRules.All.Length);
            Assert.Contains(TemperamentRules.BuffCurious,    TemperamentRules.All);
            Assert.Contains(TemperamentRules.BuffProtective, TemperamentRules.All);
            Assert.Contains(TemperamentRules.BuffPlayful,    TemperamentRules.All);
            Assert.Contains(TemperamentRules.BuffSerene,     TemperamentRules.All);
        }
    }
}
