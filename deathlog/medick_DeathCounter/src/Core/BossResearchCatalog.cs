using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace medick_DeathCounter.Core
{
    internal sealed class BossResearchLoadResult
    {
        public IReadOnlyList<BossProfile> Profiles { get; }
        public CatalogDocument Document { get; }
        public IReadOnlyList<string> Diagnostics { get; }

        public BossResearchLoadResult(IReadOnlyList<BossProfile> profiles, CatalogDocument document, IEnumerable<string> diagnostics)
        {
            Profiles = Array.AsReadOnly((profiles ?? Array.Empty<BossProfile>()).ToArray());
            Document = BossResearchCatalog.Clone(document);
            Diagnostics = Array.AsReadOnly((diagnostics ?? Enumerable.Empty<string>()).ToArray());
        }
    }

    internal static class BossResearchCatalog
    {
        public const string ResourceName = "medick_DeathCounter.BossResearchCatalog.json";

        public static BossResearchLoadResult LoadEmbedded(Assembly assembly, string resourceName, IReadOnlyList<BossProfile> fallback)
        {
            var diagnostics = new List<string>();
            try
            {
                using (var stream = assembly?.GetManifestResourceStream(resourceName ?? ResourceName))
                {
                    if (stream == null)
                        return Fallback(fallback, "Embedded boss research catalog resource was not found: " + (resourceName ?? ResourceName));
                    using (var reader = new StreamReader(stream))
                        return LoadJson(reader.ReadToEnd(), fallback);
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add("Embedded boss research catalog could not be read: " + ex.Message);
                diagnostics.Add("Using the Starter boss catalog.");
                return Fallback(fallback, diagnostics.ToArray());
            }
        }

        public static BossResearchLoadResult LoadJson(string json, IReadOnlyList<BossProfile> fallback)
        {
            IReadOnlyList<string> validation;
            CatalogDocument document;
            try
            {
                validation = BossCatalogSchema.ValidateJson(json);
                var errors = validation.Where(e => !IsAliasDuplicate(e)).ToArray();
                if (errors.Length != 0)
                    return Fallback(fallback, new[] { "Embedded boss research catalog failed validation: " + string.Join("; ", errors), "Using the Starter boss catalog." });

                document = JsonSerializer.Deserialize<CatalogDocument>(json, BossCatalogSchema.Json);
                if (document?.Encounters == null || document.Encounters.Count == 0)
                    return Fallback(fallback, new[] { "Embedded boss research catalog contains no encounters.", "Using the Starter boss catalog." });

                var profiles = document.Encounters.Select(e => new BossProfile(e)).ToArray();
                var diagnostics = validation.Where(IsAliasDuplicate).Distinct(StringComparer.Ordinal).ToList();
                if (diagnostics.Count > 0) diagnostics.Insert(0, "Some aliases are ambiguous; exact-name lookup rejects those aliases.");
                return new BossResearchLoadResult(profiles, document, diagnostics);
            }
            catch (Exception ex)
            {
                return Fallback(fallback, new[] { "Embedded boss research catalog could not be loaded: " + ex.Message, "Using the Starter boss catalog." });
            }
        }

        public static EncounterRecord Clone(EncounterRecord encounter)
        {
            if (encounter == null) return null;
            string json = JsonSerializer.Serialize(encounter, BossCatalogSchema.Json);
            return JsonSerializer.Deserialize<EncounterRecord>(json, BossCatalogSchema.Json);
        }

        public static CatalogDocument Clone(CatalogDocument document)
        {
            if (document == null) return null;
            string json = JsonSerializer.Serialize(document, BossCatalogSchema.Json);
            return JsonSerializer.Deserialize<CatalogDocument>(json, BossCatalogSchema.Json);
        }

        static bool IsAliasDuplicate(string diagnostic) =>
            diagnostic != null && diagnostic.EndsWith(" alias is duplicated", StringComparison.Ordinal);

        static BossResearchLoadResult Fallback(IReadOnlyList<BossProfile> fallback, string diagnostic) =>
            Fallback(fallback, new[] { diagnostic });

        static BossResearchLoadResult Fallback(IReadOnlyList<BossProfile> fallback, IEnumerable<string> diagnostics)
        {
            var starter = (fallback ?? Array.Empty<BossProfile>()).ToArray();
            var projected = new CatalogDocument
            {
                SchemaVersion = BossCatalogSchema.Version,
                Reviewed = BossCatalog.Reviewed,
                Encounters = starter.Select(BossCatalogSchema.Project).ToList()
            };
            return new BossResearchLoadResult(starter, projected, diagnostics);
        }
    }
}
