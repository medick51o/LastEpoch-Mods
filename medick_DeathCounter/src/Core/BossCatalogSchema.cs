using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace medick_DeathCounter.Core
{
    // Versioned catalog document. Projects the starter profiles already in
    // BossCatalog and refuses to invent roster facts, damage, or death labels.
    public static class BossCatalogSchema
    {
        public const string Version = "1.0.0";
        public static readonly string[] BrowseModes = { "timeline", "boss", "harbinger", "dungeon" };
        public static readonly string[] EvidenceClasses = { "unknown", "captured-game", "datamined-reference", "developer-documentation", "tactical-inference" };
        public static readonly string[] PositiveEvidence = { "captured-game", "datamined-reference", "developer-documentation" };
        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true
        };

        public static CatalogDocument ProjectBaseline() => new()
        {
            SchemaVersion = Version,
            Reviewed = BossCatalog.Reviewed,
            Encounters = BossCatalog.All.Select(Project).ToList()
        };

        public static EncounterRecord Project(BossProfile profile)
        {
            if (profile == null) return null;
            var encounter = new EncounterRecord
            {
                Id = profile.Id,
                DisplayName = profile.Name,
                Aliases = profile.Aliases.Select(a => new AliasRecord { Text = a, Locale = LocaleField.Unknown() }).ToList(),
                Parent = ParentRef.Unknown(),
                Variants = CatalogList<VariantRecord>.Unknown(),
                Locations = CatalogList<LocationRecord>.Unknown(),
                Abilities = CatalogList<AbilityRecord>.Unknown(),
                Ailments = CatalogList<AilmentRecord>.Unknown(),
                BrowseGroups = CatalogList<string>.Unknown(),
                Tips = profile.Tips.Select(t => new TipRecord { Text = t, EvidenceClass = "tactical-inference", Confidence = ConfidenceField.Unknown() }).ToList(),
                Claims = new List<ClaimRecord>
                {
                    new() { Id = profile.Id + ".mechanics", Statement = profile.Mechanics, EvidenceClass = "unknown", Confidence = ConfidenceField.Unknown() },
                    new() { Id = profile.Id + ".damage-evidence", Statement = profile.DamageEvidence, EvidenceClass = "unknown", Confidence = ConfidenceField.Unknown() }
                }
            };
            for (int i = 0; i < profile.Sources.Count; i++)
            {
                var source = profile.Sources[i];
                encounter.Claims.Add(new ClaimRecord
                {
                    Id = profile.Id + ".source." + (i + 1),
                    Statement = source.Label,
                    EvidenceClass = EvidenceForUrl(source.Url),
                    Confidence = ConfidenceField.Unknown(),
                    Source = new SourceRef { Label = source.Label, Url = source.Url }
                });
            }
            encounter.DamageCoverage = profile.DamageTypes.Count == 0
                ? DamageCoverage.Unknown()
                : new DamageCoverage
                {
                    Status = "known",
                    Scope = "encounter",
                    Elements = profile.DamageTypes.Select(Elements.Name).ToList(),
                    PerAbility = CatalogList<DamageComponent>.Unknown(),
                    EvidenceClass = EvidenceForCoverage(profile),
                    Confidence = ConfidenceField.Unknown()
                };
            return encounter;
        }

        // An ability title is never a damage report. Void Rahyeh, Ice Spike and Lightning Blast stay untyped.
        public static bool AbilityNameEstablishesDamage(string abilityName, string elementName)
        {
            _ = abilityName;
            _ = elementName;
            return false;
        }

        public static EncounterRecord FindExact(CatalogDocument catalog, string attacker)
        {
            if (catalog?.Encounters == null || string.IsNullOrWhiteSpace(attacker)) return null;
            string name = attacker.Trim();
            return catalog.Encounters.FirstOrDefault(e => e?.Aliases != null && e.Aliases.Any(a =>
                a != null && string.Equals(a.Text?.Trim(), name, StringComparison.OrdinalIgnoreCase)));
        }

        public static EncounterLink Classify(CatalogDocument catalog, DeathRecord death)
        {
            if (death == null || catalog == null) return new EncounterLink { Kind = "unknown" };
            if (death.IsBossFight == false) return new EncounterLink { Kind = "not-boss" };
            var match = FindExact(catalog, death.Killer);
            if (death.IsBossFight == true && match != null) return new EncounterLink { Kind = "identified", EncounterId = match.Id };
            if (death.IsBossFight == true) return new EncounterLink { Kind = "confirmed-unidentified" };
            if (match != null) return new EncounterLink { Kind = "unconfirmed-guide", EncounterId = match.Id };
            return new EncounterLink { Kind = "unknown" };
        }

        // Same words the boss-tips tab uses. Manual browsing has its own selection and no caption here.
        public static string DeathLinkCaption(DeathRecord death)
        {
            if (death == null || (death.IsBossFight != true && BossCatalog.AttackerGuide(death) == null)) return null;
            var guide = BossCatalog.AttackerGuide(death);
            if (guide == null) return "Browse boss tips";
            return death.IsBossFight == true ? "Boss tips: " + guide.Name : "Attacker guide: " + guide.Name;
        }

        // Browse state is not written onto the death. encounterId is remembered only in the out value.
        public static bool TryBrowse(string mode, string encounterId, DeathRecord death, out string selectedId)
        {
            _ = death;
            selectedId = null;
            if (mode == null || !BrowseModes.Contains(mode) || string.IsNullOrWhiteSpace(encounterId)) return false;
            selectedId = encounterId.Trim();
            return true;
        }

        public static bool TryApplyCoverageToDeath(DeathRecord death, EncounterRecord encounter)
        {
            if (death == null || encounter == null) return false;
            return false;
        }

        public static IReadOnlyList<Element> AdviceElements(DeathRecord death, EncounterRecord encounter)
        {
            _ = encounter;
            return BossCatalog.ObservedTypes(death);
        }

        public static IReadOnlyList<string> ParentElementsAppliedToFollowUp(EncounterRecord parent, EncounterRecord followUp)
        {
            if (parent == null || followUp == null) return Array.Empty<string>();
            return Array.Empty<string>();
        }

        public static PreparationReport Prepare(EncounterRecord recorded, EncounterRecord followUp, IReadOnlyCollection<Element> cappedOnRecordedDeath)
        {
            var open = new List<string>();
            var capped = new HashSet<string>(cappedOnRecordedDeath?.Select(Elements.Name) ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (recorded == null) open.Add("recorded encounter: missing");
            else DescribeRecorded(recorded, capped, open);
            if (followUp != null) DescribeFollowUp(followUp, open);
            return new PreparationReport { FullyPrepared = open.Count == 0, OpenThreats = open };
        }

        public static IReadOnlyList<string> Validate(CatalogDocument catalog) => Semantic(catalog);

        public static IReadOnlyList<string> ValidateJson(string json)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(json)) return new[] { "catalog json is empty" };
            JsonDocument parsed;
            try { parsed = JsonDocument.Parse(json); }
            catch (JsonException ex) { return new[] { "catalog json could not be parsed: " + ex.Message }; }
            using (parsed)
            {
                WalkDocument(parsed.RootElement, errors);
                if (errors.Count > 0) return errors;
            }
            CatalogDocument catalog;
            try { catalog = JsonSerializer.Deserialize<CatalogDocument>(json, Json); }
            catch (JsonException ex) { return new[] { "catalog json could not be parsed: " + ex.Message }; }
            errors.AddRange(Semantic(catalog));
            return errors;
        }

        static void DescribeRecorded(EncounterRecord encounter, HashSet<string> capped, List<string> open)
        {
            var coverage = encounter.DamageCoverage;
            if (coverage == null || coverage.Status != "known" || coverage.Elements == null || coverage.Elements.Count == 0)
                open.Add("recorded encounter: unknown damage coverage");
            else
            {
                foreach (var element in coverage.Elements)
                    if (!capped.Contains(element)) open.Add("recorded encounter: " + element + " is not closed by this death");
                if (coverage.Confidence == null || coverage.Confidence.Status != "known")
                    open.Add("recorded encounter: damage coverage confidence unknown");
                if (coverage.PerAbility == null || coverage.PerAbility.Status != "known" || coverage.PerAbility.Items == null || coverage.PerAbility.Items.Count == 0)
                    open.Add("recorded encounter: per-ability damage unknown");
                else
                    foreach (var component in coverage.PerAbility.Items)
                    {
                        if (component == null || string.IsNullOrEmpty(component.Element) || !capped.Contains(component.Element))
                            open.Add("recorded encounter: a damage component is not closed by this death");
                        if (component?.Confidence == null || component.Confidence.Status != "known")
                            open.Add("recorded encounter: damage component confidence unknown");
                    }
            }
            if (encounter.Abilities == null || encounter.Abilities.Status != "known") open.Add("recorded encounter: abilities unknown");
            if (encounter.Ailments == null || encounter.Ailments.Status != "known") open.Add("recorded encounter: ailments unknown");
            if (encounter.Variants == null || encounter.Variants.Status != "known") open.Add("recorded encounter: variants unknown");
        }

        static void DescribeFollowUp(EncounterRecord followUp, List<string> open)
        {
            var coverage = followUp.DamageCoverage;
            if (coverage == null || coverage.Status != "known" || coverage.Elements == null || coverage.Elements.Count == 0)
                open.Add("follow-up: unknown additional threats");
            else
                foreach (var element in coverage.Elements)
                    open.Add("follow-up: " + element + " is not closed by the parent death");
            if (followUp.Abilities == null || followUp.Abilities.Status != "known") open.Add("follow-up: abilities unknown");
            if (followUp.Ailments == null || followUp.Ailments.Status != "known") open.Add("follow-up: ailments unknown");
        }

        static string EvidenceForUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "unknown";
            if (url.IndexOf("://forum.lastepoch.com", StringComparison.OrdinalIgnoreCase) >= 0) return "developer-documentation";
            if (url.IndexOf("://lastepoch.tunklab.com", StringComparison.OrdinalIgnoreCase) >= 0) return "datamined-reference";
            return "unknown";
        }

        static string EvidenceForCoverage(BossProfile profile)
        {
            if (profile.Sources.Any(s => EvidenceForUrl(s.Url) == "datamined-reference")) return "datamined-reference";
            if (profile.Sources.Any(s => EvidenceForUrl(s.Url) == "developer-documentation")) return "developer-documentation";
            return "unknown";
        }

        static readonly Regex EncounterIdPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex ClaimIdPattern = new("^[a-z0-9]+([.-][a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly HashSet<string> Statuses = new(StringComparer.Ordinal) { "unknown", "absent", "known" };
        static readonly HashSet<string> Levels = new(StringComparer.Ordinal) { "low", "medium", "high" };
        static readonly HashSet<string> Relationships = new(StringComparer.Ordinal) { "alternate", "empowered", "tier", "reskin", "transformed", "inherited-move" };
        static readonly HashSet<string> Deliveries = new(StringComparer.Ordinal) { "hit", "dot", "mixed", "unknown" };
        static readonly Dictionary<string, string[]> Shapes = new(StringComparer.Ordinal)
        {
            ["document"] = new[] { "schemaVersion", "reviewed", "encounters" },
            ["encounter"] = new[] { "id", "displayName", "aliases", "parent", "variants", "locations", "abilities", "damageCoverage", "ailments", "browseGroups", "tips", "claims" },
            ["alias"] = new[] { "text", "locale" },
            ["locale"] = new[] { "status", "tag" },
            ["parent"] = new[] { "status", "encounterId" },
            ["list"] = new[] { "status", "items" },
            ["coverage"] = new[] { "status", "scope", "elements", "perAbility", "evidenceClass", "confidence" },
            ["confidence"] = new[] { "status", "level" },
            ["tip"] = new[] { "text", "evidenceClass", "confidence" },
            ["claim"] = new[] { "id", "statement", "evidenceClass", "confidence", "source" },
            ["source"] = new[] { "label", "url" },
            ["variant"] = new[] { "id", "displayName", "relationship" },
            ["location"] = new[] { "displayName", "sceneId", "localizedName" },
            ["ability"] = new[] { "id", "displayName", "internalName", "inheritedFromEncounterId" },
            ["ailment"] = new[] { "name", "effect" },
            ["component"] = new[] { "abilityId", "element", "delivery", "evidenceClass", "confidence" }
        };

        static void Walk(JsonElement el, string shape, string path, List<string> errors)
        {
            if (el.ValueKind != JsonValueKind.Object)
            {
                errors.Add(path + " must be an object");
                return;
            }
            var allowed = Shapes[shape];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prop in el.EnumerateObject())
            {
                if (!seen.Add(prop.Name)) errors.Add(path + "." + prop.Name + " is duplicated");
                if (!allowed.Contains(prop.Name)) errors.Add(path + "." + prop.Name + " is not part of schema " + Version);
            }
            foreach (var name in allowed)
                if (!seen.Contains(name)) errors.Add(path + " is missing " + name);
        }

        static void WalkDocument(JsonElement root, List<string> errors)
        {
            Walk(root, "document", "$", errors);
            if (root.ValueKind != JsonValueKind.Object) return;
            if (!root.TryGetProperty("encounters", out var encounters) || encounters.ValueKind != JsonValueKind.Array)
            {
                errors.Add("$.encounters must be an array");
                return;
            }
            int index = 0;
            foreach (var encounter in encounters.EnumerateArray())
            {
                string path = "$.encounters[" + index + "]";
                Walk(encounter, "encounter", path, errors);
                if (encounter.ValueKind == JsonValueKind.Object)
                {
                    WalkArray(encounter, "aliases", "alias", path, errors);
                    WalkChild(encounter, "parent", "parent", path, errors, true);
                    WalkList(encounter, "variants", "variant", path, errors);
                    WalkList(encounter, "locations", "location", path, errors);
                    WalkList(encounter, "abilities", "ability", path, errors);
                    WalkChild(encounter, "damageCoverage", "coverage", path, errors, true);
                    WalkList(encounter, "ailments", "ailment", path, errors);
                    WalkList(encounter, "browseGroups", "string", path, errors);
                    WalkArray(encounter, "tips", "tip", path, errors);
                    WalkArray(encounter, "claims", "claim", path, errors);
                    if (encounter.TryGetProperty("aliases", out var aliases) && aliases.ValueKind == JsonValueKind.Array)
                        for (int a = 0; a < aliases.GetArrayLength(); a++)
                            if (aliases[a].ValueKind == JsonValueKind.Object) WalkChild(aliases[a], "locale", "locale", path + ".aliases[" + a + "]", errors, true);
                    if (encounter.TryGetProperty("damageCoverage", out var coverage) && coverage.ValueKind == JsonValueKind.Object)
                    {
                        WalkChild(coverage, "confidence", "confidence", path + ".damageCoverage", errors, true);
                        WalkList(coverage, "perAbility", "component", path + ".damageCoverage", errors);
                    }
                    if (encounter.TryGetProperty("locations", out var locations) && locations.ValueKind == JsonValueKind.Object
                        && locations.TryGetProperty("items", out var locationItems) && locationItems.ValueKind == JsonValueKind.Array)
                        for (int n = 0; n < locationItems.GetArrayLength(); n++)
                            if (locationItems[n].ValueKind == JsonValueKind.Object)
                                WalkChild(locationItems[n], "localizedName", "locale", path + ".locations.items[" + n + "]", errors, true);
                    if (encounter.TryGetProperty("tips", out var tips) && tips.ValueKind == JsonValueKind.Array)
                        for (int t = 0; t < tips.GetArrayLength(); t++)
                            if (tips[t].ValueKind == JsonValueKind.Object) WalkChild(tips[t], "confidence", "confidence", path + ".tips[" + t + "]", errors, true);
                    if (encounter.TryGetProperty("claims", out var claims) && claims.ValueKind == JsonValueKind.Array)
                        for (int c = 0; c < claims.GetArrayLength(); c++)
                            if (claims[c].ValueKind == JsonValueKind.Object)
                            {
                                WalkChild(claims[c], "confidence", "confidence", path + ".claims[" + c + "]", errors, true);
                                WalkChild(claims[c], "source", "source", path + ".claims[" + c + "]", errors, false);
                            }
                }
                index++;
            }
        }

        static void WalkChild(JsonElement parent, string name, string shape, string path, List<string> errors, bool requiredObject)
        {
            if (!parent.TryGetProperty(name, out var child)) return;
            if (child.ValueKind == JsonValueKind.Null)
            {
                if (requiredObject) errors.Add(path + "." + name + " must be an object");
                return;
            }
            Walk(child, shape, path + "." + name, errors);
        }

        static void WalkArray(JsonElement parent, string name, string shape, string path, List<string> errors)
        {
            if (!parent.TryGetProperty(name, out var array)) return;
            if (array.ValueKind == JsonValueKind.Null) { errors.Add(path + "." + name + " must be an array"); return; }
            if (array.ValueKind != JsonValueKind.Array) { errors.Add(path + "." + name + " must be an array"); return; }
            for (int i = 0; i < array.GetArrayLength(); i++)
            {
                if (shape == "string")
                {
                    if (array[i].ValueKind != JsonValueKind.String) errors.Add(path + "." + name + "[" + i + "] must be a string");
                }
                else Walk(array[i], shape, path + "." + name + "[" + i + "]", errors);
            }
        }

        static void WalkList(JsonElement parent, string name, string itemShape, string path, List<string> errors)
        {
            if (!parent.TryGetProperty(name, out var list)) return;
            if (list.ValueKind != JsonValueKind.Object)
            {
                errors.Add(path + "." + name + " must be an object");
                return;
            }
            Walk(list, "list", path + "." + name, errors);
            if (!list.TryGetProperty("items", out var items) || items.ValueKind == JsonValueKind.Null) return;
            if (items.ValueKind != JsonValueKind.Array) { errors.Add(path + "." + name + ".items must be an array"); return; }
            for (int i = 0; i < items.GetArrayLength(); i++)
            {
                if (itemShape == "string")
                {
                    if (items[i].ValueKind != JsonValueKind.String) errors.Add(path + "." + name + ".items[" + i + "] must be a string");
                }
                else
                {
                    Walk(items[i], itemShape, path + "." + name + ".items[" + i + "]", errors);
                    if (itemShape == "component" && items[i].ValueKind == JsonValueKind.Object)
                        WalkChild(items[i], "confidence", "confidence", path + "." + name + ".items[" + i + "]", errors, true);
                }
            }
        }

        static IReadOnlyList<string> Semantic(CatalogDocument catalog)
        {
            var errors = new List<string>();
            if (catalog == null) return new[] { "catalog is missing" };
            if (catalog.SchemaVersion != Version) errors.Add("schemaVersion must be " + Version);
            Text(catalog.Reviewed, "reviewed", errors, true);
            if (catalog.Encounters == null) return errors.Append("encounters is missing").ToList();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var claimIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var encounter in catalog.Encounters)
            {
                if (encounter == null) { errors.Add("encounter is missing"); continue; }
                if (string.IsNullOrWhiteSpace(encounter.Id) || !EncounterIdPattern.IsMatch(encounter.Id))
                    errors.Add("encounter id is not a stable slug");
                else if (!ids.Add(encounter.Id)) errors.Add(encounter.Id + " is duplicated");
                Text(encounter.DisplayName, encounter.Id + " display name", errors, true);
                if (encounter.Aliases == null || encounter.Aliases.Count == 0) errors.Add(encounter.Id + " needs an alias");
                else foreach (var alias in encounter.Aliases)
                {
                    Text(alias?.Text, encounter.Id + " alias", errors, true);
                    if (alias?.Text != null && !aliases.Add(alias.Text.Trim())) errors.Add(alias.Text.Trim() + " alias is duplicated");
                    CheckLocale(alias?.Locale, encounter.Id + " alias locale", errors);
                }
                CheckParent(encounter, errors);
                CheckList(encounter.Variants, encounter.Id + " variants", errors);
                CheckList(encounter.Locations, encounter.Id + " locations", errors);
                CheckList(encounter.Abilities, encounter.Id + " abilities", errors);
                CheckList(encounter.Ailments, encounter.Id + " ailments", errors);
                CheckList(encounter.BrowseGroups, encounter.Id + " browse groups", errors);
                if (encounter.BrowseGroups?.Status == "known")
                    foreach (var group in encounter.BrowseGroups.Items ?? new List<string>())
                        if (!BrowseModes.Contains(group)) errors.Add(encounter.Id + " browse group is not supported");
                CheckCoverage(encounter, errors);
                if (encounter.Tips == null) errors.Add(encounter.Id + " tips are missing");
                else foreach (var tip in encounter.Tips)
                {
                    Text(tip?.Text, encounter.Id + " tip", errors, true);
                    if (tip != null && tip.EvidenceClass != "tactical-inference") errors.Add(encounter.Id + " tip must stay a tactical inference");
                    CheckConfidence(tip?.Confidence, encounter.Id + " tip confidence", errors);
                }
                if (encounter.Claims == null) errors.Add(encounter.Id + " claims are missing");
                else foreach (var claim in encounter.Claims)
                {
                    if (claim == null || string.IsNullOrWhiteSpace(claim.Id) || !ClaimIdPattern.IsMatch(claim.Id)) errors.Add(encounter.Id + " claim id is invalid");
                    else if (!claimIds.Add(claim.Id)) errors.Add(claim.Id + " claim is duplicated");
                    Text(claim?.Statement, encounter.Id + " claim", errors, true);
                    if (claim != null && !EvidenceClasses.Contains(claim.EvidenceClass)) errors.Add(encounter.Id + " claim evidence class is invalid");
                    CheckConfidence(claim?.Confidence, encounter.Id + " claim confidence", errors);
                    if (claim?.Source != null)
                    {
                        Text(claim.Source.Label, claim.Id + " source label", errors, true);
                        Text(claim.Source.Url, claim.Id + " source url", errors, true);
                    }
                }
            }
            var byId = catalog.Encounters.Where(e => e != null && !string.IsNullOrWhiteSpace(e.Id)).ToDictionary(e => e.Id, e => e);
            foreach (var encounter in catalog.Encounters.Where(e => e?.Parent?.Status == "known"))
            {
                if (!byId.ContainsKey(encounter.Parent.EncounterId ?? "")) errors.Add(encounter.Id + " parent was not found");
                else if (Cycle(encounter, byId)) errors.Add(encounter.Id + " parent cycle");
            }
            foreach (var encounter in catalog.Encounters.Where(e => e != null))
                CheckKnownChildren(encounter, byId, errors);
            return errors;
        }

        static void CheckKnownChildren(EncounterRecord encounter, Dictionary<string, EncounterRecord> byId, List<string> errors)
        {
            if (encounter.Variants?.Status == "known")
                foreach (var variant in encounter.Variants.Items ?? new List<VariantRecord>())
                {
                    if (variant == null || !EncounterIdPattern.IsMatch(variant.Id ?? "")) errors.Add(encounter.Id + " variant id is invalid");
                    Text(variant?.DisplayName, encounter.Id + " variant name", errors, true);
                    if (variant != null && !Relationships.Contains(variant.Relationship ?? "")) errors.Add(encounter.Id + " variant relationship is invalid");
                }
            if (encounter.Locations?.Status == "known")
                foreach (var location in encounter.Locations.Items ?? new List<LocationRecord>())
                {
                    if (location == null || (string.IsNullOrWhiteSpace(location.DisplayName) && string.IsNullOrWhiteSpace(location.SceneId)))
                        errors.Add(encounter.Id + " location needs a name or scene");
                    Text(location?.DisplayName, encounter.Id + " location", errors, false);
                    CheckLocale(location?.LocalizedName, encounter.Id + " localized zone", errors);
                }
            if (encounter.Abilities?.Status == "known")
                foreach (var ability in encounter.Abilities.Items ?? new List<AbilityRecord>())
                {
                    if (ability == null || !EncounterIdPattern.IsMatch(ability.Id ?? "")) errors.Add(encounter.Id + " ability id is invalid");
                    if (ability != null && string.IsNullOrWhiteSpace(ability.DisplayName) && string.IsNullOrWhiteSpace(ability.InternalName))
                        errors.Add(encounter.Id + " ability needs a name");
                    Text(ability?.DisplayName, encounter.Id + " ability", errors, false);
                    if (!string.IsNullOrEmpty(ability?.InheritedFromEncounterId) && !byId.ContainsKey(ability.InheritedFromEncounterId))
                        errors.Add(encounter.Id + " inherited ability source was not found");
                }
            if (encounter.Ailments?.Status == "known")
                foreach (var ailment in encounter.Ailments.Items ?? new List<AilmentRecord>())
                    Text(ailment?.Name, encounter.Id + " ailment", errors, true);
            var coverage = encounter.DamageCoverage;
            if (coverage?.PerAbility?.Status == "known")
                foreach (var component in coverage.PerAbility.Items ?? new List<DamageComponent>())
                {
                    if (component == null || !Elements.Names.Contains(component.Element)) errors.Add(encounter.Id + " damage component element is invalid");
                    if (component != null && !Deliveries.Contains(component.Delivery ?? "")) errors.Add(encounter.Id + " damage delivery is invalid");
                    if (component != null && !PositiveEvidence.Contains(component.EvidenceClass ?? "")) errors.Add(encounter.Id + " damage component needs source evidence");
                    CheckConfidence(component?.Confidence, encounter.Id + " component confidence", errors);
                    if (AbilityNameEstablishesDamage(component?.AbilityId, component?.Element)) errors.Add(encounter.Id + " ability name was treated as damage");
                }
        }

        static void CheckParent(EncounterRecord encounter, List<string> errors)
        {
            var parent = encounter.Parent;
            if (parent == null || !Statuses.Contains(parent.Status ?? "")) { errors.Add(encounter.Id + " parent status is invalid"); return; }
            bool hasId = !string.IsNullOrWhiteSpace(parent.EncounterId);
            if (parent.Status == "known" && !hasId) errors.Add(encounter.Id + " parent id is missing");
            if (parent.Status != "known" && hasId) errors.Add(encounter.Id + " parent id must be empty unless the parent is known");
            if (parent.Status == "known" && parent.EncounterId == encounter.Id) errors.Add(encounter.Id + " cannot parent itself");
        }

        static void CheckList<T>(CatalogList<T> list, string label, List<string> errors)
        {
            if (list == null || !Statuses.Contains(list.Status ?? "")) { errors.Add(label + " status is invalid"); return; }
            if (list.Status == "known" && list.Items == null) errors.Add(label + " items are missing");
            if (list.Status != "known" && list.Items != null) errors.Add(label + " items must be null until known");
        }

        static void CheckCoverage(EncounterRecord encounter, List<string> errors)
        {
            var coverage = encounter.DamageCoverage;
            if (coverage == null || !Statuses.Contains(coverage.Status ?? "")) { errors.Add(encounter.Id + " damage coverage status is invalid"); return; }
            CheckConfidence(coverage.Confidence, encounter.Id + " coverage confidence", errors);
            if (coverage.PerAbility == null) errors.Add(encounter.Id + " per-ability field is missing");
            else CheckList(coverage.PerAbility, encounter.Id + " per-ability damage", errors);
            if (coverage.Status != "known")
            {
                if (coverage.Elements != null || coverage.Scope != null) errors.Add(encounter.Id + " unknown coverage must keep elements and scope null");
                if (coverage.EvidenceClass != "unknown") errors.Add(encounter.Id + " unknown coverage cannot claim an evidence class");
                if (coverage.PerAbility?.Status == "known") errors.Add(encounter.Id + " per-ability damage cannot be known without coverage");
                return;
            }
            if (coverage.Elements == null || coverage.Elements.Count == 0 || coverage.Elements.Any(e => !Elements.Names.Contains(e)) || coverage.Elements.Distinct(StringComparer.Ordinal).Count() != coverage.Elements.Count)
                errors.Add(encounter.Id + " damage elements are invalid");
            if (coverage.Scope != "encounter" && coverage.Scope != "ability") errors.Add(encounter.Id + " damage scope is invalid");
            if (coverage.Scope == "ability" && coverage.PerAbility?.Status != "known") errors.Add(encounter.Id + " ability scope needs per-ability rows");
            if (!PositiveEvidence.Contains(coverage.EvidenceClass ?? "")) errors.Add(encounter.Id + " damage coverage needs source evidence");
        }

        static void CheckLocale(LocaleField locale, string label, List<string> errors)
        {
            if (locale == null || !Statuses.Contains(locale.Status ?? "")) { errors.Add(label + " status is invalid"); return; }
            if (locale.Status == "known" && string.IsNullOrWhiteSpace(locale.Tag)) errors.Add(label + " tag is missing");
            if (locale.Status != "known" && locale.Tag != null) errors.Add(label + " tag must be null until known");
        }

        static void CheckConfidence(ConfidenceField confidence, string label, List<string> errors)
        {
            if (confidence == null || !Statuses.Contains(confidence.Status ?? "")) { errors.Add(label + " status is invalid"); return; }
            if (confidence.Status == "known" && !Levels.Contains(confidence.Level ?? "")) errors.Add(label + " level is invalid");
            if (confidence.Status != "known" && confidence.Level != null) errors.Add(label + " level must be null until known");
        }

        static void Text(string value, string label, List<string> errors, bool required)
        {
            if (string.IsNullOrWhiteSpace(value)) { if (required) errors.Add(label + " is missing"); return; }
            if (value.Contains("--", StringComparison.Ordinal)) errors.Add(label + " contains a double dash");
            if (value.IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("TBD", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                errors.Add(label + " contains a placeholder");
        }

        static bool Cycle(EncounterRecord start, Dictionary<string, EncounterRecord> byId)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = start;
            while (current?.Parent?.Status == "known" && !string.IsNullOrEmpty(current.Parent.EncounterId))
            {
                if (!seen.Add(current.Id)) return true;
                if (!byId.TryGetValue(current.Parent.EncounterId, out current)) return false;
            }
            return false;
        }
    }

    public sealed class CatalogDocument
    {
        public string SchemaVersion { get; set; }
        public string Reviewed { get; set; }
        public List<EncounterRecord> Encounters { get; set; }
    }

    public sealed class EncounterRecord
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public List<AliasRecord> Aliases { get; set; }
        public ParentRef Parent { get; set; }
        public CatalogList<VariantRecord> Variants { get; set; }
        public CatalogList<LocationRecord> Locations { get; set; }
        public CatalogList<AbilityRecord> Abilities { get; set; }
        public DamageCoverage DamageCoverage { get; set; }
        public CatalogList<AilmentRecord> Ailments { get; set; }
        public CatalogList<string> BrowseGroups { get; set; }
        public List<TipRecord> Tips { get; set; }
        public List<ClaimRecord> Claims { get; set; }
    }

    public sealed class CatalogList<T>
    {
        public string Status { get; set; }
        public List<T> Items { get; set; }
        public static CatalogList<T> Unknown() => new() { Status = "unknown", Items = null };
        public static CatalogList<T> Absent() => new() { Status = "absent", Items = null };
        public static CatalogList<T> Known(List<T> items) => new() { Status = "known", Items = items ?? new List<T>() };
    }

    public sealed class LocaleField
    {
        public string Status { get; set; }
        public string Tag { get; set; }
        public static LocaleField Unknown() => new() { Status = "unknown", Tag = null };
    }

    public sealed class ParentRef
    {
        public string Status { get; set; }
        public string EncounterId { get; set; }
        public static ParentRef Unknown() => new() { Status = "unknown", EncounterId = null };
        public static ParentRef Absent() => new() { Status = "absent", EncounterId = null };
        public static ParentRef Known(string id) => new() { Status = "known", EncounterId = id };
    }

    public sealed class ConfidenceField
    {
        public string Status { get; set; }
        public string Level { get; set; }
        public static ConfidenceField Unknown() => new() { Status = "unknown", Level = null };
        public static ConfidenceField Known(string level) => new() { Status = "known", Level = level };
    }

    public sealed class AliasRecord
    {
        public string Text { get; set; }
        public LocaleField Locale { get; set; }
    }

    public sealed class VariantRecord
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Relationship { get; set; }
    }

    public sealed class LocationRecord
    {
        public string DisplayName { get; set; }
        public string SceneId { get; set; }
        public LocaleField LocalizedName { get; set; }
    }

    public sealed class AbilityRecord
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string InternalName { get; set; }
        public string InheritedFromEncounterId { get; set; }
    }

    public sealed class AilmentRecord
    {
        public string Name { get; set; }
        public string Effect { get; set; }
    }

    public sealed class DamageComponent
    {
        public string AbilityId { get; set; }
        public string Element { get; set; }
        public string Delivery { get; set; }
        public string EvidenceClass { get; set; }
        public ConfidenceField Confidence { get; set; }
    }

    public sealed class DamageCoverage
    {
        public string Status { get; set; }
        public string Scope { get; set; }
        public List<string> Elements { get; set; }
        public CatalogList<DamageComponent> PerAbility { get; set; }
        public string EvidenceClass { get; set; }
        public ConfidenceField Confidence { get; set; }
        public static DamageCoverage Unknown() => new()
        {
            Status = "unknown", Scope = null, Elements = null,
            PerAbility = CatalogList<DamageComponent>.Unknown(),
            EvidenceClass = "unknown", Confidence = ConfidenceField.Unknown()
        };
    }

    public sealed class TipRecord
    {
        public string Text { get; set; }
        public string EvidenceClass { get; set; }
        public ConfidenceField Confidence { get; set; }
    }

    public sealed class ClaimRecord
    {
        public string Id { get; set; }
        public string Statement { get; set; }
        public string EvidenceClass { get; set; }
        public ConfidenceField Confidence { get; set; }
        public SourceRef Source { get; set; }
    }

    public sealed class SourceRef
    {
        public string Label { get; set; }
        public string Url { get; set; }
    }

    public sealed class EncounterLink
    {
        public string Kind { get; set; }
        public string EncounterId { get; set; }
    }

    public sealed class PreparationReport
    {
        public bool FullyPrepared { get; set; }
        public List<string> OpenThreats { get; set; } = new();
    }
}
