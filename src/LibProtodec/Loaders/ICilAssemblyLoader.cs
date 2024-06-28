// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using LibProtodec.Models.Cil;

namespace LibProtodec.Loaders;

public interface ICilAssemblyLoader : IDisposable
{
    IReadOnlyList<ICilType> LoadedTypes { get; }

    // ReSharper disable once InconsistentNaming
    ICilType IMessage { get; }

    ICilType ClientBase { get; }

    ICilType BindServiceMethodAttribute { get; }
}