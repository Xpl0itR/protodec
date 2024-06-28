// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrAttribute(CustomAttributeData clrAttribute) : ICilAttribute
{
    private IList<object?>? _constructorArguments;

    public ICilType Type =>
        ClrType.GetOrCreate(clrAttribute.AttributeType);

    public IList<object?> ConstructorArguments
    {
        get
        {
            if (_constructorArguments is null)
            {
                IList<CustomAttributeTypedArgument> args = clrAttribute.ConstructorArguments;

                if (args.Count < 1)
                {
                    _constructorArguments = Array.Empty<object>();
                }
                else
                {
                    _constructorArguments = args.Select(static arg => arg.Value).ToList();
                }
            }

            return _constructorArguments;
        }
    }
}