using System;
using System.Reflection;

namespace medick_DeathCounter.Core
{
    public static class InteropSignaturePolicy
    {
        // Nullable boxing can fail before a Harmony callback runs. Catching
        // inside the patch cannot contain it; avoid the detour altogether.
        public static bool CanPatch(MethodInfo method) => method != null
            && !Unsupported(method.ReturnType)
            && Array.TrueForAll(method.GetParameters(), p => !Unsupported(p.ParameterType));

        static bool Unsupported(Type type)
        {
            if (type.HasElementType) return Unsupported(type.GetElementType());
            if (!type.IsGenericType) return false;
            if (type.GetGenericTypeDefinition().FullName == "Il2CppSystem.Nullable`1") return true;
            return Array.Exists(type.GetGenericArguments(), Unsupported);
        }
    }
}
