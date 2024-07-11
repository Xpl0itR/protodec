// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using LibProtodec.Models.Protobuf.Types;

namespace LibProtodec.Models.Protobuf.TopLevels;

public abstract class TopLevel
{
    public required string Name { get; init; }

    public bool      IsObsolete { get; init; }
    public Protobuf? Protobuf   { get; set;  }
    public TopLevel? Parent     { get; set;  }

    public string QualifyTypeName(IProtobufType type)
    {
        if (type is not TopLevel { Parent: not null } typeTopLevel
         || typeTopLevel.Parent == this)
            return type.Name;

        List<string> names = [typeTopLevel.Name];

        TopLevel? parent = typeTopLevel.Parent;
        while (parent is not null && parent != this)
        {
            names.Add(parent.Name);
            parent = parent.Parent;
        }

        names.Reverse();
        return string.Join('.', names);
    }

    public abstract void WriteTo(IndentedTextWriter writer);
}