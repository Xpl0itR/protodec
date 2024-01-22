// Copyright © 2023-2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using SystemEx.Collections;

namespace LibProtodec;

public sealed class Enum : Protobuf
{
    public readonly List<KeyValuePair<int, string>> Fields = [];

    public override void WriteFileTo(IndentedTextWriter writer)
    {
        this.WritePreambleTo(writer);
        WriteTo(writer);
    }

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("enum ");
        writer.Write(this.Name);
        writer.WriteLine(" {");
        writer.Indent++;

        if (Fields.ContainsDuplicateKey())
        {
            writer.WriteLine("option allow_alias = true;");
        }

        foreach ((int id, string name) in Fields)
        {
            writer.Write(name);
            writer.Write(" = ");
            writer.Write(id);
            writer.WriteLine(';');
        }

        writer.Indent--;
        writer.Write('}');
    }
}