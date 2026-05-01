using UnityEngine;

namespace KitsuneCompanion
{
    // XUi controller for the Kitsune Companion sidebar in the inventory/
    // character menu. Mirrors XUiC_KitsuneStatusStrip's lookup logic but
    // exposes split bindings for the larger row-per-stat layout, and shows
    // "Manifested" / "Faint" placeholders the compact HUD strip suppresses.
    public class XUiC_KitsuneCompanionPanel : XUiController
    {
        private const string EntityClassName = "kitsuneCompanion";
        private const float SearchRadius = 80f;
        private const float DirtyRefreshInterval = 0.5f;

        private const string Placeholder = "—";

        private float _nextDirty;

        public override void Update(float _dt)
        {
            base.Update(_dt);

            float now = Time.time;
            if (now < _nextDirty) return;
            _nextDirty = now + DirtyRefreshInterval;

            try { RefreshBindingsSelfAndChildren(); } catch { }
            try { SetAllChildrenDirty(true); } catch { }
        }

        public override bool GetBindingValueInternal(ref string value, string bindingName)
        {
            var kitsune = FindMyKitsune();
            bool has = kitsune != null;

            switch (bindingName)
            {
                case "kitsunePanelName":
                    value = has ? KitsuneNames.GetName(kitsune.entityId) : Placeholder;
                    return true;
                case "kitsunePanelForm":
                    value = has ? ResolveFormLabel(kitsune.Buffs) : Placeholder;
                    return true;
                case "kitsunePanelTemperament":
                    value = has ? ResolveTemperamentLabel(kitsune.Buffs) : Placeholder;
                    return true;
                case "kitsunePanelBond":
                    value = has ? ResolveBondLabel(kitsune.Buffs) : Placeholder;
                    return true;
            }
            return base.GetBindingValueInternal(ref value, bindingName);
        }

        private static EntityAlive FindMyKitsune()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            var world = gm.World;
            if (world == null) return null;

            var locals = world.GetLocalPlayers();
            if (locals == null || locals.Count == 0) return null;
            var player = locals[0];
            if (player == null) return null;

            var alives = world.EntityAlives;
            if (alives == null) return null;

            EntityAlive best = null;
            float bestSq = SearchRadius * SearchRadius;
            for (int i = 0; i < alives.Count; i++)
            {
                var a = alives[i];
                if (a == null || a.entityClass == 0) continue;
                if (EntityClass.GetEntityClassName(a.entityClass) != EntityClassName) continue;
                float d = (a.position - player.position).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = a;
                }
            }
            return best;
        }

        private static string ResolveFormLabel(EntityBuffs buffs)
        {
            if (buffs == null) return "Manifested";
            if (buffs.HasBuff(EvolutionRules.FormMist)) return "Mist";
            return "Manifested";
        }

        private static string ResolveTemperamentLabel(EntityBuffs buffs)
        {
            if (buffs == null) return Placeholder;
            if (buffs.HasBuff(TemperamentRules.BuffPlayful))    return "Playful";
            if (buffs.HasBuff(TemperamentRules.BuffSerene))     return "Serene";
            if (buffs.HasBuff(TemperamentRules.BuffProtective)) return "Protective";
            if (buffs.HasBuff(TemperamentRules.BuffCurious))    return "Curious";
            return Placeholder;
        }

        private static string ResolveBondLabel(EntityBuffs buffs)
        {
            if (buffs == null) return "Faint";

            float points = 0f;
            if (buffs.HasCustomVar(BondRules.CvarBondPoints))
                points = buffs.GetCustomVar(BondRules.CvarBondPoints);

            int tier = BondRules.Tier(points);
            if (tier >= 4) return "Kindred"; // max tier, no progress %

            int pct = Mathf.Clamp((int)(BondRules.TierProgress(points) * 100f + 0.5f), 0, 100);

            // Tier 0 (Faint) is "no bond yet" — show progress toward the FIRST
            // tier so "50%" doesn't read as "halfway to Kindred" globally.
            // Tiers 1-3 show "current tier %", which reads as progress within
            // that tier toward the next.
            switch (tier)
            {
                case 0: return $"Faint → Familiar {pct}%";
                case 1: return $"Familiar {pct}%";
                case 2: return $"Trusted {pct}%";
                case 3: return $"Bound {pct}%";
                default: return "Faint";
            }
        }
    }
}
