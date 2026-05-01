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
        private const float CacheLifetimeSeconds = 0.5f;
        private const float DirtyRefreshInterval = 1f;

        private float _cacheUntil;
        private float _nextDirty;
        private EntityAlive _cachedKitsune;
        private string _cachedName = "";
        private string _cachedStatus = "";
        private bool _cachedHasKitsune;
        private int _diagLogCount;

        public override void Update(float _dt)
        {
            base.Update(_dt);
            // XUi only re-resolves bindings when children are marked dirty.
            // Push a dirty flag every second so the widget reflects live state
            // (temperament shifting at night, bond accruing, form bonding).
            float now = Time.time;
            if (now < _nextDirty) return;
            _nextDirty = now + DirtyRefreshInterval;
            SetAllChildrenDirty(true);
        }

        public override bool GetBindingValueInternal(ref string value, string bindingName)
        {
            EnsureFresh();
            switch (bindingName)
            {
                case "kitsuneName":
                    value = _cachedName;
                    return true;
                case "kitsuneStatus":
                    value = _cachedStatus;
                    return true;
                case "kitsuneVisible":
                    value = _cachedHasKitsune ? "true" : "false";
                    return true;
            }
            return base.GetBindingValueInternal(ref value, bindingName);
        }

        private void EnsureFresh()
        {
            float now = Time.time;
            if (now < _cacheUntil) return;
            _cacheUntil = now + CacheLifetimeSeconds;

            _cachedKitsune = FindMyKitsune();
            if (_cachedKitsune == null)
            {
                _cachedHasKitsune = false;
                _cachedName = "";
                _cachedStatus = "";
                return;
            }

            _cachedHasKitsune = true;
            _cachedName = KitsuneNames.GetName(_cachedKitsune.entityId);
            _cachedStatus = BuildStatusLine(_cachedKitsune);

            // Throttled diag (~5s cadence) to confirm what the controller
            // actually computes vs what the heavy tick claims is on the entity.
            if (_diagLogCount++ % 10 == 0)
            {
                var b = _cachedKitsune.Buffs;
                bool hasSerene    = b != null && b.HasBuff(TemperamentRules.BuffSerene);
                bool hasCurious   = b != null && b.HasBuff(TemperamentRules.BuffCurious);
                bool hasPlayful   = b != null && b.HasBuff(TemperamentRules.BuffPlayful);
                bool hasProtective = b != null && b.HasBuff(TemperamentRules.BuffProtective);
                Log.Out($"[KitsuneCompanion] hud.diag: id={_cachedKitsune.entityId} name={_cachedName} status='{_cachedStatus}' Serene={hasSerene} Curious={hasCurious} Playful={hasPlayful} Protective={hasProtective}");
            }
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
