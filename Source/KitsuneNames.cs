namespace KitsuneCompanion
{
    public static class KitsuneNames
    {
        // Mix of Japanese-origin (kitsune lore) and Western fox-themed names.
        // Order matters for hash stability — adding names is safe; reordering or
        // removing breaks existing kitsune name assignments.
        public static readonly string[] Pool =
        {
            "Akari", "Ayame", "Hana", "Hoshi", "Inari",
            "Kaida", "Kage", "Kohaku", "Kuro", "Mei",
            "Rin", "Sakura", "Sora", "Suki", "Tora",
            "Tsuki", "Yume", "Yumi", "Yuki", "Kogitsune",
            "Vesper", "Saoirse", "Nox", "Ember", "Mist",
            "Hazel", "Flicker", "Fennec", "Reynard", "Vixen",
            "Sable", "Cinder", "Wren", "Briar", "Tamsin",
            "Kit"
        };

        public static string GetName(int entityId)
        {
            if (Pool.Length == 0) return "Kitsune";
            // MurmurHash3 finalizer — gives good distribution on sequential
            // small ids where a single multiplicative step would cluster.
            uint h = unchecked((uint)entityId);
            h ^= h >> 16;
            h = unchecked(h * 0x85ebca6bu);
            h ^= h >> 13;
            h = unchecked(h * 0xc2b2ae35u);
            h ^= h >> 16;
            int idx = (int)(h % (uint)Pool.Length);
            return Pool[idx];
        }
    }
}
