// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Reflection;
using LibCpp2IL;
using LibCpp2IL.Metadata;
using LibCpp2IL.Reflection;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppMethod(Il2CppMethodDefinition il2CppMethod) : Il2CppMember, ICilMethod
{
    public string Name =>
        il2CppMethod.Name!;

    public bool IsInherited =>
        false;

    public bool IsConstructor =>
        Name is ".ctor" or ".cctor";

    public bool IsPublic =>
        (il2CppMethod.Attributes & MethodAttributes.Public) != 0;

    public bool IsStatic =>
        (il2CppMethod.Attributes & MethodAttributes.Static) != 0;

    public bool IsVirtual =>
        (il2CppMethod.Attributes & MethodAttributes.Virtual) != 0;

    public ICilType ReturnType =>
        Il2CppType.GetOrCreate(
            LibCpp2ILUtils.GetTypeReflectionData(
                LibCpp2IlMain.Binary!.GetType(
                    il2CppMethod.returnTypeIdx)));

    public IEnumerable<ICilType> GetParameterTypes()
    {
        foreach (Il2CppParameterReflectionData parameter in il2CppMethod.Parameters!)
        {
            yield return Il2CppType.GetOrCreate(parameter.Type);
        }
    }

    protected override Il2CppImageDefinition DeclaringAssembly =>
        il2CppMethod.DeclaringType!.DeclaringAssembly!;

    protected override int CustomAttributeIndex =>
        il2CppMethod.customAttributeIndex;

    protected override uint Token =>
        il2CppMethod.token;
}