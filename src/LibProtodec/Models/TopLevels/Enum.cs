// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

global using Enum = LibProtodec.Models.TopLevels.Enum;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using LibProtodec.Models.Fields;
using LibProtodec.Models.Types;

namespace LibProtodec.Models.TopLevels;

public sealed class Enum : TopLevel, INestableType
{
    public readonly List<EnumField> Fields = [];

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("enum ");
        writer.Write(this.Name);
        writer.WriteLine(" {");
        writer.Indent++;

        if (ContainsDuplicateField)
        {
            Protobuf.WriteOptionTo(writer, "allow_alias", "true");
        }

        if (this.IsObsolete)
        {
            Protobuf.WriteOptionTo(writer, "deprecated", "true");
        }

        if (IsClosed)
        {
            Protobuf.WriteOptionTo(writer, "features.enum_type", "CLOSED");
        }

        foreach (EnumField field in Fields)
        {
            field.WriteTo(writer);
        }

        writer.Indent--;
        writer.Write('}');
    }

    public bool IsClosed { get; set; }

    private bool ContainsDuplicateField
    {
        get
        {
            if (Fields.Count < 2)
                return false;

            HashSet<int> set = [];
            foreach (EnumField field in Fields)
                if (!set.Add(field.Id))
                    return true;

            return false;
        }
    }
}