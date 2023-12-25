using System.CodeDom.Compiler;
using System.Collections.Generic;

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