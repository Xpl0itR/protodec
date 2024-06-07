// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace LibProtodec.Models.TopLevels;

public abstract class TopLevel
{
    public required string Name { get; init; }
    public bool      IsObsolete { get; init; }
    public Protobuf? Protobuf   { get; set;  }
    public TopLevel? Parent     { get; set;  }

    public string QualifyName(TopLevel topLevel)
    {
        List<string> names = [Name];

        TopLevel? parent = Parent;
        while (parent is not null && parent != topLevel)
        {
            names.Add(parent.Name);
            parent = parent.Parent;
        }

        names.Reverse();
        return string.Join('.', names);
    }

    public abstract void WriteTo(IndentedTextWriter writer);
}