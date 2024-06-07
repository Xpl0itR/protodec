using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibProtodec;
using LibProtodec.Models;

const string indent = "  ";
const string help   = """
    Usage: protodec(.exe) <target_assembly_path> <out_path> [options]
    Arguments:
      target_assembly_path  Either the path to the target assembly or a directory of assemblies, all of which be parsed.
      out_path              An existing directory to output into individual files, otherwise output to a single file.
    Options:
      --parse_service_servers                                     Parses gRPC service definitions from server classes.
      --parse_service_clients                                     Parses gRPC service definitions from client classes.
      --skip_enums                                                Skip parsing enums and replace references to them with int32.
      --include_properties_without_non_user_code_attribute        Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing.
      --include_service_methods_without_generated_code_attribute  Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services.
    """;

if (args.Length < 2)
{
    Console.WriteLine(help);
    return;
}

string        assembly = args[0];
string        outPath  = Path.GetFullPath(args[1]);
ParserOptions options  = ParserOptions.None;

if (args.Contains("--skip_enums"))
    options |= ParserOptions.SkipEnums;

if (args.Contains("--include_properties_without_non_user_code_attribute"))
    options |= ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute;

if (args.Contains("--include_service_methods_without_generated_code_attribute"))
    options |= ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute;

using AssemblyInspector inspector = new(assembly);
ProtodecContext ctx = new();

foreach (Type message in inspector.GetProtobufMessageTypes())
{
    ctx.ParseMessage(message, options);
}

if (args.Contains("--parse_service_servers"))
{
    foreach (Type service in inspector.GetProtobufServiceServerTypes())
    {
        ctx.ParseService(service, options);
    }
}

if (args.Contains("--parse_service_clients"))
{
    foreach (Type service in inspector.GetProtobufServiceClientTypes())
    {
        ctx.ParseService(service, options);
    }
}

if (Directory.Exists(outPath))
{
    HashSet<string> writtenFiles = [];

    foreach (Protobuf protobuf in ctx.Protobufs)
    {
        // This workaround stops files from being overwritten in the case of a naming conflict,
        // however the actual conflict will still have to be resolved manually
        string fileName = protobuf.FileName;
        while (!writtenFiles.Add(fileName))
        {
            fileName = '_' + fileName;
        }

        string protobufPath = Path.Join(outPath, fileName);

        using StreamWriter       streamWriter = new(protobufPath);
        using IndentedTextWriter indentWriter = new(streamWriter, indent);

        protobuf.WriteTo(indentWriter);
    }
}
else
{
    using StreamWriter       streamWriter = new(outPath);
    using IndentedTextWriter indentWriter = new(streamWriter, indent);

    ctx.WriteAllTo(indentWriter);
}