using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// InteropGuard: is every Unity / Il2CppInterop member this DLL calls one that
// a shipped Terrible mod (built against the game's real interop assemblies)
// also calls?
//
// Why: a NoGame build compiles against plain Unity from NuGet. In game the
// same names resolve against MelonLoader's generated interop, where IL2CPP
// has stripped members the game never used and every field became a
// property (GUIContent.none is a field in Unity, get_none() in game). Either
// difference is a MissingMemberException at runtime. Members proven by a DLL
// that already ran in game are safe; anything else is reported.
//
// usage: InteropGuard <candidate.dll> <repo root> [allowed member ...]
//   Proven set = every *.dll and every *.dll inside *.zip under the repo
//   root, except bin/ obj/ and the candidate's own name.
//   Exit 0 = all proven (or allowed), 1 = unproven calls found.
class Program
{
    static readonly string[] Scopes = { "UnityEngine", "Il2CppInterop" };

    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine("usage: InteropGuard <candidate.dll> <repo root> [allowed ...]"); return 2; }
        string candidate = Path.GetFullPath(args[0]);
        string self = Path.GetFileName(candidate);
        var allowed = new HashSet<string>(args.Skip(2));

        var proven = new HashSet<string>();
        int sources = 0;
        foreach (var f in Directory.EnumerateFiles(args[1], "*", SearchOption.AllDirectories))
        {
            var rel = f.Replace('\\', '/');
            if (rel.Contains("/bin/") || rel.Contains("/obj/") || rel.Contains("/.git/")) continue;
            try
            {
                if (f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f) != self)
                { proven.UnionWith(Members(File.ReadAllBytes(f))); sources++; }
                else if (f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var zip = ZipFile.OpenRead(f);
                    foreach (var e in zip.Entries.Where(e => e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && e.Name != self))
                    {
                        using var s = e.Open(); using var ms = new MemoryStream(); s.CopyTo(ms);
                        proven.UnionWith(Members(ms.ToArray())); sources++;
                    }
                }
            }
            catch { /* not a .NET assembly */ }
        }

        var ours = Members(File.ReadAllBytes(candidate));
        var unproven = ours.Where(m => !proven.Contains(m) && !allowed.Contains(m)).OrderBy(m => m).ToList();
        Console.WriteLine($"InteropGuard: {self} calls {ours.Count} interop members; proven set from {sources} shipped DLLs has {proven.Count}.");
        foreach (var m in ours.Where(allowed.Contains)) Console.WriteLine($"  allowed   {m}");
        foreach (var m in unproven) Console.WriteLine($"  UNPROVEN  {m}");
        Console.WriteLine(unproven.Count == 0 ? "OK: every interop call is proven in game." : $"FAIL: {unproven.Count} unproven call(s).");
        return unproven.Count == 0 ? 0 : 1;
    }

    // "Assembly: Namespace.Type::member" for each member reference into a
    // Unity or Il2CppInterop assembly. Field vs method matters (the kind is
    // part of the key), so a Unity field that is an interop property fails.
    static HashSet<string> Members(byte[] image)
    {
        var set = new HashSet<string>();
        using var pe = new PEReader(new MemoryStream(image));
        if (!pe.HasMetadata) return set;
        var md = pe.GetMetadataReader();
        foreach (var h in md.MemberReferences)
        {
            var m = md.GetMemberReference(h);
            if (m.Parent.Kind != HandleKind.TypeReference) continue;
            var tr = md.GetTypeReference((TypeReferenceHandle)m.Parent);
            string asm = Scope(md, tr.ResolutionScope);
            if (!Scopes.Any(asm.StartsWith)) continue;
            set.Add($"{asm}: {md.GetString(tr.Namespace)}.{md.GetString(tr.Name)}::{md.GetString(m.Name)} ({m.GetKind()})");
        }
        return set;
    }

    static string Scope(MetadataReader md, EntityHandle s) => s.Kind switch
    {
        HandleKind.AssemblyReference => md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)s).Name),
        HandleKind.TypeReference     => Scope(md, md.GetTypeReference((TypeReferenceHandle)s).ResolutionScope),
        _                            => "?",
    };
}
