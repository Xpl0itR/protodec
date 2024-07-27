// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using CommunityToolkit.Diagnostics;
using LibCpp2IL.Metadata;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppAttribute(Il2CppTypeDefinition il2CppAttrType, object?[]? ctorArgValues) : ICilAttribute
{
    public ICilType Type =>
        Il2CppType.GetOrCreate(il2CppAttrType);

    public bool CanReadConstructorArgumentValues =>
        ctorArgValues is not null;

    public IList<object?> ConstructorArgumentValues =>
        ctorArgValues
     ?? ThrowHelper.ThrowNotSupportedException<IList<object?>>(
            "Attribute constructor argument parsing is only available on Il2Cpp metadata version 29 or greater.");
}