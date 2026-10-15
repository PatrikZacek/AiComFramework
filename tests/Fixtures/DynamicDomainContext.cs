using System.Reflection;
using System.Reflection.Emit;
using AIComFramework.Core.Contexts;

namespace AIComFramework.Tests.Fixtures;

internal static class DynamicDomainContext
{
    internal static AiContext Create(string assemblyName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Domains");
        var type = module.DefineType("Example.SupportContext", TypeAttributes.Public, typeof(AiContext));
        type.DefineDefaultConstructor(MethodAttributes.Public);
        var configure = type.DefineMethod("OnConfiguring", MethodAttributes.Family | MethodAttributes.Virtual,
            typeof(void), [typeof(AiContextBuilder)]);
        configure.GetILGenerator().Emit(OpCodes.Ret);
        type.DefineMethodOverride(configure, typeof(AiContext).GetMethod("OnConfiguring", BindingFlags.Instance | BindingFlags.NonPublic)!);
        return (AiContext)Activator.CreateInstance(type.CreateType()!)!;
    }
}
