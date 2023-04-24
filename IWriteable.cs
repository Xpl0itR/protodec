using System.CodeDom.Compiler;

namespace protodec;

public interface IWritable
{
    string Name { get; }

    void WriteFileTo(IndentedTextWriter writer);

    void WriteTo(IndentedTextWriter writer);
}