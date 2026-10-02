using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;
using System.Security.Cryptography;

[assembly: MelonInfo(typeof(MedicK.TerribleInteropRepair.RepairPlugin), "Terrible Interop Repair", "1.0.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace MedicK.TerribleInteropRepair;

public sealed class RepairPlugin : MelonPlugin
{
    // This runs AFTER assembly generation, before mod dependencies are loaded.
    // OnPreInitialization would repair yesterday's file, then generation could
    // overwrite it in the same launch.
    public override void OnPreModsLoaded()
    {
        try
        {
            string path = Path.Combine(MelonEnvironment.Il2CppAssembliesDirectory, "UnityEngine.CoreModule.dll");
            string result = CoreModuleRepair.Repair(path);
            MelonLogger.Msg("[Terrible Interop Repair] " + result);
        }
        catch (Exception ex)
        {
            MelonLogger.Error("[Terrible Interop Repair] CoreModule repair failed; original retained when replacement did not complete. " + ex);
        }
    }
}

public static class CoreModuleRepair
{
    public static string Repair(string path)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new FileNotFoundException("Generated CoreModule not found.", path);
        string receipt = path + ".terrible-repair.sha256";
        byte[] original = File.ReadAllBytes(path);
        string originalHash = Convert.ToHexString(SHA256.HashData(original));
        if (File.Exists(receipt) && File.ReadAllText(receipt).Trim() == originalHash)
            return "CoreModule already normalized; no changes.";

        byte[] rewritten;
        string[] before;
        using (var input = new MemoryStream(original))
        using (var assembly = AssemblyDefinition.ReadAssembly(input))
        using (var output = new MemoryStream())
        {
            before = Signatures(assembly);
            // Re-emit the metadata tables. Do not delete, rename, or merge types.
            assembly.Write(output);
            rewritten = output.ToArray();
        }
        using (var input = new MemoryStream(rewritten))
        using (var verification = AssemblyDefinition.ReadAssembly(input))
            if (!before.SequenceEqual(Signatures(verification)))
                throw new InvalidDataException("Rewrite changed type/member signatures; original retained.");

        string repairedHash = Convert.ToHexString(SHA256.HashData(rewritten));
        string backup = path + ".terrible-original-" + originalHash.Substring(0, 16) + ".bak";
        if (!File.Exists(backup)) File.WriteAllBytes(backup, original);
        else if (!SHA256.HashData(File.ReadAllBytes(backup)).SequenceEqual(SHA256.HashData(original)))
            throw new InvalidDataException("Existing backup differs from original; refusing replacement.");

        string temporary = path + ".terrible-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, rewritten);
            // Same-directory atomic replacement; backup was created above.
            File.Replace(temporary, path, null);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        File.WriteAllText(receipt, repairedHash);
        return "CoreModule metadata normalized without removing types. Backup: " + Path.GetFileName(backup);
    }

    private static string[] Signatures(AssemblyDefinition assembly)
    {
        var signatures = new List<string>();
        foreach (var module in assembly.Modules)
        {
            signatures.Add("A:" + assembly.Name.FullName + ":" + module.Name);
            AddTypes(module.Types, signatures);
        }
        return signatures.OrderBy(s => s, StringComparer.Ordinal).ToArray();
    }

    private static void AddTypes(IEnumerable<TypeDefinition> types, List<string> signatures)
    {
        foreach (var type in types)
        {
            signatures.Add("T:" + type.FullName + ":" + type.Attributes + ":" + type.BaseType?.FullName);
            foreach (var field in type.Fields) signatures.Add("F:" + field.FullName + ":" + field.Attributes);
            foreach (var method in type.Methods) signatures.Add("M:" + method.FullName + ":" + method.Attributes);
            foreach (var property in type.Properties) signatures.Add("P:" + property.FullName + ":" + property.Attributes);
            foreach (var evt in type.Events) signatures.Add("E:" + evt.FullName + ":" + evt.Attributes);
            AddTypes(type.NestedTypes, signatures);
        }
    }
}
