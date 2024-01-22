using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using LibProtodec;

const string indent = "  ";
const string help   = """
    Usage: protodec(.exe) <target_assembly_path> <out_path> [options]
    Arguments:
      target_assembly_path  Either the path to the target assembly or a directory of assemblies, all of which be parsed.
      out_path              An existing directory to output into individual files, otherwise output to a single file.
    Options:
      --skip_enums                                Skip parsing enums and replace references to them with int32.
      --skip_properties_without_protoc_attribute  Skip properties that aren't decorated with `GeneratedCode("protoc")` when parsing
    """;

if (args.Length < 2)
{
    Console.WriteLine(help);
    return;
}

string assemblyPath                         = args[0];
string outPath                              = Path.GetFullPath(args[1]);
bool   skipEnums                            = args.Contains("--skip_enums");
bool   skipPropertiesWithoutProtocAttribute = args.Contains("--skip_properties_without_protoc_attribute");

using AssemblyInspector inspector = new(assemblyPath);
ProtodecContext ctx = new();

foreach (Type message in inspector.GetProtobufMessageTypes())
{
    ctx.ParseMessage(message, skipEnums, skipPropertiesWithoutProtocAttribute);
}

if (Directory.Exists(outPath))
{
    foreach (Protobuf proto in ctx.Protobufs.Values)
    {
        string protoPath = Path.Join(outPath, proto.Name + ".proto");

        using StreamWriter       streamWriter = new(protoPath);
        using IndentedTextWriter indentWriter = new(streamWriter, indent);

        proto.WriteFileTo(indentWriter);
    }
}
else
{
    using StreamWriter       streamWriter = new(outPath);
    using IndentedTextWriter indentWriter = new(streamWriter, indent);

    ctx.WriteAllTo(indentWriter);
}