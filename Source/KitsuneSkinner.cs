using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KitsuneCompanion
{
    // Per-instance Renderer.material clone + texture swap.
    // Runs once per kitsune per session; uses an in-memory entityId set so the
    // marker doesn't persist into save data (where it would lie about renderer
    // state on the next session).
    internal static class KitsuneSkinner
    {
        // Material-name substring -> filename in <modroot>/Resources/Textures/.
        // Match is case-insensitive substring. Empty by default; populate once
        // the runtime log identifies the coyote body material name.
        private static readonly Dictionary<string, string> TextureMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Vanilla coyote body Material is named exactly "coyote";
                // its _MainTex is the 1024x1024 "coyote" diffuse. Substring
                // match catches both "coyote" and "coyote (Instance)" after
                // Renderer.materials clones it per-instance.
                { "coyote", "kitsuneCoyoteBody.png" },
            };

        private static readonly string[] TextureProps =
            { "_MainTex", "_BaseMap", "_BaseColorMap" };

        private static readonly HashSet<int> _skinnedThisSession = new HashSet<int>();
        private static bool _loggedOnce;

        public static void EnsureSkinned(EntityAlive kitsune, string modPath)
        {
            if (kitsune == null) return;
            if (_skinnedThisSession.Contains(kitsune.entityId)) return;

            var root = kitsune.RootTransform;
            if (root == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            int swapped = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;

                var sharedMats = r.sharedMaterials;
                if (sharedMats == null) continue;

                bool needsClone = false;
                for (int m = 0; m < sharedMats.Length; m++)
                {
                    var sm = sharedMats[m];
                    if (sm == null) continue;
                    if (!_loggedOnce)
                        Log.Out($"[KitsuneCompanion] Skin probe: Renderer[{i}={r.name}].mat[{m}]={sm.name}");
                    if (TryGetTextureFile(sm.name, out _)) needsClone = true;
                }

                if (!needsClone) continue;

                // Renderer.materials [plural] returns per-instance copies that
                // this renderer alone owns. Wild coyotes keep the shared vanilla
                // material; only this kitsune is repainted.
                var instMats = r.materials;
                for (int m = 0; m < instMats.Length; m++)
                {
                    var im = instMats[m];
                    if (im == null) continue;
                    if (!TryGetTextureFile(im.name, out var fn)) continue;

                    var path = Path.Combine(modPath, "Resources", "Textures", fn);
                    if (!File.Exists(path))
                    {
                        Log.Out($"[KitsuneCompanion] Skin: PNG not found at {path}");
                        continue;
                    }

                    var tex = LoadTexture(path);
                    if (tex == null) continue;

                    for (int p = 0; p < TextureProps.Length; p++)
                    {
                        if (im.HasProperty(TextureProps[p]))
                            im.SetTexture(TextureProps[p], tex);
                    }
                    swapped++;
                }
            }

            _loggedOnce = true;
            _skinnedThisSession.Add(kitsune.entityId);
            if (swapped > 0)
                Log.Out($"[KitsuneCompanion] Skinned kitsune {kitsune.entityId}: {swapped} material(s) swapped");
        }

        private static bool TryGetTextureFile(string materialName, out string filename)
        {
            filename = null;
            if (string.IsNullOrEmpty(materialName)) return false;
            foreach (var kvp in TextureMap)
            {
                if (materialName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filename = kvp.Value;
                    return true;
                }
            }
            return false;
        }

        private static Texture2D LoadTexture(string path)
        {
            try
            {
                var data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!tex.LoadImage(data, false)) return null;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply(true, false);
                return tex;
            }
            catch (Exception e)
            {
                Log.Out($"[KitsuneCompanion] Skin: failed to load {path}: {e.Message}");
                return null;
            }
        }
    }
}
