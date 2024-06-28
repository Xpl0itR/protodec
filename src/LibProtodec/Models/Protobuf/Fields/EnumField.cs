// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Protobuf.Fields;

public sealed class EnumField
{
    public required string Name { get; init; }
    public required int    Id   { get; init; }

    public bool IsObsolete { get; init; }

    public void WriteTo(System.IO.TextWriter writer)
    {
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