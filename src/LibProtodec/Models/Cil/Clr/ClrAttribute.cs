// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrAttribute(CustomAttributeData clrAttribute) : ICilAttribute
{
    private ICilType?  _type;
    private object?[]? _constructorArgumentValues;

    public ICilType Type =>
        _type ??= ClrType.GetOrCreate(
            clrAttribute.AttributeType);

    public IList<object?> ConstructorArgumentValues
    {
        get
        {
            if (_constructorArgumentValues is null)
            {
                IList<CustomAttributeTypedArgument> args = clrAttribute.ConstructorArguments;

                _constructorArgumentValues = args.Count < 1
                    ? Array.Empty<object>()
                    : new object[args.Count];

                for (int i = 0; i < args.Count; i++)
                {
                    _constructorArgumentValues[i] = args[i].Value;
                }
            }

            return _constructorArgumentValues;
        }
    }
}