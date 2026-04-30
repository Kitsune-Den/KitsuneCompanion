using Xunit;
using KitsuneCompanion;

namespace KitsuneCompanion.Tests
{
    public class KitsuneNamesTests
    {
        [Fact]
        public void GetName_IsDeterministic()
        {
            Assert.Equal(KitsuneNames.GetName(123), KitsuneNames.GetName(123));
            Assert.Equal(KitsuneNames.GetName(int.MaxValue), KitsuneNames.GetName(int.MaxValue));
        }

        [Fact]
        public void GetName_AlwaysReturnsAPoolEntry()
        {
            for (int id = -100; id < 200; id++)
            {
                var name = KitsuneNames.GetName(id);
                Assert.NotNull(name);
                Assert.Contains(name, KitsuneNames.Pool);
            }
        }

        [Fact]
        public void GetName_DifferentIdsTendToDiffer()
        {
            // Not a strict pigeonhole guarantee, but with 36 names and 36 ids
            // a multiplicative hash should yield at least 20 distinct picks.
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int id = 0; id < 36; id++) seen.Add(KitsuneNames.GetName(id));
            Assert.True(seen.Count >= 20, $"Expected variety; only got {seen.Count}");
        }

        [Fact]
        public void Pool_HasReasonableSize()
        {
            Assert.True(KitsuneNames.Pool.Length >= 16, "Pool too small for variety");
        }
    }
}
