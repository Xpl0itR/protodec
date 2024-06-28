// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Cil.Clr;

namespace LibProtodec.Loaders;

public sealed class ClrAssemblyLoader : ICilAssemblyLoader
{
    public readonly MetadataLoadContext LoadContext;

    public ClrAssemblyLoader(string assemblyPath)
    {
        bool isFile = File.Exists(assemblyPath);
        string assemblyDir = isFile
            ? Path.GetDirectoryName(assemblyPath)!
            : assemblyPath;

        PermissiveAssemblyResolver assemblyResolver = new(
            Directory.EnumerateFiles(assemblyDir, searchPattern: "*.dll"));

        LoadContext = new MetadataLoadContext(assemblyResolver);
        LoadedTypes = isFile
            ? LoadContext.LoadFromAssemblyPath(assemblyPath)
                         .GetTypes()
                         .Select(ClrType.GetOrCreate)
                         .ToList()
            : assemblyResolver.AssemblyPathLookup.Values
                              .SelectMany(path => LoadContext.LoadFromAssemblyPath(path).GetTypes())
                              .Select(ClrType.GetOrCreate)
                              .ToList();
    }

    public IReadOnlyList<ICilType> LoadedTypes { get; }

    public ICilType IMessage
    {
        get
        {
            ICilType? iMessage = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Google.Protobuf.IMessage", null);
            if (iMessage is not null)
                return iMessage;

            Type? iMessageType = LoadContext.LoadFromAssemblyName("Google.Protobuf").GetType("Google.Protobuf.IMessage");
            Guard.IsNotNull(iMessageType);

            return ClrType.GetOrCreate(iMessageType);
        }
    }

    public ICilType ClientBase
    {
        get
        {
            ICilType? clientBase = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Grpc.Core.ClientBase", null);
            if (clientBase is not null)
                return clientBase;

            Type? clientBaseType = LoadContext.LoadFromAssemblyName("Grpc.Core.Api").GetType("Grpc.Core.ClientBase");
            Guard.IsNotNull(clientBaseType);

            return ClrType.GetOrCreate(clientBaseType);
        }
    }

    public ICilType BindServiceMethodAttribute
    {
        get
        {
            ICilType? attribute = LoadedTypes.SingleOrDefault(static type => type?.FullName == "Grpc.Core.BindServiceMethodAttribute", null);
            if (attribute is not null)
                return attribute;

            Type? attributeType = LoadContext.LoadFromAssemblyName("Grpc.Core.Api").GetType("Grpc.Core.BindServiceMethodAttribute");
            Guard.IsNotNull(attributeType);

            return ClrType.GetOrCreate(attributeType);
        }
    }

    public void Dispose() =>
        LoadContext.Dispose();

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