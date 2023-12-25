using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using LibProtodec;

const string indent = "  ";
const string help   = """
    Usage: protodec(.exe) <target_assembly_dir> <out_path> [target_assembly_name] [options]
    Arguments:
      target_assembly_dir   A directory of assemblies to be loaded.
      out_path              An existing directory to output into individual files, otherwise output to a single file.
      target_assembly_name  The name of an assembly to parse. If omitted, all assemblies in the target_assembly_dir will be parsed.
    Options:
      --skip_enums                                Skip parsing enums and replace references to them with int32.
      --skip_properties_without_protoc_attribute  Skip properties that aren't decorated with `GeneratedCode("protoc")` when parsing
    """;

if (args.Length < 2)
{
    Console.WriteLine(help);
    return;
}

string? assemblyName = null;
if (args.Length > 2 && !args[2].StartsWith('-'))
{
    assemblyName = args[2];
}

string assemblyDir = args[0];
string outPath     = Path.GetFullPath(args[1]);
bool   skipEnums   = args.Contains("--skip_enums");
bool   skipPropertiesWithoutProtocAttribute = args.Contains("--skip_properties_without_protoc_attribute");

using AssemblyInspector inspector = new(assemblyDir, assemblyName);
Protodec protodec = new();

foreach (Type message in inspector.GetProtobufMessageTypes())
{
    protodec.ParseMessage(message, skipEnums, skipPropertiesWithoutProtocAttribute);
}

if (Directory.Exists(outPath))
{
    foreach (Protobuf proto in protodec.Protobufs.Values)
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

    protodec.WriteAllTo(indentWriter);
}