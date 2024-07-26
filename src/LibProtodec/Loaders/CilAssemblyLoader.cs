// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using LibProtodec.Models.Cil;

namespace LibProtodec.Loaders;

public abstract class CilAssemblyLoader : IDisposable
{
    public IReadOnlyList<ICilType> LoadedTypes { get; protected init; }

    public IEnumerable<ICilType> GetProtobufMessageTypes()
    {
        ICilType iMessage = FindType("Google.Protobuf.IMessage", "Google.Protobuf");

        return LoadedTypes.Where(
            type => type is { IsNested: false, IsSealed: true }
                 && type.Namespace?.StartsWith("Google.Protobuf", StringComparison.Ordinal) != true
                 && type.IsAssignableTo(iMessage));
    }

    public IEnumerable<ICilType> GetProtobufServiceClientTypes()
    {
        ICilType clientBase = FindType("Grpc.Core.ClientBase", "Grpc.Core.Api");

        return LoadedTypes.Where(
            type => type is { IsNested: true, IsAbstract: false }
                 && type.IsAssignableTo(clientBase));
    }

    public IEnumerable<ICilType> GetProtobufServiceServerTypes()
    {
        ICilType bindServiceMethodAttribute = FindType("Grpc.Core.BindServiceMethodAttribute", "Grpc.Core.Api");

        return LoadedTypes.Where(
            type => type is { IsNested: true, IsAbstract: true, DeclaringType: { IsNested: false, IsSealed: true, IsAbstract: true } }
                 && type.CustomAttributes.Any(attribute => attribute.Type == bindServiceMethodAttribute));
    }

    public virtual void Dispose() { }

    protected abstract ICilType FindType(string typeFullName, string assemblySimpleName);
}