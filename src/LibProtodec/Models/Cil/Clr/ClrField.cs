// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrField(FieldInfo clrField) : ClrMember(clrField), ICilField
{
    public object? ConstantValue =>
        clrField.GetRawConstantValue();

    public bool IsLiteral =>
        clrField.IsLiteral;

    public bool IsPublic =>
        clrField.IsPublic;

    public bool IsStatic =>
        clrField.IsStatic;
}