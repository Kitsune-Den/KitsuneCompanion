using Xunit;
using KitsuneCompanion;

namespace KitsuneCompanion.Tests
{
    public class BondRulesTests
    {
        [Theory]
        [InlineData(0f,    0)]
        [InlineData(9.99f, 0)]
        [InlineData(10f,   1)]
        [InlineData(49.99f, 1)]
        [InlineData(50f,   2)]
        [InlineData(199.99f, 2)]
        [InlineData(200f,  3)]
        [InlineData(9999f, 3)]
        public void Tier_BoundariesAreInclusiveOnLowerEdge(float points, int expected)
        {
            Assert.Equal(expected, BondRules.Tier(points));
        }

        [Fact]
        public void BuffForTier_MapsCorrectly()
        {
            Assert.Null(BondRules.BuffForTier(0));
            Assert.Equal(BondRules.BuffTrusted,  BondRules.BuffForTier(1));
            Assert.Equal(BondRules.BuffDevoted,  BondRules.BuffForTier(2));
            Assert.Equal(BondRules.BuffAwakened, BondRules.BuffForTier(3));
        }

        [Fact]
        public void AllBondBuffs_ContainsThreeTiers()
        {
            Assert.Equal(3, BondRules.AllBondBuffs.Length);
            Assert.Contains(BondRules.BuffTrusted,  BondRules.AllBondBuffs);
            Assert.Contains(BondRules.BuffDevoted,  BondRules.AllBondBuffs);
            Assert.Contains(BondRules.BuffAwakened, BondRules.AllBondBuffs);
        }

        [Fact]
        public void OneCharm_LiftsZeroToTrusted()
        {
            Assert.Equal(0, BondRules.Tier(0f));
            Assert.Equal(1, BondRules.Tier(0f + BondRules.BondPerCharm));
        }
    }
}
