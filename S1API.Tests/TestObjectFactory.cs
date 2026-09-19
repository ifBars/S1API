using System.Runtime.CompilerServices;

namespace S1API.Tests;

internal static class TestObjectFactory
{
    public static T CreateUninitialized<T>() where T : class =>
        (T)CreateUninitialized(typeof(T));

    public static object CreateUninitialized(Type type)
    {
        object instance = RuntimeHelpers.GetUninitializedObject(type);

        // IL2CPP wrappers finalize through GameAssembly, which is unavailable in
        // the standalone test host. These fixtures never own a native object.
        GC.SuppressFinalize(instance);
        return instance;
    }
}
