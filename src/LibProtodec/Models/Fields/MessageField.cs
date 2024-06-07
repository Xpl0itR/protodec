// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using LibProtodec.Models.TopLevels;
using LibProtodec.Models.Types;

namespace LibProtodec.Models.Fields;

public sealed class MessageField
{
    public required IType  Type { get; init; }
    public required string Name { get; init; }
    public required int    Id   { get; init; }

    public bool IsObsolete { get; init; }
    public bool HasHasProp { get; init; }

    public void WriteTo(TextWriter writer, TopLevel topLevel, bool isOneOf)
    {
        if (HasHasProp && !isOneOf && Type is not Repeated)
        {
            writer.Write("optional ");
        }

        Protobuf.WriteTypeNameTo(writer, Type, topLevel);
        writer.Write(' ');
        writer.Write(Name);
        writer.Write(" = ");
        writer.Write(Id);

        if (IsObsolete)
        {
            writer.Write(" [deprecated = true]");
        }

        writer.WriteLine(';');
    }
}