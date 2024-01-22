// Copyright © 2023-2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibProtodec;

public sealed class Message : Protobuf
{
    public readonly HashSet<string>                                              Imports = [];
    public readonly Dictionary<string, int[]>                                    OneOfs  = [];
    public readonly Dictionary<int, (bool IsOptional, string Type, string Name)> Fields  = [];
    public readonly Dictionary<string, Protobuf>                                 Nested  = [];

    public override void WriteFileTo(IndentedTextWriter writer)
    {
        this.WritePreambleTo(writer);

        if (Imports.Count > 0)
        {
            foreach (string import in Imports)
            {
                writer.Write("import \"");
                writer.Write(import);
                writer.WriteLine(".proto\";");
            }

            writer.WriteLine();
        }

        WriteTo(writer);
    }

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("message ");
        writer.Write(this.Name);
        writer.WriteLine(" {");
        writer.Indent++;

        int[] oneOfs = OneOfs.SelectMany(oneOf => oneOf.Value).ToArray();

        foreach ((int fieldId, (bool, string, string) field) in Fields)
        {
            if (oneOfs.Contains(fieldId))
                continue;

            WriteField(writer, fieldId, field);
        }

        foreach ((string name, int[] fieldIds) in OneOfs)
        {
            // ReSharper disable once StringLiteralTypo
            writer.Write("oneof ");
            writer.Write(name);
            writer.WriteLine(" {");
            writer.Indent++;

            foreach (int fieldId in fieldIds)
            {
                WriteField(writer, fieldId, Fields[fieldId]);
            }

            writer.Indent--;
            writer.WriteLine('}');
        }

        foreach (Protobuf nested in Nested.Values)
        {
            nested.WriteTo(writer);
            writer.WriteLine();
        }

        writer.Indent--;
        writer.Write('}');
    }

    private static void WriteField(TextWriter writer, int fieldId, (bool IsOptional, string Type, string Name) field)
    {
        if (field.IsOptional)
        {
            writer.Write("optional ");
        }

        writer.Write(field.Type);
        writer.Write(' ');
        writer.Write(field.Name);
        writer.Write(" = ");
        writer.Write(fieldId);
        writer.WriteLine(';');
    }
}