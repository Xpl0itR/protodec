using System.Collections.Concurrent;
using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrModule : ICilModule
{
    private readonly Module _module;

    private ClrModule(Module module) =>
        _module = module;

    public string ResolveFieldName(int token) =>
        _module.ResolveField(token).Name;

    public string ResolveMethodName(int token) =>
        _module.ResolveMethod(token).Name;


    private static readonly ConcurrentDictionary<string, ClrModule> ModuleLookup = [];

    public static ICilModule GetOrCreate(Module clrModule) =>
        ModuleLookup.GetOrAdd(
            clrModule.FullyQualifiedName,
            static (_, clrModule) => new ClrModule(clrModule),
            clrModule);
}