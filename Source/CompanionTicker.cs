using UnityEngine;

namespace KitsuneCompanion
{
    internal static class CompanionTicker
    {
        private const string EntityClassName = "kitsuneCompanion";

        private const float FollowMaxRange = 30f;
        private const float FollowStartDistance = 6f;
        private const float TalismanRange = 8f;
        private const float BondAccrualRange = 12f;

        private static int _diagTickCount;

        public static void Tick(World world)
        {
            var alives = world.EntityAlives;
            if (alives == null) return;

            int kitsuneSeen = 0;
            for (int i = 0; i < alives.Count; i++)
            {
                var alive = alives[i];
                if (alive != null && IsKitsune(alive))
                {
                    kitsuneSeen++;
                    UpdateKitsune(alive, world);
                }
            }

            // Slim diag — verifies tick alive and kitsune count. Remove
            // entirely once follow + temperament confirmed end-to-end.
            if (_diagTickCount++ % 15 == 0)
                Log.Out($"[KitsuneCompanion] tick: alives={alives.Count} kitsuneSeen={kitsuneSeen}");
        }

        private static bool IsKitsune(EntityAlive alive)
        {
            // entityClass is a 32-bit hash, can legitimately be negative.
            // Only zero indicates an unset/invalid class.
            if (alive.entityClass == 0) return false;
            return EntityClass.GetEntityClassName(alive.entityClass) == EntityClassName;
        }

        private static void UpdateKitsune(EntityAlive kitsune, World world)
        {
            if (ModApi.ModPath != null)
                KitsuneSkinner.EnsureSkinned(kitsune, ModApi.ModPath);

            var player = world.GetClosestPlayer(kitsune.position, FollowMaxRange, false);
            if (player == null)
            {
                ClearAllTemperaments(kitsune);
                return;
            }

            float distToPlayer = Vector3.Distance(kitsune.position, player.position);

            TryConsumeFormTalisman(kitsune, player, distToPlayer);
            TryConsumeBondCharm(kitsune, player, distToPlayer);
            AccrueBond(kitsune, distToPlayer);
            ApplyBondTier(kitsune);

            int max = player.GetMaxHealth();
            float pct = max > 0 ? (float)player.Health / max : 1f;
            int bondTier = BondRules.Tier(GetBondPoints(kitsune));
            string desired = TemperamentRules.Choose(pct, !world.IsDaytime(), bondTier);

            ApplyTemperament(kitsune, desired);
            UpdateFollow(kitsune, player, distToPlayer);
        }

        // ---------- Bond ----------

        private static float GetBondPoints(EntityAlive kitsune)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return 0f;
            if (!buffs.HasCustomVar(BondRules.CvarBondPoints)) return 0f;
            return buffs.GetCustomVar(BondRules.CvarBondPoints);
        }

