// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetRipper.Primitives;
using LibCpp2IL;
using LibProtodec;
using LibProtodec.Loaders;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Protobuf;

const string indent = "  ";
const string help   = """
    Usage: protodec(.exe) <game_assembly_path> <global_metadata_path> <unity_version> <out_path> [options]
    Arguments:
      game_assembly_path    The path to the game assembly DLL.
      global_metadata_path  The path to the global-metadata.dat file.
      unity_version         The version of Unity which was used to create the metadata file or alternatively, the path to the globalgamemanagers or the data.unity3d file.
      out_path              An existing directory to output into individual files, otherwise output to a single file.
    Options:
      --parse_service_servers                                     Parses gRPC service definitions from server classes.
      --parse_service_clients                                     Parses gRPC service definitions from client classes.
      --skip_enums                                                Skip parsing enums and replace references to them with int32.
      --include_properties_without_non_user_code_attribute        Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing.
      --include_service_methods_without_generated_code_attribute  Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services.
    """;

if (args.Length < 4)
{
    Console.WriteLine(help);
    return;
}

string        assembly = args[0];
string        metadata = args[1];
string        uVersion = args[2];
string        outPath  = Path.GetFullPath(args[3]);
ParserOptions options  = ParserOptions.None;

if (args.Contains("--skip_enums"))
    options |= ParserOptions.SkipEnums;

if (args.Contains("--include_properties_without_non_user_code_attribute"))
    options |= ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute;

if (args.Contains("--include_service_methods_without_generated_code_attribute"))
    options |= ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute;

if (!UnityVersion.TryParse(uVersion, out UnityVersion unityVersion, out _))
{
    unityVersion = uVersion.EndsWith("globalgamemanagers")
        ? LibCpp2IlMain.GetVersionFromGlobalGameManagers(
            File.ReadAllBytes(uVersion))
        : LibCpp2IlMain.GetVersionFromDataUnity3D(
            File.OpenRead(uVersion));
}

using ICilAssemblyLoader loader = new Il2CppAssemblyLoader(assembly, metadata, unityVersion);
ProtodecContext ctx = new();

foreach (ICilType message in GetProtobufMessageTypes())
{
    ctx.ParseMessage(message, options);
}

if (args.Contains("--parse_service_servers"))
{
    foreach (ICilType service in GetProtobufServiceServerTypes())
    {
        ctx.ParseService(service, options);
    }
}

if (args.Contains("--parse_service_clients"))
{
    foreach (ICilType service in GetProtobufServiceClientTypes())
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

IEnumerable<ICilType> GetProtobufMessageTypes() =>
    loader.LoadedTypes.Where(
        type => type is { IsNested: false, IsSealed: true }
             && type.Namespace?.StartsWith("Google.Protobuf", StringComparison.Ordinal) != true
             && type.IsAssignableTo(loader.IMessage));

IEnumerable<ICilType> GetProtobufServiceClientTypes() =>
    loader.LoadedTypes.Where(
        type => type is { IsNested: true, IsAbstract: false }
             && type.IsAssignableTo(loader.ClientBase));

IEnumerable<ICilType> GetProtobufServiceServerTypes() =>
    loader.LoadedTypes.Where(
        type => type is { IsNested: true, IsAbstract: true, DeclaringType: { IsNested: false, IsSealed: true, IsAbstract: true } }
             && type.GetCustomAttributes().Any(attribute => attribute.Type == loader.BindServiceMethodAttribute));