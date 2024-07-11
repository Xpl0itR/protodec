// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using LibProtodec.Models.Cil;

namespace LibProtodec.Loaders;

public abstract class CilAssemblyLoader : IDisposable
{
    private ICilType? _iMessage;
    private ICilType? _clientBase;
    private ICilType? _bindServiceMethodAttribute;

    // ReSharper disable once InconsistentNaming
    public ICilType IMessage =>
        _iMessage ??= FindType("Google.Protobuf.IMessage", "Google.Protobuf");

    public ICilType ClientBase =>
        _clientBase ??= FindType("Grpc.Core.ClientBase", "Grpc.Core.Api");

    public ICilType BindServiceMethodAttribute =>
        _bindServiceMethodAttribute ??= FindType("Grpc.Core.BindServiceMethodAttribute", "Grpc.Core.Api");

    public IReadOnlyList<ICilType> LoadedTypes { get; protected init; }

    public virtual void Dispose() { }

    protected abstract ICilType FindType(string typeFullName, string assemblySimpleName);
}