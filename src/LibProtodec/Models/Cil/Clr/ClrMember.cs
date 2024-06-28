// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public abstract class ClrMember(MemberInfo clrMember)
{
    public string Name =>
        clrMember.Name;

    public bool IsInherited =>
        clrMember.DeclaringType != clrMember.ReflectedType;

    public ICilType? DeclaringType =>
        clrMember.DeclaringType is null
            ? null
            : ClrType.GetOrCreate(
                clrMember.DeclaringType);

    public IEnumerable<ICilAttribute> GetCustomAttributes()
    {
        foreach (CustomAttributeData attribute in clrMember.GetCustomAttributesData())
        {
            yield return new ClrAttribute(attribute);
        }
    }
}