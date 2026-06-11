using System;
using System.Linq;
using System.Reflection;

// Metadata probe: enumerate the interop members the legendary-grading fix
// needs. No game required — member enumeration never runs type initializers.
class Program
{
    static void Dump(Type t, string filter)
    {
        Console.WriteLine($"\n===== {t.FullName} (filter: {filter}) =====");
        var rx = new System.Text.RegularExpressions.Regex(filter,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                           .Where(m => rx.IsMatch(m.Name))
                           .OrderBy(m => m.Name))
        {
            try { Console.WriteLine($"  {m.MemberType,-8} {m}"); } catch { }
        }
    }

    static void Main()
    {
        var asm = typeof(Il2Cpp.ItemDataUnpacked).Assembly;
        Console.WriteLine("Il2CppLE loaded: " + asm.GetName().Version);

        Dump(typeof(Il2Cpp.ItemDataUnpacked), "roll|unique|legend|implicit|seal|weaver|mod");
        Dump(typeof(Il2Cpp.UniqueItemMod), ".");
        Dump(typeof(Il2Cpp.ItemAffix), "roll|tier|value");
        Dump(typeof(Il2Cpp.TooltipItemManager), "Formatter|Unique|Legend|Seal");

        var ul = asm.GetType("Il2Cpp.UniqueList");
        if (ul != null) Dump(ul, "Legend|unique");
    }
}
