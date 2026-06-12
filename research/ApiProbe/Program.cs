using System;
using System.Linq;
using System.Reflection;
class Program
{
    static void Main()
    {
        var asm = typeof(Il2Cpp.ItemDataUnpacked).Assembly;
        var t = asm.GetType("Il2Cpp.TooltipItemManager");
        Console.WriteLine("=== TooltipItemManager: singleton + range switch details ===");
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (m.Name.Contains("nstance") || m.Name.Contains("showRanges") || m.Name == "SetShowRangesInsteadOfDescriptionSetting" || m.Name.Contains("ShowModRanges") || m.Name.Contains("ShowDefaultInfo") || m.Name.Contains("ShowExplanation"))
                Console.WriteLine($"  {(m.IsStatic ? "STATIC" : "inst  ")} {m}");
        }
        foreach (var pr in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (pr.Name.Contains("nstance") || pr.Name.Contains("showRanges"))
                Console.WriteLine($"  PROP {(pr.GetMethod != null && pr.GetMethod.IsStatic ? "STATIC" : "inst")} {pr.PropertyType.Name} {pr.Name}");
        }
        var baseT = t.BaseType;
        Console.WriteLine("  base: " + baseT?.FullName);
        var tm = asm.GetType("Il2Cpp.TooltipMode");
        if (tm != null && tm.IsEnum)
        {
            Console.WriteLine("=== TooltipMode values ===");
            foreach (var n in Enum.GetNames(tm)) Console.WriteLine("  " + n);
        }
    }
}
