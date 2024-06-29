// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using LibCpp2IL.Reflection;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppAttribute(Il2CppTypeReflectionData il2CppAttrType) : ICilAttribute
{
    public ICilType Type =>
        Il2CppType.GetOrCreate(il2CppAttrType);

    public IList<object?> ConstructorArguments =>
        throw new NotImplementedException();
}