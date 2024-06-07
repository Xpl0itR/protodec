// Copyright © 2023-2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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

    public AssemblyInspector(string assemblyPath)
    {
        bool   isFile      = File.Exists(assemblyPath);
        string assemblyDir = isFile
            ? Path.GetDirectoryName(assemblyPath)!
            : assemblyPath;

        PermissiveAssemblyResolver assemblyResolver = new(
            Directory.EnumerateFiles(assemblyDir, searchPattern: "*.dll"));

        AssemblyContext = new MetadataLoadContext(assemblyResolver);
        LoadedTypes     = isFile
            ? AssemblyContext.LoadFromAssemblyPath(assemblyPath).GetTypes()
            : assemblyResolver.AssemblyPathLookup.Values.SelectMany(path => AssemblyContext.LoadFromAssemblyPath(path).GetTypes()).ToList();
    }

    public IEnumerable<Type> GetProtobufMessageTypes()
    {
        Type? iMessage = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Google.Protobuf.IMessage", null)
                      ?? AssemblyContext.LoadFromAssemblyName("Google.Protobuf")
                                        .GetType("Google.Protobuf.IMessage");

        return LoadedTypes.Where(
            type => type is { IsNested: false, IsSealed: true }
                 && type.Namespace?.StartsWith("Google.Protobuf", StringComparison.Ordinal) != true
                 && type.IsAssignableTo(iMessage));
    }

    public IEnumerable<Type> GetProtobufServiceClientTypes()
    {
        Type? clientBase = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Grpc.Core.ClientBase", null)
                        ?? AssemblyContext.LoadFromAssemblyName("Grpc.Core.Api")
                                          .GetType("Grpc.Core.ClientBase");

        return LoadedTypes.Where(
            type => type is { IsNested: true, IsAbstract: false }
                 && type.IsAssignableTo(clientBase));
    }

    public IEnumerable<Type> GetProtobufServiceServerTypes()
    {
        Type? bindServiceMethodAttribute = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Grpc.Core.BindServiceMethodAttribute", null)
                                        ?? AssemblyContext.LoadFromAssemblyName("Grpc.Core.Api")
                                                          .GetType("Grpc.Core.BindServiceMethodAttribute");

        return LoadedTypes.Where(
            type => type is { IsNested: true, IsAbstract: true, DeclaringType: { IsNested: false, IsSealed: true, IsAbstract: true } }
                 && type.GetCustomAttributesData().Any(attribute => attribute.AttributeType == bindServiceMethodAttribute));
    }

    public void Dispose() =>
        AssemblyContext.Dispose();

    /// <summary>
    ///     An assembly resolver that uses paths to every assembly that may be loaded.
    ///     The file name is expected to be the same as the assembly's simple name (casing ignored).
    ///     PublicKeyToken, Version and CultureName are ignored.
    /// </summary>
    private sealed class PermissiveAssemblyResolver(IEnumerable<string> assemblyPaths) : MetadataAssemblyResolver
    {
        public readonly IReadOnlyDictionary<string, string> AssemblyPathLookup =
            assemblyPaths.ToDictionary(
                static path => Path.GetFileNameWithoutExtension(path),
                StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override Assembly? Resolve(MetadataLoadContext mlc, AssemblyName assemblyName) =>
            AssemblyPathLookup.TryGetValue(assemblyName.Name!, out string? assemblyPath)
                ? mlc.LoadFromAssemblyPath(assemblyPath)
                : null;
    }
}