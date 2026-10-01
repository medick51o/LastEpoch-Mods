using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    // Reflection over the game's interop types. This mod only hard-references
    // game types earlier Terrible mods have proven (PlayerFinder, Actor,
    // EpochInputManager); everything about health, damage and ailments is
    // looked up by NAME at runtime. A renamed member then costs one feature,
    // not the build, and the names can be fixed from the cfg (HookOverrides)
    // without a recompile.
    internal static class Refl
    {
        const BindingFlags Inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly Dictionary<(Type, string), MemberInfo> _members = new();
        static readonly Dictionary<string, Type> _types = new();

        // "Il2Cpp.BaseHealth" → the interop Type, or null. Cached, including misses.
        public static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            if (_types.TryGetValue(fullName, out var t)) return t;
            t = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(fullName, false); } catch { }
                if (t != null) break;
            }
            _types[fullName] = t;
            return t;
        }

        static MemberInfo Member(Type t, string name)
        {
            var key = (t, name);
            if (_members.TryGetValue(key, out var m)) return m;
            m = null;
            for (var cur = t; cur != null && m == null; cur = cur.BaseType)
            {
                // Il2CppInterop exposes game fields as properties; plain fields
                // are checked too for managed types.
                m = (MemberInfo)cur.GetProperty(name, Inst | BindingFlags.DeclaredOnly)
                    ?? cur.GetField(name, Inst | BindingFlags.DeclaredOnly);
                if (m is PropertyInfo p && p.GetIndexParameters().Length > 0) m = null;
            }
            _members[key] = m;
            return m;
        }

        // First readable member among names; null when none exists or all throw.
        public static object Get(object o, params string[] names)
        {
            if (o == null) return null;
            var t = o.GetType();
            foreach (var n in names)
            {
                var m = Member(t, n);
                if (m == null) continue;
                try
                {
                    var v = m is PropertyInfo p ? p.GetValue(o) : ((FieldInfo)m).GetValue(o);
                    if (v != null) return v;
                }
                catch { }
            }
            return null;
        }

        public static bool TryFloat(object v, out float f)
        {
            switch (v)
            {
                case float x:  f = x; return float.IsFinite(x);
                case double x: f = (float)x; return double.IsFinite(x);
                case int x:    f = x; return true;
                case long x:   f = x; return true;
                default:       f = 0f; return false;
            }
        }

        public static float GetFloat(object o, float fallback, params string[] names) =>
            TryFloat(Get(o, names), out var f) ? f : fallback;

        public static string GetString(object o, params string[] names)
        {
            var v = Get(o, names);
            return v is string s && !string.IsNullOrWhiteSpace(s) ? s : null;
        }

        public static IntPtr Ptr(object o) => o is Il2CppObjectBase b ? b.Pointer : IntPtr.Zero;

        // Unity object name without "(Clone)", underscores or trailing ids.
        public static string CleanName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string s = raw.Replace("(Clone)", "").Replace('_', ' ').Trim();
            while (s.Length > 0 && (char.IsDigit(s[s.Length - 1]) || s[s.Length - 1] == ' ')) s = s.Substring(0, s.Length - 1);
            return s.Length > 0 ? s : null;
        }

        public static string UnityName(object o)
        {
            try { return o is UnityEngine.Object u ? CleanName(u.name) : null; }
            catch { return null; }
        }

        // float[] / Il2CppStructArray<float> → managed float[]
        public static float[] Floats(object v)
        {
            try
            {
                if (v is float[] f) return f;
                if (v is Il2CppStructArray<float> a)
                {
                    var r = new float[a.Length];
                    for (int i = 0; i < r.Length; i++) r[i] = a[i];
                    return r;
                }
            }
            catch { }
            return null;
        }

        // gameObject.GetComponent<T>() for a T we only know by name.
        public static Component GetComponent(GameObject go, Type t)
        {
            if (go == null || t == null) return null;
            try
            {
                var gm = _getComponentGeneric ??= typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .First(m => m.Name == "GetComponent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                return gm.MakeGenericMethod(t).Invoke(go, null) as Component;
            }
            catch { return null; }
        }
        static MethodInfo _getComponentGeneric;

        // gameObject.GetComponentInChildren<T>() for a T we only know by name.
        public static Component GetComponentInChildren(GameObject go, Type t)
        {
            if (go == null || t == null) return null;
            try
            {
                var gm = _getInChildrenGeneric ??= typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .First(m => m.Name == "GetComponentInChildren" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                return gm.MakeGenericMethod(t).Invoke(go, null) as Component;
            }
            catch { return null; }
        }
        static MethodInfo _getInChildrenGeneric;

        // Il2CppInterop hands Harmony every argument wrapped as its DECLARED
        // type: ApplyDamage's DamageSource arrives as a DamageSource wrapper
        // even when the object is a DamageStatsHolder or an ActiveAilment, so
        // its real members are invisible (review 2026-10-01 #1). Re-wrap to
        // the object's runtime class. The IL2CPP natives are called by
        // reflection: nothing new for InteropGuard to prove, and a missing
        // export just leaves the declared wrapper.
        static MethodInfo _objGetClass, _classGetName, _classGetNs, _classGetDecl;
        static bool _rewrapResolved;
        static readonly Dictionary<IntPtr, Type> _classTypes = new();

        public static object Rewrap(object o)
        {
            if (o is not Il2CppObjectBase b) return o;
            try
            {
                IntPtr ptr = b.Pointer;
                if (ptr == IntPtr.Zero) return o;
                if (!_rewrapResolved)
                {
                    _rewrapResolved = true;
                    var il2cpp = typeof(Il2CppObjectBase).Assembly.GetType("Il2CppInterop.Runtime.IL2CPP");
                    _objGetClass  = il2cpp?.GetMethod("il2cpp_object_get_class", new[] { typeof(IntPtr) });
                    _classGetName = il2cpp?.GetMethod("il2cpp_class_get_name", new[] { typeof(IntPtr) });
                    _classGetNs   = il2cpp?.GetMethod("il2cpp_class_get_namespace", new[] { typeof(IntPtr) });
                    _classGetDecl = il2cpp?.GetMethod("il2cpp_class_get_declaring_type", new[] { typeof(IntPtr) });
                }
                if (_objGetClass == null || _classGetName == null) return o;

                var klass = (IntPtr)_objGetClass.Invoke(null, new object[] { ptr });
                if (!_classTypes.TryGetValue(klass, out var t))
                {
                    t = ManagedTypeFor(klass);
                    // Open generics cannot be constructed (re-review #3), and a
                    // same-named class from another assembly would read the wrong
                    // field offsets (re-review #2): accept only the wrapper whose
                    // native class IS this object's class.
                    if (t != null && (t.ContainsGenericParameters || !SameNativeClass(t, klass))) t = null;
                    _classTypes[klass] = t;
                }
                if (t == null || t == o.GetType() || !typeof(Il2CppObjectBase).IsAssignableFrom(t)) return o;
                return Activator.CreateInstance(t, ptr);
            }
            catch { return o; }
        }

        static MethodInfo _nativeClassPtr;
        static bool _nativeClassResolved;

        static bool SameNativeClass(Type t, IntPtr klass)
        {
            if (!_nativeClassResolved)
            {
                _nativeClassResolved = true;
                _nativeClassPtr = typeof(Il2CppObjectBase).Assembly.GetType("Il2CppInterop.Runtime.Il2CppClassPointerStore")
                    ?.GetMethod("GetNativeClassPointer", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
            }
            if (_nativeClassPtr == null) return false;   // cannot prove it: keep the declared wrapper
            try { return (IntPtr)_nativeClassPtr.Invoke(null, new object[] { t }) == klass; }
            catch { return false; }
        }

        // Il2CppInterop naming: global types → "Il2Cpp.Name", namespaced →
        // "Il2Cpp<Ns>.Name", nested → "Outer+Inner".
        static Type ManagedTypeFor(IntPtr klass)
        {
            string name = ClassName(klass);
            if (name == null) return null;
            IntPtr decl = _classGetDecl != null ? (IntPtr)_classGetDecl.Invoke(null, new object[] { klass }) : IntPtr.Zero;
            if (decl != IntPtr.Zero)
            {
                var outer = ManagedTypeFor(decl);
                return outer?.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
            }
            string ns = _classGetNs != null ? Marshal.PtrToStringAnsi((IntPtr)_classGetNs.Invoke(null, new object[] { klass })) : null;
            return string.IsNullOrEmpty(ns)
                ? FindType("Il2Cpp." + name)
                : FindType("Il2Cpp" + ns + "." + name) ?? FindType(ns + "." + name);
        }

        static string ClassName(IntPtr klass) =>
            klass == IntPtr.Zero ? null : Marshal.PtrToStringAnsi((IntPtr)_classGetName.Invoke(null, new object[] { klass }));

        // o.name() for a zero-argument instance method; null if missing or throwing.
        public static object Call(object o, string method)
        {
            if (o == null) return null;
            try { return o.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(o, null); }
            catch { return null; }
        }

        // Static zero-argument method or property on a type known by name.
        public static object Static(string typeName, string member)
        {
            var key = (typeName, member);
            if (!_statics.TryGetValue(key, out var mi))
            {
                var t = FindType(typeName);
                mi = (MemberInfo)t?.GetMethod(member, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                     ?? t?.GetProperty(member, BindingFlags.Public | BindingFlags.Static);
                _statics[key] = mi;
            }
            try
            {
                return mi switch
                {
                    MethodInfo m   => m.Invoke(null, null),
                    PropertyInfo p => p.GetValue(null),
                    _              => null,
                };
            }
            catch { return null; }
        }
        static readonly Dictionary<(string, string), MemberInfo> _statics = new();

        // list[i] for an interop array or List (anything with an int indexer).
        public static object Index(object list, int i)
        {
            if (list == null || i < 0) return null;
            try { return list.GetType().GetMethod("get_Item", new[] { typeof(int) })?.Invoke(list, new object[] { i }); }
            catch { return null; }
        }

        // For the ProbeApi dump: declared methods and data members of a type.
        public static IEnumerable<string> Describe(Type t)
        {
            const BindingFlags all = Inst | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var m in t.GetMethods(all).Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
                yield return $"  method {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})";
            foreach (var p in t.GetProperties(all).OrderBy(p => p.Name))
                yield return $"  member {p.PropertyType.Name} {p.Name}";
        }
    }
}
