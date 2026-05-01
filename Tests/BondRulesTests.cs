using Xunit;
using KitsuneCompanion;

namespace KitsuneCompanion.Tests
{
    public class BondRulesTests
    {
        [Theory]
        // 5-tier ladder: Faint(0)→Familiar(5)→Trusted(25)→Bound(100)→Kindred(300)
        [InlineData(0f,      0)]   // Faint
        [InlineData(4.99f,   0)]
        [InlineData(5f,      1)]   // Familiar
        [InlineData(24.99f,  1)]
        [InlineData(25f,     2)]   // Trusted
        [InlineData(99.99f,  2)]
        [InlineData(100f,    3)]   // Bound
        [InlineData(299.99f, 3)]
        [InlineData(300f,    4)]   // Kindred
        [InlineData(9999f,   4)]
        public void Tier_BoundariesAreInclusiveOnLowerEdge(float points, int expected)
        {
            Assert.Equal(expected, BondRules.Tier(points));
        }

        [Fact]
        public void BuffForTier_MapsCorrectly()
        {
            Assert.Null(BondRules.BuffForTier(0));
            Assert.Equal(BondRules.BuffFamiliar, BondRules.BuffForTier(1));
            Assert.Equal(BondRules.BuffTrusted,  BondRules.BuffForTier(2));
            Assert.Equal(BondRules.BuffBound,    BondRules.BuffForTier(3));
            Assert.Equal(BondRules.BuffKindred,  BondRules.BuffForTier(4));
        }

        [Fact]
        public void AllBondBuffs_ContainsFourTiers()
        {
            Assert.Equal(4, BondRules.AllBondBuffs.Length);
            Assert.Contains(BondRules.BuffFamiliar, BondRules.AllBondBuffs);
            Assert.Contains(BondRules.BuffTrusted,  BondRules.AllBondBuffs);
            Assert.Contains(BondRules.BuffBound,    BondRules.AllBondBuffs);
            Assert.Contains(BondRules.BuffKindred,  BondRules.AllBondBuffs);
        }

        [Fact]
        public void OneCharm_LiftsZeroToTrusted()
        {
            // 1 charm = 25 points = Trusted threshold (skips Familiar at 5).
            Assert.Equal(0, BondRules.Tier(0f));
            Assert.Equal(2, BondRules.Tier(0f + BondRules.BondPerCharm));
        }

        [Theory]
        [InlineData(-5f,   0f)]   // negative clamped to 0
        [InlineData(0f,    0f)]   // tier 0 floor
        [InlineData(2.5f,  0.5f)] // tier 0 midpoint (between 0 and 5)
        [InlineData(5f,    0f)]   // tier 1 floor (Familiar)
        [InlineData(15f,   0.5f)] // tier 1 midpoint (between 5 and 25)
        [InlineData(25f,   0f)]   // tier 2 floor (Trusted)
        [InlineData(62.5f, 0.5f)] // tier 2 midpoint (between 25 and 100)
        [InlineData(100f,  0f)]   // tier 3 floor (Bound)
        [InlineData(200f,  0.5f)] // tier 3 midpoint (between 100 and 300)
        [InlineData(300f,  1f)]   // tier 4 (Kindred) — max tier returns 1
        [InlineData(9999f, 1f)]   // max tier holds at 1
        public void TierProgress_FractionalPositionWithinTier(float points, float expected)
        {
            Assert.Equal(expected, BondRules.TierProgress(points), precision: 4);
        }

        [Fact]
        public void ClampDelta_BelowCap_PassesDeltaThrough()
        {
            Assert.Equal(25f, BondRules.ClampDelta(currentBond: 0f, requestedDelta: 25f));
            Assert.Equal(50f, BondRules.ClampDelta(currentBond: 500f, requestedDelta: 50f));
        }

        [Fact]
        public void ClampDelta_AtCap_ReturnsZero()
        {
            Assert.Equal(0f, BondRules.ClampDelta(currentBond: BondRules.MaxBondPoints, requestedDelta: 25f));
            Assert.Equal(0f, BondRules.ClampDelta(currentBond: BondRules.MaxBondPoints + 100f, requestedDelta: 25f));
        }

        [Fact]
        public void ClampDelta_CrossingCap_ReturnsRemainingHeadroom()
        {
            // 990 + 25 would be 1015; cap is 1000; expect 10.
            Assert.Equal(10f, BondRules.ClampDelta(currentBond: 990f, requestedDelta: 25f));
        }

        [Fact]
        public void ClampDelta_NegativeDelta_PassesThroughUnchanged()
        {
            // Decay (future use) shouldn't be clamped by the upper bound.
            Assert.Equal(-5f, BondRules.ClampDelta(currentBond: 100f, requestedDelta: -5f));
            Assert.Equal(-50f, BondRules.ClampDelta(currentBond: BondRules.MaxBondPoints, requestedDelta: -50f));
        }
    }
}
