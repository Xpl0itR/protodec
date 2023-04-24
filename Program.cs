using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using protodec;

const string indent = "  ";
const string help   = """
    Usage: protodec(.exe) [options] <target_assembly_path> <out_path>
    Options:
      --skip_enums                  Skip parsing enums and replace references to then with int32.
      --include_runtime_assemblies  Add the assemblies of the current runtime to the search path.
    Arguments:
      target_assembly_path  Either a single assembly or a directory of assemblies to be parsed.
      out_path              An existing directory to output into individual files, otherwise output to a single file.
    """;

if (args.Length < 2)
{
    Console.WriteLine(help);
    return;
}

string assembly  = args[0];
string outPath   = args[1];
bool   runtime   = args.Contains("--include_runtime_assemblies");
bool   skipEnums = args.Contains("--skip_enums");

using AssemblyInspector inspector = new(assembly, runtime);
Protodec protodec = new();

foreach (Type message in inspector.GetProtobufMessageTypes())
{
    protodec.ParseMessage(message, skipEnums);
}

outPath = Path.GetFullPath(outPath);
if (Directory.Exists(outPath))
{
    foreach (IWritable proto in protodec.Messages.Values.Concat<IWritable>(protodec.Enums.Values))
    {
        using StreamWriter       streamWriter = new(Path.Join(outPath, proto.Name + ".proto"));
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