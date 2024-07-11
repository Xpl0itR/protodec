// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrProperty(PropertyInfo clrProperty) : ClrMember(clrProperty), ICilProperty
{
    private readonly MethodInfo? _getterInfo = clrProperty.GetMethod;
    private readonly MethodInfo? _setterInfo = clrProperty.SetMethod;

    private ClrMethod? _getter;
    private ClrMethod? _setter;

    public bool CanRead =>
        _getterInfo is not null;

    public bool CanWrite =>
        _setterInfo is not null;

    public ICilMethod? Getter =>
        _getterInfo is null
            ? null
            : _getter ??= new ClrMethod(_getterInfo);

    public ICilMethod? Setter =>
        _setterInfo is null
            ? null
            : _setter ??= new ClrMethod(_setterInfo);

    public ICilType Type =>
        ClrType.GetOrCreate(
            clrProperty.PropertyType);
}