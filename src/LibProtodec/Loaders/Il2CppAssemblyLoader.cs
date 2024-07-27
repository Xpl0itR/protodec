// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using AssetRipper.Primitives;
using CommunityToolkit.Diagnostics;
using LibCpp2IL;
using LibCpp2IL.Logging;
using LibCpp2IL.Metadata;
using LibCpp2IL.Reflection;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Cil.Il2Cpp;
using Microsoft.Extensions.Logging;

namespace LibProtodec.Loaders;

public sealed class Il2CppAssemblyLoader : CilAssemblyLoader
{
    public Il2CppAssemblyLoader(string assemblyPath, string metadataPath, UnityVersion unityVersion, ILoggerFactory? loggerFactory = null)
    {
        if (loggerFactory is not null)
        {
            LibLogger.Writer = new LibCpp2IlLogger(
                loggerFactory.CreateLogger(nameof(LibCpp2IL)));
        }

        if (!LibCpp2IlMain.LoadFromFile(assemblyPath, metadataPath, unityVersion))
            ThrowHelper.ThrowInvalidDataException("Failed to load IL2Cpp assembly!");

        this.LoadedTypes = LibCpp2IlMain.TheMetadata!.typeDefs.Select(Il2CppType.GetOrCreate).ToList();

        loggerFactory?.CreateLogger<Il2CppAssemblyLoader>()
                      .LogLoadedTypeAndAssemblyCount(this.LoadedTypes.Count, LibCpp2IlMain.TheMetadata.imageDefinitions.Length);
    }

    protected override ICilType FindType(string typeFullName, string assemblySimpleName)
    {
        Il2CppTypeDefinition? type = LibCpp2IlReflection.GetTypeByFullName(typeFullName);
        Guard.IsNotNull(type);

        return Il2CppType.GetOrCreate(type);
    }

    private sealed class LibCpp2IlLogger(ILogger logger) : LogWriter
    {
        private static readonly Func<string, Exception?, string> MessageFormatter = (message, _) => message.Trim();

        public override void Info(string message) =>
            logger.Log(LogLevel.Information, default(EventId), message, null, MessageFormatter);

        public override void Warn(string message) =>
            logger.Log(LogLevel.Warning, default(EventId), message, null, MessageFormatter);

        public override void Error(string message) =>
            logger.Log(LogLevel.Error, default(EventId), message, null, MessageFormatter);

        public override void Verbose(string message) =>
            logger.Log(LogLevel.Debug, default(EventId), message, null, MessageFormatter);
    }
}