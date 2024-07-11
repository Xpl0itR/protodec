// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using LibProtodec.Models.Protobuf.Fields;
using LibProtodec.Models.Protobuf.Types;

namespace LibProtodec.Models.Protobuf.TopLevels;

public sealed class Message : TopLevel, INestableType
{
    public readonly Dictionary<string, List<int>>     OneOfs = [];
    public readonly Dictionary<int, MessageField>     Fields = [];
    public readonly Dictionary<string, INestableType> Nested = [];

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("message ");
        writer.Write(this.Name);
        writer.WriteLine(" {");
        writer.Indent++;

        if (this.IsObsolete)
        {
            Protobuf.WriteOptionTo(writer, "deprecated", "true");
        }

        int[] oneOfs = OneOfs.SelectMany(static oneOf => oneOf.Value).ToArray();

        foreach (MessageField field in Fields.Values)
        {
            if (oneOfs.Contains(field.Id))
                continue;

            field.WriteTo(writer, isOneOf: false);
        }

        foreach ((string name, List<int> fieldIds) in OneOfs)
        {
            // ReSharper disable once StringLiteralTypo
            writer.Write("oneof ");
            writer.Write(name);
            writer.WriteLine(" {");
            writer.Indent++;

            foreach (int fieldId in fieldIds)
            {
                Fields[fieldId].WriteTo(writer, isOneOf: true);
            }

            writer.Indent--;
            writer.WriteLine('}');
        }

        foreach (INestableType nested in Nested.Values)
        {
            nested.WriteTo(writer);
            writer.WriteLine();
        }

        writer.Indent--;
        writer.Write('}');
    }
}