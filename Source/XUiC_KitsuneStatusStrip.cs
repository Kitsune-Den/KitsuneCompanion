using UnityEngine;

namespace KitsuneCompanion
{
    // XUi controller for the in-game HUD status strip widget defined in
    // Config/XUi/windows.xml. Resolves binding tokens like {kitsuneName}
    // by finding the local player's nearest kitsune and reading bond /
    // temperament / form state from it. Refreshed by XUi on a regular
    // cadence; we cache results between resolutions in case multiple
    // bindings hit in the same frame.
    public class XUiC_KitsuneStatusStrip : XUiController
    {
        private const string EntityClassName = "kitsuneCompanion";
        private const float SearchRadius = 80f;
        private const float DirtyRefreshInterval = 0.5f;

        private float _nextDirty;
        private int _updateCount;
        private int _diagLogCount;

        public override void Init()
        {
            base.Init();
            Log.Out("[KitsuneCompanion] hud.init: controller instantiated");
        }

        public override void Update(float _dt)
        {
            base.Update(_dt);
            _updateCount++;

            float now = Time.time;
            if (now < _nextDirty) return;
            _nextDirty = now + DirtyRefreshInterval;

            // Try a few different ways to push a refresh; one will hit.
            try { RefreshBindingsSelfAndChildren(); } catch { }
            try { SetAllChildrenDirty(true); } catch { }

            // Log Update activity every ~3s to confirm it fires at all.
            if (_updateCount % 180 == 1)
                Log.Out($"[KitsuneCompanion] hud.update: count={_updateCount} fired");
        }

        public override bool GetBindingValueInternal(ref string value, string bindingName)
        {
            // No cache — compute every call. Any cache adds a window where
            // bindings show stale data; the resolution is cheap.
            var kitsune = FindMyKitsune();
            bool has = kitsune != null;
            string name = "";
            string status = "";
            if (has)
            {
                name = KitsuneNames.GetName(kitsune.entityId);
                status = BuildStatusLine(kitsune);
            }

            // Per-resolution diag (throttled ~once per 10 resolutions).
            if (_diagLogCount++ % 30 == 0 && has)
            {
                var b = kitsune.Buffs;
                bool s = b != null && b.HasBuff(TemperamentRules.BuffSerene);
                bool c = b != null && b.HasBuff(TemperamentRules.BuffCurious);
                bool pl = b != null && b.HasBuff(TemperamentRules.BuffPlayful);
                bool pr = b != null && b.HasBuff(TemperamentRules.BuffProtective);
                Log.Out($"[KitsuneCompanion] hud.diag: bind={bindingName} id={kitsune.entityId} status='{status}' S={s} C={c} P={pl} Pr={pr}");
            }

            switch (bindingName)
            {
                case "kitsuneName":   value = name;                  return true;
                case "kitsuneStatus": value = status;                return true;
                case "kitsuneVisible": value = has ? "true" : "false"; return true;
            }
            return base.GetBindingValueInternal(ref value, bindingName);
        }

        private static EntityAlive FindMyKitsune()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            var world = gm.World;
            if (world == null) return null;

            // Local player POV — only the player whose UI this is matters.
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

        // Status line: "Curious - Trusted 60%" or "Curious" if Faint (no tier).
        // Form (Mist/Ember/...) prepended when active: "Mist · Curious - Trusted 60%".
        private static string BuildStatusLine(EntityAlive kitsune)
        {
            var buffs = kitsune.Buffs;
            if (buffs == null) return "";

            string temperament = ResolveTemperamentLabel(buffs);
            string tierWithProgress = ResolveTierWithProgress(buffs);
            string form = ResolveFormLabel(buffs);

            string body = string.IsNullOrEmpty(tierWithProgress)
                ? temperament
                : $"{temperament} - {tierWithProgress}";

            if (!string.IsNullOrEmpty(form))
                return $"{form} · {body}"; // middle dot
            return body;
        }

        private static string ResolveTemperamentLabel(EntityBuffs buffs)
        {
            if (buffs.HasBuff(TemperamentRules.BuffPlayful))    return "Playful";
            if (buffs.HasBuff(TemperamentRules.BuffSerene))     return "Serene";
            if (buffs.HasBuff(TemperamentRules.BuffProtective)) return "Protective";
            if (buffs.HasBuff(TemperamentRules.BuffCurious))    return "Curious";
            return "";
        }

        private static string ResolveTierWithProgress(EntityBuffs buffs)
        {
            float points = 0f;
            if (buffs.HasCustomVar(BondRules.CvarBondPoints))
                points = buffs.GetCustomVar(BondRules.CvarBondPoints);
            int tier = BondRules.Tier(points);
            if (tier == 0) return ""; // Faint, no display

            string name;
            switch (tier)
            {
                case 1: name = "Familiar"; break;
                case 2: name = "Trusted";  break;
                case 3: name = "Bound";    break;
                case 4: name = "Kindred";  break;
                default: return "";
            }

            int pct = Mathf.Clamp((int)(BondRules.TierProgress(points) * 100f + 0.5f), 0, 100);
            return $"{name} {pct}%";
        }

        private static string ResolveFormLabel(EntityBuffs buffs)
        {
            if (buffs.HasBuff(EvolutionRules.FormMist)) return "Mist";
            return ""; // Manifested = no specialization, hide from compact strip
        }
    }
}
