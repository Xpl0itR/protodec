using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace protodec;

public sealed class AssemblyInspector : IDisposable
{
    private const string DllPattern = "*.dll";

    private readonly MetadataLoadContext _assemblyContext;
    private readonly string[]            _assemblyPaths;
    private readonly Type                _googleProtobufIMessage;

    public AssemblyInspector(string assemblyPath, bool includeRuntimeAssemblies)
    {
        if (File.Exists(assemblyPath))
        {
            _assemblyPaths = new[] { assemblyPath };
        }
        else if (Directory.Exists(assemblyPath))
        {
            _assemblyPaths = Directory.EnumerateFiles(assemblyPath, DllPattern).ToArray();
        }
        else
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(assemblyPath);
        }

        PathAssemblyResolver resolver = new(includeRuntimeAssemblies ? ConcatRuntimeAssemblyPaths(_assemblyPaths) : _assemblyPaths);

        _assemblyContext        = new MetadataLoadContext(resolver);
        _googleProtobufIMessage = _assemblyContext.LoadFromAssemblyName("Google.Protobuf")
                                                  .GetType("Google.Protobuf.IMessage")!;
    }

    public IEnumerable<Type> GetProtobufMessageTypes() =>
        from assemblyPath
            in _assemblyPaths
        from type
            in _assemblyContext.LoadFromAssemblyPath(assemblyPath).GetTypes()
        where type.IsSealed
           && type.Namespace != "Google.Protobuf.Reflection"
           && type.Namespace != "Google.Protobuf.WellKnownTypes"
           && type.IsAssignableTo(_googleProtobufIMessage)
        select type;

    public void Dispose() =>
        _assemblyContext.Dispose();

    private static IEnumerable<string> ConcatRuntimeAssemblyPaths(IEnumerable<string> paths)
    {
        string path = RuntimeEnvironment.GetRuntimeDirectory();

        return Directory.EnumerateFiles(path, DllPattern)
                        .Concat(paths);
    }
}