// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using LibCpp2IL;
using LibCpp2IL.Metadata;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppProperty(Il2CppPropertyDefinition il2CppProperty, Il2CppTypeDefinition declaringType) : Il2CppMember, ICilProperty
{
    private Il2CppMethod? _getter;
    private Il2CppMethod? _setter;

    public string Name =>
        il2CppProperty.Name!;

    public bool IsInherited =>
        false;

    public bool CanRead =>
        il2CppProperty.get >= 0;

    public bool CanWrite =>
        il2CppProperty.set >= 0;

    public ICilMethod? Getter =>
        CanRead
            ? _getter ??= new Il2CppMethod(
                LibCpp2IlMain.TheMetadata!.methodDefs[
                    declaringType.FirstMethodIdx + il2CppProperty.get])
            : null;

    public ICilMethod? Setter =>
        CanWrite
            ? _setter ??= new Il2CppMethod(
                LibCpp2IlMain.TheMetadata!.methodDefs[
                    declaringType.FirstMethodIdx + il2CppProperty.set])
            : null;

    public ICilType Type =>
        Getter?.ReturnType
     ?? Setter!.GetParameterTypes().First();

    protected override Il2CppImageDefinition DeclaringAssembly =>
        declaringType.DeclaringAssembly!;

    protected override int CustomAttributeIndex =>
        il2CppProperty.customAttributeIndex;

    protected override uint Token =>
        il2CppProperty.token;
}