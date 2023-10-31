using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LibProtodec;

public sealed class AssemblyInspector : IDisposable
{
    public readonly MetadataLoadContext AssemblyContext;
    public readonly IReadOnlyList<Type> LoadedTypes;

    public AssemblyInspector(string assemblyDir, string? assemblyName = null)
    {
        string[] assemblyPaths = Directory.EnumerateFiles(assemblyDir, searchPattern: "*.dll")
                                          .ToArray();

        AssemblyContext = new MetadataLoadContext(
            new PathAssemblyResolver(assemblyPaths));

        LoadedTypes = assemblyName is null
            ? assemblyPaths.SelectMany(path => AssemblyContext.LoadFromAssemblyPath(path).GetTypes()).ToList()
            : AssemblyContext.LoadFromAssemblyName(assemblyName).GetTypes();
    }

    public IEnumerable<Type> GetProtobufMessageTypes()
    {
        Type googleProtobufIMessage = AssemblyContext.LoadFromAssemblyName("Google.Protobuf")
                                                     .GetType("Google.Protobuf.IMessage")!;
        return from type
                   in LoadedTypes
               where !type.IsNested
                  && type.IsSealed
                  && type.Namespace != "Google.Protobuf.Reflection"
                  && type.Namespace != "Google.Protobuf.WellKnownTypes"
                  && type.IsAssignableTo(googleProtobufIMessage)
               select type;
    }

    public void Dispose() =>
        AssemblyContext.Dispose();
}