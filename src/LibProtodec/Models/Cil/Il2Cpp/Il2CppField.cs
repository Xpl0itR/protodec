// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;
using LibCpp2IL.Metadata;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppField(Il2CppFieldDefinition il2CppField) : Il2CppMember, ICilField
{
    private readonly FieldAttributes _attributes =
        (FieldAttributes)il2CppField.RawFieldType!.Attrs;

    public string Name =>
        il2CppField.Name!;

    public object? ConstantValue =>
        il2CppField.DefaultValue!.Value;

    public bool IsLiteral =>
        (_attributes & FieldAttributes.Literal) != 0;

    public bool IsPublic =>
        (_attributes & FieldAttributes.Public) != 0;

    public bool IsStatic =>
        (_attributes & FieldAttributes.Static) != 0;

    protected override Il2CppImageDefinition DeclaringAssembly =>
        il2CppField.FieldType!.baseType!.DeclaringAssembly!;

    protected override int CustomAttributeIndex =>
        il2CppField.customAttributeIndex;

    protected override uint Token =>
        il2CppField.token;
}