using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace protodec;

public sealed record ProtobufEnum(string Name) : IWritable
{
    public readonly Dictionary<int, string> Fields = new();

    public void WriteFileTo(IndentedTextWriter writer)
    {
        Protodec.WritePreambleTo(writer);
        WriteTo(writer);
    }

    public void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("enum ");
        writer.Write(Name);
        writer.WriteLine(" {");
        writer.Indent++;

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