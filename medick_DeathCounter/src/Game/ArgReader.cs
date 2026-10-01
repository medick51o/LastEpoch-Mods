using System;
using System.Reflection;
using medick_DeathCounter.Core;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    // Pulls "who, with what, how much, which element, which ailment" out of a
    // damage call's arguments without knowing the exact signature. Reads only
    // what is there; anything it cannot find stays null and the analyzer
    // copes. Runs for PLAYER hits only (GameHooks filters first).
    internal static class ArgReader
    {
        // ActiveAilment (a DoT's DamageSource) carries ailment / creator / ability.
        static readonly string[] Nested = { "creator", "source", "Source", "attacker", "owner", "sourceActor", "damageSource",
                                            "ability", "Ability", "ailment", "Ailment", "damageStats", "stats" };
        static readonly string[] DamageArrays = { "damage", "Damage", "damageValues", "damages" };
        static readonly string[] CritNames    = { "isCrit", "crit", "critical", "isCritical", "wasCrit" };
        static readonly string[] NameNames    = { "displayName", "DisplayName", "baseDisplayName", "actorName", "localizedName", "abilityName", "playerAbilityName", "ailmentName", "name" };

        public static void Read(object[] args, MethodBase method, HitEvent h)
        {
            if (args == null) return;
            string mname = method?.Name ?? "";
            if (mname.Contains("DoT") || mname.Contains("Dot") || mname.Contains("OverTime") || mname.Contains("Tick"))
                h.IsDot = true;

            string hintElement = null;
            ParameterInfo[] ps = null;
            try { ps = method?.GetParameters(); } catch { }

            // An explicit Actor argument (ApplyDamage's `Actor attacker`) is the
            // killer; it beats any name dug out of the damage source below.
            var actorType = PlayerProbe.ActorType;
            if (actorType != null)
                foreach (var a in args)
                    if (a is Component ac && actorType.IsInstanceOfType(a)) { h.Source = ActorName(ac); break; }

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == null) continue;
                string pname = (ps != null && i < ps.Length ? ps[i].Name : "")?.ToLowerInvariant() ?? "";

                switch (a)
                {
                    case bool b:
                        if (pname.Contains("crit")) h.Crit = b;
                        else if (b && (pname.Contains("dot") || pname.Contains("overtime"))) h.IsDot = true;
                        break;
                    case string s:
                        h.Ailment ??= Ailments.Find(s)?.Name;
                        break;
                    case Enum e:
                        string en = e.ToString();
                        h.Ailment ??= Ailments.Find(en)?.Name;
                        if (Elements.TryParse(en, out _)) hintElement = en;   // the amount may come in a later arg
                        break;
                    default:
                        if (Refl.TryFloat(a, out float f))
                        {
                            if (h.Amount <= 0f && f > 0f && !pname.Contains("mult") && !pname.Contains("chance") && !pname.Contains("ratio") && !pname.Contains("percent"))
                                h.Amount = f;
                        }
                        else Inspect(a, h, 0);
                        break;
                }
            }

            // An element enum with a bare amount: put it all on that element.
            if (h.ByElement == null && hintElement != null && Elements.TryParse(hintElement, out var el) && h.Amount > 0f)
            {
                h.ByElement = new float[Elements.Count];
                h.ByElement[(int)el] = h.Amount;
            }
        }

        static void Inspect(object o, HitEvent h, int depth)
        {
            if (o == null || depth > 1) return;
            string tn = o.GetType().Name;

            var actorType = PlayerProbe.ActorType;
            if (o is Component comp && h.Source == null && !tn.Contains("DamageStatsHolder"))
            {
                if (actorType != null && actorType.IsInstanceOfType(o))
                    h.Source = ActorName(comp);
                else
                {
                    // Some other component on an attacker (a projectile, a minion's
                    // AI): name it after its owning actor when it has one.
                    GameObject go = null;
                    try { go = comp.gameObject; } catch { }
                    var owner = Refl.GetComponent(go, actorType);
                    h.Source = owner != null ? ActorName(owner) : Refl.UnityName(go);
                }
            }

            if (tn.Contains("Damage"))
            {
                var arr = Refl.Floats(Refl.Get(o, DamageArrays));
                if (arr != null && arr.Length >= Elements.Count && h.ByElement == null)
                    h.ByElement = ElementMap.ToOurOrder(arr);
                if (h.Crit == null && Refl.Get(o, CritNames) is bool c) h.Crit = c;
                if (Refl.Get(o, "isHit") is bool isHit && !isHit) h.IsDot = true;   // DamageStats: DoT ticks are not hits
            }
            if (tn.Contains("DamageStatsHolder"))
            {
                // An ability's damage object: ask it who made it and what it is.
                if (h.Source == null && Refl.Call(o, "getCreator") is Component creator) h.Source = ActorName(creator);
                if (h.Ability == null) h.Ability = Pretty(Refl.Call(o, "getAbilityName") as string);
            }
            if (tn.Contains("ActiveAilment")) h.IsDot = true;
            if (tn.Contains("Ability") && h.Ability == null)
                h.Ability = Pretty(Refl.GetString(o, NameNames) ?? Refl.UnityName(o));
            if (tn.Contains("Ailment") && h.Ailment == null)
                h.Ailment = Ailments.Find(Refl.GetString(o, NameNames) ?? Refl.UnityName(o) ?? tn)?.Name;

            if (depth == 0)
                foreach (var n in Nested)
                {
                    var v = Refl.Get(o, n);
                    if (v != null && !ReferenceEquals(v, o) && !(v is string) && !v.GetType().IsPrimitive) Inspect(v, h, depth + 1);
                }
        }

        public static string ActorName(Component a)
        {
            if (a == null) return null;
            try
            {
                if (PlayerProbe.IsPlayerObject(a.gameObject)) return "Yourself";
                // ActorDisplayInformation can sit on a child (the visuals object).
                var infoType = Refl.FindType("Il2Cpp.ActorDisplayInformation");
                var info = Refl.GetComponent(a.gameObject, infoType) ?? Refl.GetComponentInChildren(a.gameObject, infoType);
                var n = Refl.GetString(info, "displayName", "baseDisplayName", "DisplayName");
                if (n != null) return Pretty(n);
                return Refl.UnityName(a.gameObject);
            }
            catch { return null; }
        }

        // For the ailment tap: the first argument that names a known ailment.
        public static string AilmentName(object[] args)
        {
            if (args == null) return null;
            foreach (var a in args)
            {
                if (a == null) continue;
                // Only arguments that ARE ailments: an Actor named "Shock Wraith"
                // must not read as Shock.
                string text = a switch
                {
                    string s => s,
                    Enum e   => e.ToString(),
                    _ when a.GetType().Name.Contains("Ailment") =>
                        Refl.GetString(a, "displayName", "instanceName", "name") ?? Refl.UnityName(a)
                        ?? Refl.GetString(Refl.Get(a, "ailment"), "displayName", "instanceName", "name"),
                    _ => null,
                };
                var hit = Ailments.Find(text);
                if (hit != null) return hit.Name;
            }
            return null;
        }

        // Localization keys and internal names → something readable.
        static string Pretty(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = Refl.CleanName(s) ?? s;
            return s.Length > 48 ? s.Substring(0, 48) : s;
        }
    }

    // The game's DamageType enum order → our Element order, by name, once.
    internal static class ElementMap
    {
        static int[] _map;   // _map[gameIndex] = (int)Element, or -1

        public static float[] ToOurOrder(float[] game)
        {
            _map ??= Build();
            var r = new float[Elements.Count];
            for (int i = 0; i < game.Length && i < _map.Length; i++)
                if (_map[i] >= 0) r[_map[i]] += game[i];
            return r;
        }

        static int[] Build()
        {
            var t = Refl.FindType("Il2Cpp.DamageType");
            if (t == null || !t.IsEnum) return new[] { 0, 1, 2, 3, 4, 5, 6 };
            var names = Enum.GetNames(t);
            var vals  = Enum.GetValues(t);
            int max = 0;
            foreach (var v in vals) max = Math.Max(max, Convert.ToInt32(v));
            var map = new int[max + 1];
            for (int i = 0; i < map.Length; i++) map[i] = -1;
            for (int i = 0; i < names.Length; i++)
            {
                int idx = Convert.ToInt32(vals.GetValue(i));
                if (idx >= 0 && Elements.TryParse(names[i], out var el)) map[idx] = (int)el;
            }
            Dbg.Log("DamageType map: " + string.Join(",", map));
            return map;
        }
    }
}
