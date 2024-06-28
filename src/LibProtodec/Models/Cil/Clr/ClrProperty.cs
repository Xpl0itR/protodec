// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrProperty(PropertyInfo clrProperty) : ClrMember(clrProperty), ICilProperty
{
    public ICilType Type =>
        ClrType.GetOrCreate(
            clrProperty.PropertyType);

    public bool CanRead =>
        clrProperty.CanRead;

    public bool CanWrite =>
        clrProperty.CanWrite;

    public ICilMethod? Getter =>
        CanRead
            ? new ClrMethod(clrProperty.GetMethod!)
            : null;

    public ICilMethod? Setter =>
        CanWrite
            ? new ClrMethod(clrProperty.SetMethod!)
            : null;
}