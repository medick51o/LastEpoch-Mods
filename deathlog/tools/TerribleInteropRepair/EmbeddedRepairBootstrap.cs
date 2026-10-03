using System;
using System.IO;
using System.Runtime.CompilerServices;
using MelonLoader;
using MelonLoader.Utils;

namespace MedicK.TerribleInteropRepair;

internal static class EmbeddedRepairBootstrap
{
    // Run when MelonLoader first executes this assembly (during mod discovery),
    // before its dependency graph and Unity support module initialization.
    // No Unity or game types may be referenced by this bootstrap.
    #pragma warning disable CA2255 // Intentional early initialization in a MelonLoader mod assembly.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        try
        {
            string path = Path.Combine(MelonEnvironment.Il2CppAssembliesDirectory, "UnityEngine.CoreModule.dll");
            MelonLogger.Msg("[Terrible embedded repair] " + CoreModuleRepair.Repair(path));
        }
        catch (Exception ex)
        {
            MelonLogger.Error("[Terrible embedded repair] CoreModule normalization failed: " + ex);
        }
    }
}