        private static void AddBondPoints(EntityAlive kitsune, float delta)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return;
            float clamped = BondRules.ClampDelta(GetBondPoints(kitsune), delta);
            if (clamped == 0f) return;
            buffs.IncrementCustomVar(BondRules.CvarBondPoints, clamped);
        }

        private static void AccrueBond(EntityAlive kitsune, float distToPlayer)
        {
            if (distToPlayer > BondAccrualRange) return;
            AddBondPoints(kitsune, BondRules.BondPerTick);
        }

        private static void TryConsumeBondCharm(EntityAlive kitsune, EntityPlayer player, float distToPlayer)
        {
            if (distToPlayer > TalismanRange) return;
            if (player.bag == null) return;

            var charm = ItemClass.GetItem(BondRules.BondCharmItem, false);
            if (charm == null || charm.IsEmpty()) return;

            int count = player.bag.GetItemCount(charm, -1, -1, false);
            if (count <= 0) return;

            player.bag.DecItem(charm, 1, false, null);
            AddBondPoints(kitsune, BondRules.BondPerCharm);
            Log.Out($"[KitsuneCompanion] {KitsuneNames.GetName(kitsune.entityId)}: +{BondRules.BondPerCharm} bond from charm (total {GetBondPoints(kitsune):F1})");
        }

        private static void ApplyBondTier(EntityAlive kitsune)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return;

            int tier = BondRules.Tier(GetBondPoints(kitsune));
            string desired = BondRules.BuffForTier(tier);

            for (int i = 0; i < BondRules.AllBondBuffs.Length; i++)
            {
                var name = BondRules.AllBondBuffs[i];
                bool has = buffs.HasBuff(name);
                if (name == desired)
                {
                    if (!has) buffs.AddBuff(desired, -1, true, false, -1f);
                }
                else if (has)
                {
                    buffs.RemoveBuff(name, true);
                }
            }
        }

        // ---------- Forms ----------

        private static void TryConsumeFormTalisman(EntityAlive kitsune, EntityPlayer player, float distToPlayer)
        {
            if (distToPlayer > TalismanRange) return;
            if (player.bag == null) return;

            for (int i = 0; i < EvolutionRules.AllForms.Length; i++)
            {
                string form = EvolutionRules.AllForms[i];
                string talismanName = TalismanForForm(form);
                if (talismanName == null) continue;

                var itemValue = ItemClass.GetItem(talismanName, false);
                if (itemValue == null || itemValue.IsEmpty()) continue;

                int count = player.bag.GetItemCount(itemValue, -1, -1, false);
                if (count <= 0) continue;

                if (kitsune.Buffs.HasBuff(form)) continue;

                player.bag.DecItem(itemValue, 1, false, null);
                ApplyForm(kitsune, form);
                Log.Out($"[KitsuneCompanion] {KitsuneNames.GetName(kitsune.entityId)} bonded to {form} via {talismanName}");
                return;
            }
        }

        private static string TalismanForForm(string form)
        {
            if (form == EvolutionRules.FormMist) return EvolutionRules.TalismanMist;
            return null;
        }

        private static void ApplyForm(EntityAlive kitsune, string form)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return;

            for (int i = 0; i < EvolutionRules.AllForms.Length; i++)
            {
                var existing = EvolutionRules.AllForms[i];
                if (existing != form && buffs.HasBuff(existing))
                    buffs.RemoveBuff(existing, true);
            }
            if (!buffs.HasBuff(form))
                buffs.AddBuff(form, -1, true, false, -1f);
        }

        private static string ActiveForm(EntityAlive kitsune)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return null;
            for (int i = 0; i < EvolutionRules.AllForms.Length; i++)
            {
                if (buffs.HasBuff(EvolutionRules.AllForms[i]))
                    return EvolutionRules.AllForms[i];
            }
            return null;
        }

        // ---------- Temperament ----------

        private static void ApplyTemperament(EntityAlive kitsune, string desired)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return;

            for (int i = 0; i < TemperamentRules.All.Length; i++)
            {
                var name = TemperamentRules.All[i];
                bool has = buffs.HasBuff(name);
                if (name == desired)
                {
                    if (!has) buffs.AddBuff(desired, -1, true, false, -1f);
                }
                else if (has)
                {
                    buffs.RemoveBuff(name, true);
                }
            }
        }

        private static void ClearAllTemperaments(EntityAlive kitsune)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return;
            for (int i = 0; i < TemperamentRules.All.Length; i++)
            {
                if (buffs.HasBuff(TemperamentRules.All[i]))
                    buffs.RemoveBuff(TemperamentRules.All[i], true);
            }
        }

        // ---------- Follow ----------

        private static void UpdateFollow(EntityAlive kitsune, EntityPlayer player, float distToPlayer)
        {
            float teleport = EvolutionRules.GetTeleportDistance(ActiveForm(kitsune));

            if (distToPlayer > teleport)
            {
                kitsune.SetPosition(player.position + TeleportOffset(player), true);
                if (kitsune.moveHelper != null) kitsune.moveHelper.Stop();
                return;
            }

            if (distToPlayer > FollowStartDistance)
            {
                // SetInvestigatePosition feeds the ApproachSpot AI task
                // (priority 5 on our entity, above Wander at 7) so the AI
                // loop itself drives movement toward the player. Calling
                // moveHelper.SetMoveTo directly loses to AI overrides every
                // frame; using the AI's own target slot wins.
                //
                // Duration 600 ticks (~30s) gives plenty of buffer; we
                // re-set every 2s anyway so it never actually expires
                // mid-pursuit.
                kitsune.SetInvestigatePosition(player.position, 600, false);
                if (kitsune.moveHelper != null)
                    kitsune.moveHelper.SetMoveTo(player.position, false);
            }
            else if (kitsune.moveHelper != null)
            {
                kitsune.moveHelper.Stop();
                kitsune.ClearInvestigatePosition();
            }
        }

        // Place the kitsune behind-right of the player relative to the player's
        // facing direction rather than always at world-space +X. Greatly reduces
        // odds of teleporting into a wall when the player is pressed against
        // geometry on the +X side.
        private static Vector3 TeleportOffset(EntityPlayer player)
        {
            var t = player.transform;
            if (t == null) return new Vector3(1.5f, 0f, 0f);

            Vector3 fwd = t.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) return new Vector3(1.5f, 0f, 0f);
            fwd.Normalize();
            Vector3 right = t.right; right.y = 0f; right.Normalize();

            return -fwd * 1.0f + right * 1.5f;
        }
    }
}
