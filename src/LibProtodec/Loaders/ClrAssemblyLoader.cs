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
using Microsoft.Extensions.Logging;

namespace LibProtodec.Loaders;

public sealed class ClrAssemblyLoader : CilAssemblyLoader
{
    public readonly MetadataLoadContext LoadContext;

    public ClrAssemblyLoader(string assemblyPath, ILogger<ClrAssemblyLoader>? logger = null)
    {
        bool isFile = File.Exists(assemblyPath);
        string assemblyDir = isFile
            ? Path.GetDirectoryName(assemblyPath)!
            : assemblyPath;

        PermissiveAssemblyResolver assemblyResolver = new(
            Directory.EnumerateFiles(assemblyDir, searchPattern: "*.dll"));
        LoadContext = new MetadataLoadContext(assemblyResolver);
        
        IEnumerable<Type> allTypes = isFile
            ? LoadContext.LoadFromAssemblyPath(assemblyPath).GetTypes()
            : assemblyResolver.AssemblyPathLookup.Values
                              .SelectMany(path => LoadContext.LoadFromAssemblyPath(path).GetTypes());

        this.LoadedTypes = allTypes.Where(static type => type.GenericTypeArguments.Length == 0)
                                   .Select(ClrType.GetOrCreate)
                                   .ToList();

        logger?.LogLoadedTypeAndAssemblyCount(this.LoadedTypes.Count, LoadContext.GetAssemblies().Count());
    }

    protected override ICilType FindType(string typeFullName, string assemblySimpleName)
    {
        ICilType? type = this.LoadedTypes.SingleOrDefault(type => type?.FullName == typeFullName, null);
        if (type is not null)
            return type;

        Type? clrType = LoadContext.LoadFromAssemblyName(assemblySimpleName).GetType(typeFullName);
        Guard.IsNotNull(clrType);

        return ClrType.GetOrCreate(clrType);
    }

    public override void Dispose() =>
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