using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace protodec;

public sealed record ProtobufMessage(string Name) : IWritable
{
    public readonly HashSet<string>                             Imports = new();
    public readonly Dictionary<string, int[]>                   OneOfs  = new();
    public readonly Dictionary<int, (string Type, string Name)> Fields  = new();
    public readonly Dictionary<string, IWritable>               Nested  = new();

    public void WriteFileTo(IndentedTextWriter writer)
    {
        Protodec.WritePreambleTo(writer);

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

    public void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("message ");
        writer.Write(Name);
        writer.WriteLine(" {");
        writer.Indent++;

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

        int[] oneOfs = OneOfs.SelectMany(oneOf => oneOf.Value).ToArray();

        foreach ((int fieldId, (string, string) field) in Fields)
        {
            if (oneOfs.Contains(fieldId))
                continue;

            WriteField(writer, fieldId, field);
        }

        foreach (IWritable nested in Nested.Values)
        {
            nested.WriteTo(writer);
            writer.WriteLine();
        }

        writer.Indent--;
        writer.Write('}');
    }

    private static void WriteField(TextWriter writer, int fieldId, (string Type, string Name) field)
    {
        writer.Write(field.Type);
        writer.Write(' ');
        writer.Write(field.Name);
        writer.Write(" = ");
        writer.Write(fieldId);
        writer.WriteLine(';');
    }
}