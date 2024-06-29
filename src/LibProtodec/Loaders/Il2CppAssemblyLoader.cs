// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using AssetRipper.Primitives;
using CommunityToolkit.Diagnostics;
using LibCpp2IL;
using LibCpp2IL.Metadata;
using LibCpp2IL.Reflection;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Cil.Il2Cpp;

namespace LibProtodec.Loaders;

public sealed class Il2CppAssemblyLoader : ICilAssemblyLoader
{
    public Il2CppAssemblyLoader(string assemblyPath, string metadataPath, UnityVersion unityVersion)
    {
        if (!LibCpp2IlMain.LoadFromFile(assemblyPath, metadataPath, unityVersion))
            ThrowHelper.ThrowInvalidDataException("Failed to load il2cpp assembly!");

        LoadedTypes = LibCpp2IlMain.TheMetadata!.typeDefs.Select(Il2CppType.GetOrCreate).ToList();
    }

    public IReadOnlyList<ICilType> LoadedTypes { get; }

    public ICilType IMessage
    {
        get
        {
            Il2CppTypeDefinition? iMessage = LibCpp2IlReflection.GetTypeByFullName("Google.Protobuf.IMessage");
            Guard.IsNotNull(iMessage);

            return Il2CppType.GetOrCreate(iMessage);
        }
    }

    public ICilType ClientBase
    {
        get
        {
            Il2CppTypeDefinition? clientBase = LibCpp2IlReflection.GetTypeByFullName("Grpc.Core.ClientBase");
            Guard.IsNotNull(clientBase);

            return Il2CppType.GetOrCreate(clientBase);
        }
    }

    public ICilType BindServiceMethodAttribute
    {
        get
        {
            Il2CppTypeDefinition? attribute = LibCpp2IlReflection.GetTypeByFullName("Grpc.Core.BindServiceMethodAttribute");
            Guard.IsNotNull(attribute);

            return Il2CppType.GetOrCreate(attribute);
        }
    }

    public void Dispose() =>
        LibCpp2IlMain.Reset();
}