﻿// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Diagnostics;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrMethod(MethodInfo clrMethod) : ClrMember(clrMethod), ICilMethod
{
    public bool IsConstructor =>
        clrMethod.IsConstructor;

    public bool IsPublic =>
        clrMethod.IsPublic;

    public bool IsStatic =>
        clrMethod.IsStatic;

    public bool IsVirtual =>
        clrMethod.IsVirtual;

    public ICilType ReturnType =>
        ClrType.GetOrCreate(
            clrMethod.ReturnType);

    public IEnumerable<ICilType> GetParameterTypes()
    {
        foreach (ParameterInfo parameter in clrMethod.GetParameters())
        {
            yield return ClrType.GetOrCreate(
                parameter.ParameterType);
        }
    }

    public byte[] GetMethodBodyILAsByteArray() =>
        clrMethod.GetMethodBody()?.GetILAsByteArray()
     ?? ThrowHelper.ThrowNotSupportedException<byte[]>();
}